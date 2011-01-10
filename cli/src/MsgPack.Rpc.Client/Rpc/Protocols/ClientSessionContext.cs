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
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Collections.Concurrent;
using MsgPack.Collections;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Defines interfaces of low-level transportation context which uses some Berkley-Socket.
	/// </summary>
	public sealed class ClientSessionContext
	{
		private readonly ClientSocketAsyncEventArgs _socketContext;

		internal ClientSocketAsyncEventArgs SocketContext
		{
			get { return this._socketContext; }
		}

		private readonly CancellationToken _cancellationToken;

		public CancellationToken CancellationToken
		{
			get { return _cancellationToken; }
		}

		private readonly ClientTransport _transport;

		public ClientTransport Transport
		{
			get { return this._transport; }
		} 

		public ConnectionOrientedClientTransport ConnectionOrientedTransport
		{
			get { return this._transport as ConnectionOrientedClientTransport; }
		}

		public SocketError SocketError
		{
			get { return this._socketContext.SocketError; }
		}

		private readonly ChunkBuffer _buffer;

		public ChunkBuffer Buffer
		{
			get { return this._buffer; }
		} 

		public RpcClientOptions Options
		{
			get { return this._socketContext.Options; }
		}
		
		internal ClientSessionContext( ClientSocketAsyncEventArgs underlying, /*FeedingUnpacker unpacker,*/ ClientTransport transport, /*Func<ClientSessionContext, bool> onResponse,*/ CancellationToken cancellationToken )
		{
			Contract.Assert( underlying != null );
			Contract.Assert( transport != null );

			this._socketContext = underlying;
			underlying.SetSessionContext( this );
			this._transport = transport;
			this._cancellationToken = cancellationToken;
			this._buffer = ClientServices.RpcBufferFactory.Create( underlying.Options );
		}
		
		public bool SendAsync()
		{
			return !this._socketContext.ConnectSocket.SendAsync( this._socketContext );
		}

		public bool SendToAsync()
		{
			return !this._socketContext.ConnectSocket.SendToAsync( this._socketContext );
		}

		public void OnSent()
		{
			this._socketContext.OnSent();
		}

		public bool ReceiveAsync()
		{
			return this._socketContext.ConnectSocket.ReceiveAsync( this._socketContext );
		}

		public bool ReceiveFromAsync()
		{
			return this._socketContext.ConnectSocket.ReceiveFromAsync( this._socketContext );
		}

		public void OnReceived()
		{
			this._socketContext.OnReceived();
		}
	}
}
