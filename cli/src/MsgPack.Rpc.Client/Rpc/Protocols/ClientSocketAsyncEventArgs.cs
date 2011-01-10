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
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Collections;
using MsgPack.Rpc.Serialization;
using MsgPack.Rpc.Services;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Encapselates low-level transportation context which uses async socket.
	/// </summary>
	public abstract class ClientSocketAsyncEventArgs : RpcSocketAsyncEventArgs
	{
		private readonly RpcClientOptions _options;

		public RpcClientOptions Options
		{
			get { return this._options; }
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

		protected ClientSocketAsyncEventArgs(
			ClientTransport transport,
			RpcClientOptions options,
			Action<RpcSocketAsyncEventArgs> onConnected,
			Action<RpcSocketAsyncEventArgs> onSent,
			Action<RpcSocketAsyncEventArgs> onReceived,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory
		)
			: base(
				onConnected,
				null,
				onSent,
				onReceived,
				onError,
				cancellationToken,
				socketFactory
			)
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._options = options;
		}

		//private ClientSessionContext _sessionContext;

		//public ClientSessionContext SessionContext
		//{
		//    get { return this._sessionContext; }
		//}

		//internal void SetSessionContext( ClientSessionContext sessionContext )
		//{
		//    this._sessionContext = sessionContext;
		//}
	}

	public sealed class ConnectingClientSocketAsyncEventArgs : ClientSocketAsyncEventArgs
	{
		public ConnectingClientSocketAsyncEventArgs(
			ClientTransport transport,
			RpcClientOptions options,
			Action<RpcSocketAsyncEventArgs> onConnected,
			Action<RpcSocketAsyncEventArgs> onSent,
			Action<RpcSocketAsyncEventArgs> onReceived,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory
		)
			: base(
				transport,
				options,
				onConnected,
				onSent,
				onReceived,
				onError,
				cancellationToken,
				socketFactory
			) { }
	}

	public sealed class SendingClientSocketAsyncEventArgs : ClientSocketAsyncEventArgs
	{
		private readonly RpcOutputBuffer _sendingBuffer;

		public RpcOutputBuffer SendingBuffer
		{
			get { return this._sendingBuffer; }
		}

		private readonly int? _messageId;

		public int? MessageId
		{
			get { return this._messageId; }
		}

		private readonly Action<SendingClientSocketAsyncEventArgs, Exception, bool> _onMessageSent;

		public SendingClientSocketAsyncEventArgs(
			ClientTransport transport,
			int? messageId,
			RpcClientOptions options,
			Action<RpcSocketAsyncEventArgs> onConnected,
			Action<RpcSocketAsyncEventArgs> onSent,
			Action<RpcSocketAsyncEventArgs> onReceived,
			Action<SendingClientSocketAsyncEventArgs, Exception, bool> onMesageSent,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory
		)
			: base(
				transport,
				options,
				onConnected,
				onSent,
				onReceived,
				onError,
				cancellationToken,
				socketFactory
			)
		{
			this._messageId = messageId;
			this._onMessageSent = onMesageSent;
			this._sendingBuffer = new RpcOutputBuffer();
			this.BufferList = this._sendingBuffer.Chunks;
		}

		internal void OnMessageSent( Exception error, bool completedSynchronously )
		{
			var handler = this._onMessageSent;
			if ( handler != null )
			{
				handler( this, error, completedSynchronously );
			}
		}
	}

	public sealed class ReceivingClientSocketAsyncEventArgs : ClientSocketAsyncEventArgs
	{
		private readonly RpcInputBuffer _receivingBuffer;

		public RpcInputBuffer ReceivingBuffer
		{
			get { return this._receivingBuffer; }
		}

		private readonly Func<ReceivingClientSocketAsyncEventArgs, int> _feeding;

		public ReceivingClientSocketAsyncEventArgs(
			ClientTransport transport,
			RpcClientOptions options,
			Action<RpcSocketAsyncEventArgs> onConnected,
			Action<RpcSocketAsyncEventArgs> onSent,
			Action<RpcSocketAsyncEventArgs> onReceived,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory,
			Func<ReceivingClientSocketAsyncEventArgs, int> feeding
		)
			: base(
			   transport,
			   options,
			   onConnected,
			   onSent,
			   onReceived,
			   onError,
			   cancellationToken,
			   socketFactory
				)
		{
			if ( feeding == null )
			{
				throw new ArgumentNullException( "feeding" );
			}

			Contract.EndContractBlock();

			this._feeding = feeding;

			this._receivingBuffer = new RpcInputBuffer( ChunkBuffer.CreateDefault(), this.ChunkSize, this.Feed );
			this.BufferList = this._receivingBuffer.Chunks;
		}

		private const int _defaultChunkSize = 32 * 1024;

		private int ChunkSize
		{
			get { return this.Options == null ? _defaultChunkSize : this.Options.ChunkSize ?? _defaultChunkSize; }
		}

		private BufferFeeding Feed( ChunkBuffer chunkBuffer )
		{
			return new BufferFeeding( this._feeding( this ) );
		}
	}
}
