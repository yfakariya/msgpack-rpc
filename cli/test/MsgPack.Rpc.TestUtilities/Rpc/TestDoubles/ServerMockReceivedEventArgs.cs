﻿#region -- License Terms --
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
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MsgPack.Rpc.Protocols;
using MsgPack.Collections;
using MsgPack.Rpc.Serialization;
using System.IO;

namespace MsgPack.Rpc.TestDoubles
{
	public sealed class ServerMockReceivedEventArgs : EventArgs
	{
		private readonly SocketAsyncEventArgs _context;
		private readonly SocketException _socketException;
		private RequestMessage? _request;
		private RpcException _deserializationError;

		public RequestMessage GetRequest()
		{
			if ( this._socketException != null )
			{
				throw this._socketException;
			}

			if ( this._request != null )
			{
				return this._request.Value;
			}

			if ( this._deserializationError != null )
			{
				throw this._deserializationError;
			}

			using ( var buffer = GCChunkBuffer.CreateDefault() )
			{
				buffer.Feed( new ArraySegment<byte>( this._context.Buffer, this._context.Offset, this._context.BytesTransferred ) );
				using ( RpcInputBuffer rpcBuffer = new RpcInputBuffer( buffer, ( _0, _1 ) => new BufferFeeding( 0 ), null ) )
				{
					RequestMessage message;
					var error =
						new RequestMessageSerializer(
								Arrays<IFilterProvider<RequestMessageSerializationFilter>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
								Arrays<IFilterProvider<RequestMessageDeserializationFilter>>.Empty,
								null
						).Deserialize( rpcBuffer, out message );
					if ( error.IsSuccess )
					{
						this._request = message;
						return message;
					}
					else
					{
						this._deserializationError = error.ToException();
						throw this._deserializationError;
					}
				}
			}
		}

		public void Reply( int? id, MessagePackObject message )
		{
			if ( id == null )
			{
				throw new ArgumentException( "id must be set.", "id" );
			}

			if ( this._context.AcceptSocket == null )
			{
				throw new InvalidOperationException();
			}

			using ( var buffer = GCChunkBuffer.CreateDefault() )
			{
				using ( RpcOutputBuffer rpcBuffer = new RpcOutputBuffer( buffer ) ) 
				{
					var error =
						new ResponseMessageSerializer(
								Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
								Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
								null
						).Serialize( id.Value, message, false, null, rpcBuffer );
					if ( !error.IsSuccess )
					{
						throw error.ToException();
					}

					this._context.AcceptSocket.Send( rpcBuffer.ReadBytes().ToArray() );
				}
			}
		}

		public void Reply( int? id, RpcException message )
		{
			if ( id == null )
			{
				throw new ArgumentException( "id must be set.", "id" );
			}

			if ( this._context.AcceptSocket == null )
			{
				throw new InvalidOperationException();
			}

			using ( var buffer = GCChunkBuffer.CreateDefault() )
			{
				using ( RpcOutputBuffer rpcBuffer = new RpcOutputBuffer( buffer ) )
				{
					var error =
						new ResponseMessageSerializer(
								Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
								Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
								Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
								null
						).Serialize( id.Value, null, false, message, rpcBuffer );
					if ( !error.IsSuccess )
					{
						throw error.ToException();
					}

					this._context.AcceptSocket.Send( rpcBuffer.ReadBytes().ToArray() );
				}
			}
		}

		internal ServerMockReceivedEventArgs( SocketAsyncEventArgs context )
		{
			this._context = context;
			if ( context.SocketError != SocketError.Success )
			{
				this._socketException = new SocketException( ( int )context.SocketError );
			}
			else
			{
				this._socketException = null;
			}
		}
	}
}
