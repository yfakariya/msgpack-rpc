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

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Wraps <see cref="SocketAsyncEventArgs"/> for RPC.
	/// </summary>
	public abstract class RpcSocketAsyncEventArgs : SocketAsyncEventArgs
	{
		private readonly Func<Socket, RpcSocket> _socketFactory;
		private readonly Action<RpcSocketAsyncEventArgs> _onConnected;
		private readonly Action<RpcSocketAsyncEventArgs> _onAcceptted;
		private readonly Action<RpcSocketAsyncEventArgs> _onSent;
		private readonly Action<RpcSocketAsyncEventArgs> _onReceived;
		private readonly Action<SocketAsyncOperation, SocketError> _onError;

		private RpcSocket _acceptSocket;

		/// <summary>
		///		Get <see cref="RpcSocket"/> which is set in <see cref="RpcSocket.AcceptAsync"/>.
		/// </summary>
		/// <value>
		///		<see cref="RpcSocket"/> which is set in <see cref="RpcSocket.AcceptAsync"/>.
		/// </value>
		public new RpcSocket AcceptSocket
		{
			get
			{
				if ( base.AcceptSocket == null )
				{
					return null;
				}

				if ( this._acceptSocket == null )
				{
					this._acceptSocket = this._socketFactory( base.AcceptSocket );
				}

				return this._acceptSocket;
			}
		}

		private readonly CancellationToken _cancellationToken;

		/// <summary>
		///		Get cancellation token to cancel asynchronous invocation.
		/// </summary>
		/// <value>
		///		Cancellation token to cancel asynchronous invocation.
		/// </value>
		public CancellationToken CancellationToken
		{
			get { return this._cancellationToken; }
		}

		private RpcSocket _connectSocket;

		/// <summary>
		///		Get <see cref="RpcSocket"/> which is set in <see cref="RpcSocket.ConnectAsync"/>.
		/// </summary>
		/// <value>
		///		<see cref="RpcSocket"/> which is set in <see cref="RpcSocket.ConnectAsync"/>.
		/// </value>
		public new RpcSocket ConnectSocket
		{
			get
			{
				if ( base.ConnectSocket == null )
				{
					return null;
				}

				if ( this._connectSocket == null )
				{
					this._connectSocket = this._socketFactory( base.ConnectSocket );
				}

				return this._connectSocket;
			}
		}

		internal object InternalUserToken
		{
			get;
			set;
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="onConnected">
		///		Callback when asynchronous connect operation is completed.
		/// </param>
		/// <param name="onAcceptted">
		///		Callback when asynchronous accept operation is completed.
		/// </param>
		/// <param name="onSent">
		///		Callback when asynchronous send operation is completed.
		/// </param>
		/// <param name="onReceived">
		///		Callback when asynchronous receive operation is completed.
		/// </param>
		/// <param name="onError">
		///		Callback when asynchronous operation is failed.
		/// </param>
		/// <param name="cancellationToken">
		///		Cancellation token to cancel asynchronous socket callback.
		/// </param>
		/// <param name="socketFactory">
		///		Factory method to wrap <see cref="Socket"/> as <see cref="RpcSocket"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="socketFactory"/> is null.
		/// </exception>
		protected RpcSocketAsyncEventArgs(
			Action<RpcSocketAsyncEventArgs> onConnected,
			Action<RpcSocketAsyncEventArgs> onAcceptted,
			Action<RpcSocketAsyncEventArgs> onSent,
			Action<RpcSocketAsyncEventArgs> onReceived,
			Action<SocketAsyncOperation, SocketError> onError,
			CancellationToken cancellationToken,
			Func<Socket, RpcSocket> socketFactory
		)
		{
			if ( socketFactory == null )
			{
				throw new ArgumentNullException( "socketFactory" );
			}

			Contract.EndContractBlock();

			this._onConnected = onConnected;
			this._onAcceptted = onAcceptted;
			this._onSent = onSent;
			this._onReceived = onReceived;
			this._onError = onError;
			this._cancellationToken = cancellationToken;
			this._socketFactory = socketFactory;
		}
		
		protected sealed override void OnCompleted( SocketAsyncEventArgs e )
		{
			if ( e.SocketError != System.Net.Sockets.SocketError.Success )
			{
				var handler = this._onError;
				if ( handler == null )
				{
					throw new SocketException( ( int )e.SocketError );
				}

				handler( e.LastOperation, e.SocketError );
				return;
			}

			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					var handler = this._onAcceptted;
					if ( handler != null )
					{
						handler( this );
					}

					return;
				}
				case SocketAsyncOperation.Connect:
				{
					var handler = this._onConnected;
					if ( handler != null )
					{
						handler( this );
					}

					return;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendTo:
				{
					var handler = this._onSent;
					if ( handler != null )
					{
						handler( this );
					}

					return;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				{
					var handler = this._onReceived;
					if ( handler != null )
					{
						handler( this );
					}

					return;
				}
			}

			base.OnCompleted( e );
		}

		/// <summary>
		///		Invoke sending callback to client.
		/// </summary>
		public void OnSent()
		{
			this._onSent( this );
		}

		/// <summary>
		///		Send data to connected remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool SendAsync()
		{
			Contract.Assume( this._connectSocket != null );
			return this._connectSocket.SendAsync( this );
		}

		/// <summary>
		///		Send data to specific remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool SendToAsync()
		{
			Contract.Assume( this.RemoteEndPoint != null );
			return this._connectSocket.SendToAsync( this );
		}

		/// <summary>
		///		Invoke receiving callback to client.
		/// </summary>
		public void OnReceived()
		{
			this._onReceived( this );
		}

		/// <summary>
		///		Receive data from connected remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool ReceiveAsync()
		{
			Contract.Assume( this._connectSocket != null );
			return this._connectSocket.ReceiveAsync( this );
		}

		/// <summary>
		///		Receive data from specified remote endpoint asynchronously.
		/// </summary>
		/// <returns>
		///		If operation has been completed synchronously then FALSE.
		/// </returns>
		public bool ReceiveFromAsync()
		{
			Contract.Assume( this.RemoteEndPoint != null );
			return this._connectSocket.ReceiveFromAsync( this );
		}
	}
}
