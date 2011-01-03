#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using MsgPack.Rpc.Protocols;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Serialize outgoing response message and deserialize incoming response message.
	/// </summary>
	public sealed class ResponseMessageSerializer
	{
		private readonly List<IFilterProvider<ResponseMessageSerializationFilter>> _preSerializationFilters;
		private readonly List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> _postSerializationFilters;
		private readonly List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> _preDeserializationFilters;
		private readonly List<IFilterProvider<ResponseMessageDeserializationFilter>> _postDeserializationFilters;
		private readonly int? _maxRequestLength;

		public ResponseMessageSerializer(
			List<IFilterProvider<ResponseMessageSerializationFilter>> preSerializationFilters,
			List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> postSerializationFilters,
			List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> preDeserializationFilters,
			List<IFilterProvider<ResponseMessageDeserializationFilter>> postDeserializationFilters,
			int? maxRequestLength
			)
		{
			Contract.Assert( maxRequestLength.GetValueOrDefault() >= 0 );

			this._preSerializationFilters = preSerializationFilters ?? Constants.EmptyResponseMessageSerializationFilterProviders;
			this._postSerializationFilters = postSerializationFilters ?? Constants.EmptySerializedMessageFilterProviders;
			this._preDeserializationFilters = preDeserializationFilters ?? Constants.EmptyDeserializedMessageFilterProviders;
			this._postDeserializationFilters = postDeserializationFilters ?? Constants.EmptyResponseMessageDeserializationFilterProviders;
			this._maxRequestLength = maxRequestLength;
		}

		public RpcErrorMessage Serialize( int messageId, object returnValue, bool isVoid, RpcException exception, RpcOutputBuffer buffer )
		{
			var context =
				exception != null
				? new ResponseMessageSerializationContext( buffer, exception, isVoid )
				: ( isVoid ? new ResponseMessageSerializationContext( buffer ) : new ResponseMessageSerializationContext( buffer, returnValue ) );

			foreach ( var preSerializationFilter in this._preSerializationFilters )
			{
				preSerializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					return context.SerializationError;
				}
			}

			SerializeCore( MessageType.Response, messageId, context );
			if ( !context.SerializationError.IsSuccess )
			{
				return context.SerializationError;
			}

			foreach ( var postSerializationFilter in this._postSerializationFilters )
			{
				using ( var swapper = buffer.CreateSwapper() )
				{
					swapper.WriteBytes( postSerializationFilter.GetFilter().Process( swapper.ReadBytes(), context ) );
					if ( !context.SerializationError.IsSuccess )
					{
						return context.SerializationError;
					}
				}
			}

			return RpcErrorMessage.Success;
		}

		private void SerializeCore( MessageType messageType, int messageId, ResponseMessageSerializationContext context )
		{
			using ( var stream = context.Buffer.OpenStream( true ) )
			using ( var packer = Packer.Create( stream ) )
			{
				packer.PackArrayHeader( 4 );
				packer.Pack( ( int )messageType );
				packer.Pack( unchecked( ( uint )messageId ) );
				if ( context.Exception == null )
				{
					packer.PackNull();
					packer.Pack( context.ReturnValue );
				}
				else
				{
					packer.PackString( context.Exception.RpcError.Identifier );
					packer.PackMapHeader( context.Exception.DebugInformation == null ? 2 : 3 );
					packer.PackString( "ErrorCode" );
					packer.Pack( context.Exception.RpcError.ErrorCode );
					packer.PackString( "Description" );
					packer.PackString( context.Exception.Message );
					if ( context.Exception.DebugInformation != null )
					{
						packer.PackString( "DebugInformation" );
						packer.PackString( context.Exception.DebugInformation );
					}
				}
			}
		}
		
		public RpcErrorMessage Deserialize( RpcInputBuffer buffer, out ResponseMessage result )
		{
			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			Contract.EndContractBlock();

			var context = new ResponseMessageDeserializationContext( buffer, this._maxRequestLength );
			var sequence = context.ReadBytes();

			foreach ( var preDeserializationFilter in this._preDeserializationFilters )
			{
				var processed = preDeserializationFilter.GetFilter().Process( sequence, context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( ResponseMessage );
					return context.SerializationError;
				}

				if ( processed == null )
				{
					throw new InvalidOperationException( "Deserialization filter did not return sequence." );
				}

				sequence = processed;
			}

			DeserializeCore( sequence, context );

			if ( !context.SerializationError.IsSuccess )
			{
				result = default( ResponseMessage );
				return context.SerializationError;
			}

			foreach ( var postDeserializationFilter in this._postDeserializationFilters )
			{
				postDeserializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( ResponseMessage );
					return context.SerializationError;
				}
			}

			if ( !context.Error.IsNil )
			{
				result = new ResponseMessage( context.MessageId, RpcException.FromMessage( context.Error, context.DeserializedResult.Value ) );
			}
			else
			{
				result = new ResponseMessage( context.MessageId, context.DeserializedResult.Value );
			}

			return RpcErrorMessage.Success;
		}

		private static void DeserializeCore( IEnumerable<byte> sequence, ResponseMessageDeserializationContext context )
		{
			using ( var unpacker = new Unpacker( sequence ) )
			{
				MessagePackObject? response = unpacker.UnpackObject();
				if ( response == null )
				{
					if ( context.SerializationError.IsSuccess )
					{
						// Since entire stream was readed and its length was in quota, the stream may be coruppted.
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." ) );
					}

					return;
				}

				if ( response == null )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." ) );
					return;
				}

				if ( !response.Value.IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Response message is not array." ) );
					return;
				}

				var requestFields = response.Value.AsList();
				if ( requestFields.Count != 4 )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Response message is not 4 element array." ) );
					return;
				}

				if ( !requestFields[ 0 ].IsTypeOf<int>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of response message is not int 32." ) );
					return;
				}

				if ( requestFields[ 0 ].AsInt32() != ( int )MessageType.Response )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of response message is not Response(2)." ) );
					return;
				}

				if ( !requestFields[ 1 ].IsTypeOf<int>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message ID of response message is not int32." ) );
					return;
				}

				context.MessageId = requestFields[ 1 ].AsInt32();

				// Error is should be string identifier of error, but arbitary objects are supported.
				context.Error = requestFields[ 2 ];

				// If error is specified, this value should be nil by original spec, but currently should specify error information.
				context.SetDeserializedResult( requestFields[ 3 ] );
			}
		}

		// FIXME: Must use PpcBuffer, and reset it buffer appropriately.
		[Obsolete]
		public ResponseMessage? Deserialize( IEnumerable<byte> input, out RpcErrorMessage error )
		{
			throw new NotImplementedException();
		}
	}
}
