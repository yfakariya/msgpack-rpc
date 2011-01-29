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
	public sealed class ClientSessionContext
	{
		private readonly RpcSocketAsyncEventArgs _socketContext;

		public RpcSocketAsyncEventArgs SocketContext
		{
			get { return this._socketContext; }
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

		private object _userToken;

		public object UserToken
		{
			get { return this._userToken; }
			set { this._userToken = value; }
		}

		public ClientSessionContext(
			ClientTransport transport,
			RpcSocketAsyncEventArgs socketContext
		)
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( socketContext == null )
			{
				throw new ArgumentNullException( "socketContext" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._socketContext = socketContext;
		}
	}

	//public struct ConnectingContext : IEquatable<ConnectingContext>
	//{
	//    private readonly ClientSessionContext _sessionContext;

	//    public ClientSessionContext SessionContext
	//    {
	//        get { return this._sessionContext; }
	//    }

	//    internal ConnectingContext( ClientSessionContext sessionContext )
	//    {
	//        this._sessionContext = sessionContext;
	//    }

	//    public override bool Equals( object obj )
	//    {
	//        if ( obj == null )
	//        {
	//            return false;
	//        }

	//        if ( !( obj is ConnectingContext ) )
	//        {
	//            return false;
	//        }
	//    }

	//    public bool Equals( ConnectingContext other )
	//    {
	//        return this._sessionContext == other._sessionContext;
	//    }
	//}

	//public sealed class SendingClientSocketAsyncEventArgs : ClientSessionContext
	//{
	//    private readonly RpcOutputBuffer _sendingBuffer;

	//    public RpcOutputBuffer SendingBuffer
	//    {
	//        get { return this._sendingBuffer; }
	//    }

	//    private readonly int? _messageId;

	//    public int? MessageId
	//    {
	//        get { return this._messageId; }
	//    }

	//    private readonly Action<SendingClientSocketAsyncEventArgs, Exception, bool> _onMessageSent;

	//    public SendingClientSocketAsyncEventArgs(
	//        ClientTransport transport,
	//        int? messageId,
	//        RpcClientOptions options,
	//        Action<RpcSocketAsyncEventArgs> onConnected,
	//        Action<RpcSocketAsyncEventArgs> onSent,
	//        Action<RpcSocketAsyncEventArgs> onReceived,
	//        Action<SendingClientSocketAsyncEventArgs, Exception, bool> onMesageSent,
	//        Action<SocketAsyncOperation, SocketError> onError,
	//        CancellationToken cancellationToken,
	//        Func<Socket, RpcSocket> socketFactory
	//    )
	//        : base(
	//            transport,
	//            options,
	//            onConnected,
	//            onSent,
	//            onReceived,
	//            onError,
	//            cancellationToken,
	//            socketFactory
	//        )
	//    {
	//        this._messageId = messageId;
	//        this._onMessageSent = onMesageSent;
	//        this._sendingBuffer = new RpcOutputBuffer();
	//        this.BufferList = this._sendingBuffer.Chunks;
	//    }

	//    internal void OnMessageSent( Exception error, bool completedSynchronously )
	//    {
	//        var handler = this._onMessageSent;
	//        if ( handler != null )
	//        {
	//            handler( this, error, completedSynchronously );
	//        }
	//    }
	//}

	//public sealed class ReceivingClientSocketAsyncEventArgs : ClientSessionContext
	//{
	//    private readonly RpcInputBuffer _receivingBuffer;

	//    public RpcInputBuffer ReceivingBuffer
	//    {
	//        get { return this._receivingBuffer; }
	//    }

	//    private readonly Func<ReceivingClientSocketAsyncEventArgs, int> _feeding;

	//    public ReceivingClientSocketAsyncEventArgs(
	//        ClientTransport transport,
	//        RpcClientOptions options,
	//        Action<RpcSocketAsyncEventArgs> onConnected,
	//        Action<RpcSocketAsyncEventArgs> onSent,
	//        Action<RpcSocketAsyncEventArgs> onReceived,
	//        Action<SocketAsyncOperation, SocketError> onError,
	//        CancellationToken cancellationToken,
	//        Func<Socket, RpcSocket> socketFactory,
	//        Func<ReceivingClientSocketAsyncEventArgs, int> feeding
	//    )
	//        : base(
	//           transport,
	//           options,
	//           onConnected,
	//           onSent,
	//           onReceived,
	//           onError,
	//           cancellationToken,
	//           socketFactory
	//            )
	//    {
	//        if ( feeding == null )
	//        {
	//            throw new ArgumentNullException( "feeding" );
	//        }

	//        Contract.EndContractBlock();

	//        this._feeding = feeding;

	//        this._receivingBuffer = new RpcInputBuffer( ChunkBuffer.CreateDefault(), this.Transport.ChunkSize, this.Feed );
	//        this.BufferList = this._receivingBuffer.Chunks;
	//    }

	//    private BufferFeeding Feed( ChunkBuffer chunkBuffer )
	//    {
	//        return new BufferFeeding( this._feeding( this ) );
	//    }
	//}
}
