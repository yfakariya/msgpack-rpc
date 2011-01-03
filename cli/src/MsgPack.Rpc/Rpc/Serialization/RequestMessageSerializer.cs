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
using MsgPack.Collections;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Serialize outgoing request/notification message and deserialize incoming request/notification message.
	/// </summary>
	public sealed class RequestMessageSerializer
	{
		private readonly List<IFilterProvider<RequestMessageSerializationFilter>> _preSerializationFilters;
		private readonly List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> _postSerializationFilters;
		private readonly List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> _preDeserializationFilters;
		private readonly List<IFilterProvider<RequestMessageDeserializationFilter>> _postDeserializationFilters;
		private readonly int? _maxRequestLength;

		public RequestMessageSerializer(
			List<IFilterProvider<RequestMessageSerializationFilter>> preSerializationFilters,
			List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> postSerializationFilters,
			List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> preDeserializationFilters,
			List<IFilterProvider<RequestMessageDeserializationFilter>> postDeserializationFilters,
			int? maxRequestLength
			)
		{
			Contract.Assert( maxRequestLength.GetValueOrDefault() >= 0 );

			this._preSerializationFilters = preSerializationFilters ?? Constants.EmptyRequestMessageSerializationFilterProviders;
			this._postSerializationFilters = postSerializationFilters ?? Constants.EmptySerializedMessageFilterProviders;
			this._preDeserializationFilters = preDeserializationFilters ?? Constants.EmptyDeserializedMessageFilterProviders;
			this._postDeserializationFilters = postDeserializationFilters ?? Constants.EmptyRequestMessageDeserializationFilterProviders;
			this._maxRequestLength = maxRequestLength;
		}

		public RpcErrorMessage Serialize( MessageType type, int? messageId, string method, IList<object> arguments, RpcOutputBuffer buffer )
		{
			switch ( type )
			{
				case MessageType.Request:
				{
					if ( messageId == null )
					{
						throw new ArgumentNullException( "messageId", "'messageId' must not be null when type is MessageType.Request." );
					}

					break;
				}
				case MessageType.Notification:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "type", "'type' must be MessageType.Request or MessageType.Notification." );
				}
			}

			if ( method == null )
			{
				throw new ArgumentNullException( "method" );
			}

			if ( String.IsNullOrWhiteSpace( method ) )
			{
				throw new ArgumentException( "'method' must not be empty nor blank.", "method" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			Contract.EndContractBlock();

			var context = new RequestMessageSerializationContext( buffer, method, arguments );

			foreach ( var preSerializationFilter in this._preSerializationFilters )
			{
				preSerializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					return context.SerializationError;
				}
			}

			SerializeCore( buffer, type, messageId, context );
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

		private static void SerializeCore( RpcOutputBuffer buffer, MessageType messageType, int? messageId, RequestMessageSerializationContext context )
		{
			using ( var stream = buffer.OpenStream( true ) )
			using ( var packer = Packer.Create( stream ) )
			{
				packer.PackArrayHeader( messageId == null ? 3 : 4 );
				packer.Pack( ( int )messageType );

				if ( messageId != null )
				{
					packer.Pack( unchecked( ( uint )messageId.Value ) );
				}

				packer.PackString( context.MethodName );
				packer.PackItems( context.Arguments );
			}
		}

		[Obsolete]
		public IEnumerable<byte> Serialize( MessageType type, int? messageId, string method, IList<object> arguments, int initialBufferLength, out RpcErrorMessage error )
		{
			throw new NotImplementedException();
			//Contract.Assert( ( type == MessageType.Request && messageId != null ) || ( type == MessageType.Notification && messageId == null ) );
			//Contract.Assert( !String.IsNullOrWhiteSpace( method ) );
			//Contract.Assert( arguments != null );

			//var context = new RequestMessageSerializationContext( null, method, arguments );

			//foreach ( var preSerializationFilter in this._preSerializationFilters )
			//{
			//    preSerializationFilter.GetFilter().Process( context );
			//    if ( !context.SerializationError.IsSuccess )
			//    {
			//        error = context.SerializationError;
			//        return Enumerable.Empty<byte>();
			//    }
			//}

			//var sequence = SerializeCore( type, messageId, context.MethodName, context.Arguments, initialBufferLength );
			//foreach ( var postSerializationFilter in this._postSerializationFilters )
			//{
			//    sequence = postSerializationFilter.GetFilter().Process( sequence, context );
			//    if ( !context.SerializationError.IsSuccess )
			//    {
			//        error = context.SerializationError;
			//        return Enumerable.Empty<byte>();
			//    }
			//}

			//error = RpcErrorMessage.Success;
			//return sequence;
		}

		private static IEnumerable<byte> SerializeCore( MessageType messageType, int? messageId, string method, IList<object> arguments, int initialBufferLength )
		{
			// TODO: stream serialization MPO* -> IEnumerable<byte>
			var buffer = new MemoryStream( initialBufferLength );
			Packer packer = Packer.Create( buffer );
			packer.Pack( ( int )messageType );
			if ( messageId != null )
			{
				packer.Pack( messageId );
			}
			packer.PackString( method );
			packer.PackItems( arguments );

			buffer.Position = 0;
			for ( int read = buffer.ReadByte(); read >= 0; read = buffer.ReadByte() )
			{
				yield return unchecked( ( byte )read );
			}
		}

		public RpcErrorMessage Deserialize( RpcInputBuffer input, out RequestMessage result )
		{
			if ( input == null )
			{
				throw new ArgumentNullException( "input" );
			}

			Contract.EndContractBlock();

			var context = new RequestMessageDeserializationContext( input, this._maxRequestLength );

			var sequence = context.ReadBytes();

			foreach ( var preDeserializationFilter in this._preDeserializationFilters )
			{
				sequence = preDeserializationFilter.GetFilter().Process( sequence, context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( RequestMessage );
					return context.SerializationError;
				}
			}

			DeserializeCore( context );

			if ( !context.SerializationError.IsSuccess )
			{
				result = default( RequestMessage );
				return context.SerializationError;
			}

			Contract.Assert( !String.IsNullOrWhiteSpace( context.MethodName ) );
			Contract.Assert( context.Arguments != null );

			foreach ( var postDeserializationFilter in this._postDeserializationFilters )
			{
				postDeserializationFilter.GetFilter().Process( context );
				if ( !context.SerializationError.IsSuccess )
				{
					result = default( RequestMessage );
					return context.SerializationError;
				}
			}

			if ( String.IsNullOrWhiteSpace( context.MethodName ) )
			{
				throw new InvalidOperationException( "Filter became method null or empty." );
			}

			if ( context.Arguments == null )
			{
				throw new InvalidOperationException( "Filter became arguments null." );
			}

			result = new RequestMessage( context.MessageId, context.MethodName, context.Arguments );
			return RpcErrorMessage.Success;
		}

		private static void DeserializeCore( RequestMessageDeserializationContext context )
		{
			using ( var unpacker = new Unpacker( context.ReadBytes() ) )
			{
				var request = unpacker.UnpackObject();
				if ( request == null )
				{
					if ( context.SerializationError.IsSuccess )
					{
						// Since entire stream was readed and its length was in quota, the stream may be coruppted.
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." ) );
					}

					return;
				}

				if ( !request.Value.IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not array." ) );
					return;
				}

				var requestFields = request.Value.AsList();
				if ( requestFields.Count > 4 || requestFields.Count < 3 )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not 3 nor 4 element array." ) );
					return;
				}

				if ( !requestFields[ 0 ].IsTypeOf<int>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not int 32." ) );
					return;
				}

				int nextPosition = 1;
				switch ( ( MessageType )requestFields[ 0 ].AsInt32() )
				{
					case MessageType.Request:
					{
						if ( !requestFields[ nextPosition ].IsTypeOf<int>().GetValueOrDefault() )
						{
							context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message ID of request message is not int32." ) );
							return;
						}

						context.MessageId = requestFields[ nextPosition ].AsInt32();

						nextPosition++;
						break;
					}
					case MessageType.Notification:
					{
						break;
					}
					default:
					{
						context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not Request(0) nor Notification(2)." ) );
						return;
					}
				}

				if ( !requestFields[ nextPosition ].IsTypeOf<string>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Method of request message (ID:{0}) is not raw. ", context.MessageId ) ) );
					return;
				}

				try
				{
					context.MethodName = requestFields[ nextPosition ].AsString( Constants.MethodEncoding );
				}
				catch ( InvalidOperationException ex )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Message ID:{0}: {1}", context.MessageId, ex.Message ) ) );
					return;
				}

				nextPosition++;

				if ( !requestFields[ nextPosition ].IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
				{
					context.SetSerializationError( new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Arguments of request message (ID:{0}) is not array.", context.MessageId ) ) );
					return;
				}

				context.Arguments = requestFields[ nextPosition ].AsList();
			}
		}

		// FIXME: Must use PpcBuffer, and reset it buffer appropriately.
		[Obsolete]
		public RequestMessage Deserialize( IEnumerable<byte> input, out RpcErrorMessage error )
		{
			throw new NotImplementedException();
			//Contract.Assert( input != null );

			//var context = new RequestMessageDeserializationContext()

			//var sequence = this.CheckMessageLength( input, context );
			//foreach ( var preDeserializationFilter in this._preDeserializationFilters )
			//{
			//    sequence = preDeserializationFilter.GetFilter().Process( input, context );
			//    if ( !context.SerializationError.IsSuccess )
			//    {
			//        error = context.SerializationError;
			//        return default( RequestMessage );
			//    }
			//}

			//if ( this._preDeserializationFilters.Any() )
			//{
			//    sequence = this.CheckMessageLength( sequence, context );
			//}

			//DeserializeCore( sequence, context );

			//foreach ( var postDeserializationFilter in this._postDeserializationFilters )
			//{
			//    postDeserializationFilter.GetFilter().Process( context );
			//    if ( !context.SerializationError.IsSuccess )
			//    {
			//        error = context.SerializationError;
			//        return default( RequestMessage );
			//    }
			//}

			//if ( !String.IsNullOrWhiteSpace( context.MethodName ) )
			//{
			//    throw new InvalidOperationException( "Filter became method null or empty." );
			//}

			//if ( context.Arguments == null )
			//{
			//    throw new InvalidOperationException( "Filter became arguments null." );
			//}

			//error = RpcErrorMessage.Success;
			//return new RequestMessage( context.MessageId, context.MethodName, context.Arguments );
		}

		private IEnumerable<byte> CheckMessageLength( IEnumerable<byte> source, SerializationErrorSink context )
		{
			throw new NotImplementedException();
			//if ( source == null )
			//{
			//    throw new InvalidOperationException( "filter became source byte stream null." );
			//}

			//int count = 0;
			//foreach ( var b in source )
			//{
			//    if ( ++count > this._maxRequestLength )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageTooLargeError, null, String.Format( CultureInfo.CurrentCulture, "Message must be equal or lessor than {0} bytes.", this._maxRequestLength ) );
			//        yield break;
			//    }

			//    yield return b;
			//}
		}

		private static void DeserializeCore( IEnumerable<byte> sequence, RequestMessageDeserializationContext context )
		{
			//using ( var stream = new EnumerableStream( sequence ) )
			//{
			//    var request = Unpacking.UnpackObject( stream );
			//    if ( request == null )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Cannot deserialize message stream." );
			//        return;
			//    }

			//    if ( !request.Value.IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not array." );
			//        return;
			//    }

			//    var requestFields = request.Value.AsList();
			//    if ( requestFields.Count > 4 || requestFields.Count < 3 )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Request message is not 3 nor 4 element array." );
			//        return;
			//    }

			//    if ( !requestFields[ 0 ].IsTypeOf<int>().GetValueOrDefault() )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not int 32." );
			//        return;
			//    }

			//    int nextPosition = 1;
			//    switch ( ( MessageType )requestFields[ 0 ].AsInt32() )
			//    {
			//        case MessageType.Request:
			//        {
			//            if ( !requestFields[ nextPosition ].IsTypeOf<int>().GetValueOrDefault() )
			//            {
			//                context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message ID of request message is not int32." );
			//                return;
			//            }

			//            context.MessageId = requestFields[ nextPosition ].AsInt32();

			//            nextPosition++;
			//            break;
			//        }
			//        case MessageType.Notification:
			//        {
			//            break;
			//        }
			//        default:
			//        {
			//            context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", "Message type of request message is not Request(0) nor Notification(2)." );
			//            return;
			//        }
			//    }

			//    if ( !requestFields[ nextPosition ].IsTypeOf<string>().GetValueOrDefault() )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Method of request message (ID:{0}) is not raw. ", context.MessageId ) );
			//        return;
			//    }

			//    try
			//    {
			//        context.MethodName = requestFields[ nextPosition ].AsString( Constants.MethodEncoding );
			//    }
			//    catch ( InvalidOperationException ex )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Message ID:{0}: {1}", context.MessageId, ex.Message ) );
			//        return;
			//    }

			//    nextPosition++;

			//    if ( !requestFields[ nextPosition ].IsTypeOf<IList<MessagePackObject>>().GetValueOrDefault() )
			//    {
			//        context.SerializationError = new RpcErrorMessage( RpcError.MessageRefusedError, "Invalid message.", String.Format( CultureInfo.CurrentCulture, "Arguments of request message (ID:{0}) is not array.", context.MessageId ) );
			//        return;
			//    }

			//    context.Arguments = requestFields[ nextPosition ].AsList();
			//}
		}
	}
}
