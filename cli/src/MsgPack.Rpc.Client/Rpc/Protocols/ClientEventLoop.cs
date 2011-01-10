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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Provides common interfaces and basic features of client side <see cref="EventLoop"/>.
	/// </summary>
	public abstract class ClientEventLoop : EventLoop
	{
		private readonly RpcClientOptions _options;

		private bool _isDisposed;

		#region -- Cancellation --

		private readonly CancellationTokenSource _cancellationTokenSource;

		/// <summary>
		///		Get <see cref="CancellationToken"/> token to listen request to cancel all pending events on this loop.
		/// </summary>
		/// <value>
		///		<see cref="CancellationToken"/> to listen request to cancel all pending events on this loop.
		/// </value>
		public CancellationToken CancellationToken
		{
			get { return this._cancellationTokenSource.Token; }
		}

		/// <summary>
		///		Cancel all pending events on this loop.
		/// </summary>
		public void Cancel()
		{
			this._cancellationTokenSource.Cancel();
		}

		#endregion -- Cancellation --

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="options">
		///		Client option configuration. This value may be null.
		///	</param>
		/// <param name="errorHandler">
		///		Initial event handler of <see cref="TransportError"/>. This handler may be null.
		///	</param>
		/// <param name="cancellationTokenSource">
		///		<see cref="CancellationTokenSource"/> to cancel all pending events on this eventloop. This value may be null.
		/// </param>
		protected ClientEventLoop( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource )
			: base( errorHandler )
		{
			this._options = options;
			this._cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
		}

		/// <summary>
		///		Cleanup unamanged resources and optionally managed resources.
		/// </summary>
		/// <param name="disposing">
		///		To cleanup managed resources too, then true.
		/// </param>
		protected override void Dispose( bool disposing )
		{
			if ( !this._isDisposed )
			{
				this._cancellationTokenSource.Cancel();
				this._cancellationTokenSource.Dispose();
				this._isDisposed = true;
			}

			base.Dispose( disposing );
		}

		private void VerifyIsNotDisposed()
		{
			if ( this._isDisposed )
			{
				throw new ObjectDisposedException( this.GetType().FullName );
			}
		}

		#region -- Context Factory --

		/// <summary>
		///		Create new <see cref="ConnectingClientSocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="transport">
		///		<see cref="ConnectionOrientedClientTransport"/> which is responsible for this session.
		///	</param>
		/// <param name="remoteEndPoint">
		///		<see cref="EndPoint"/> to be connected.
		///	</param>
		/// <returns>
		///		New <see cref="ConnectingClientSocketAsyncEventArgs"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		<paramref name="transport"/> or <paramref name="remoteEndPoint"/> is null.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance has been disposed.
		///	</exception>
		public ConnectingClientSocketAsyncEventArgs CreateConnectingContext( ConnectionOrientedClientTransport transport, EndPoint remoteEndPoint )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			this.VerifyIsNotDisposed();

			Contract.EndContractBlock();

			return
				new ConnectingClientSocketAsyncEventArgs(
					transport,
					this._options,
					e => BridgeTo<ConnectingClientSocketAsyncEventArgs>( e, this.OnConnected ),
					e => BridgeTo<SendingClientSocketAsyncEventArgs>( e, this.OnSent ),
					e => BridgeTo<ReceivingClientSocketAsyncEventArgs>( e, this.OnReceived ),
					this.HandleError,
					this.CancellationToken,
					ClientServices.SocketFactory
				) { RemoteEndPoint = remoteEndPoint };
		}

		/// <summary>
		///		Create new <see cref="SendingClientSocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="transport">
		///		<see cref="ClientTransport"/> which is responsible for this session.
		///	</param>
		///	<param name="messageId">
		///		Message ID of the session. If message is notification message, specify null.
		///	</param>
		///	<param name="onMessageSent">
		///		Callback will be called when message is sent anyway. 
		///		1st parameter is returning <see cref="SendingClientSocketAsyncEventArgs"/> itself,
		///		2nd parameter is occurred <see cref="Exception"/> when failed to sent,
		///		and 3rd parameter indicates operation is completed synchronously or not.
		///	</param>
		/// <returns>
		///		New <see cref="SendingClientSocketAsyncEventArgs"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is null.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance has been disposed.
		///	</exception>
		public SendingClientSocketAsyncEventArgs CreateSendingContext( ClientTransport transport, int? messageId, Action<SendingClientSocketAsyncEventArgs, Exception, bool> onMessageSent )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			this.VerifyIsNotDisposed();

			Contract.EndContractBlock();

			return
				new SendingClientSocketAsyncEventArgs(
					transport,
					messageId,
					this._options,
					e => BridgeTo<ConnectingClientSocketAsyncEventArgs>( e, this.OnConnected ),
					e => BridgeTo<SendingClientSocketAsyncEventArgs>( e, this.OnSent ),
					e => BridgeTo<ReceivingClientSocketAsyncEventArgs>( e, this.OnReceived ),
					onMessageSent,
					this.HandleError,
					this.CancellationToken,
					ClientServices.SocketFactory
				);
		}

		/// <summary>
		///		Create new <see cref="ReceivingClientSocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="transport">
		///		<see cref="ClientTransport"/> which is responsible for all sessions.
		///	</param>
		/// <returns>
		///		New <see cref="ReceivingClientSocketAsyncEventArgs"/>.
		///	</returns>
		///	<exception cref="ArgumentNullException">
		///		<paramref name="transport"/> is null.
		///	</exception>
		///	<exception cref="ObjectDisposedException">
		///		This instance has been disposed.
		///	</exception>
		public ReceivingClientSocketAsyncEventArgs CreateReceivingContext( ClientTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			this.VerifyIsNotDisposed();

			Contract.EndContractBlock();

			return
				new ReceivingClientSocketAsyncEventArgs(
					transport,
					this._options,
					e => BridgeTo<ConnectingClientSocketAsyncEventArgs>( e, this.OnConnected ),
					e => BridgeTo<SendingClientSocketAsyncEventArgs>( e, this.OnSent ),
					e => BridgeTo<ReceivingClientSocketAsyncEventArgs>( e, this.OnReceived ),
					this.HandleError,
					this.CancellationToken,
					ClientServices.SocketFactory,
					this.FeedMore
				);
		}

		/// <summary>
		///		Bridge callback of <see cref="RpcSocketAsyncEventArgs"/> to method of this class.
		/// </summary>
		/// <typeparam name="T">Expected concrete type of <see cref="RpcSocketAsyncEventArgs"/>.</typeparam>
		/// <param name="e">Argument of callback.</param>
		/// <param name="handler">Handler of this class.</param>
		private static void BridgeTo<T>( RpcSocketAsyncEventArgs e, Action<T, bool> handler )
			where T : RpcSocketAsyncEventArgs
		{
			Contract.Assume( e != null );
			var context = e as T;
			Contract.Assume( context != null );
			handler( context, false );
		}

		#endregion -- Context Factory --

		#region -- Connect --
		
		/// <summary>
		///		Initiate asynchrnous connecting operation.
		/// </summary>
		/// <param name="context">
		///		Context information.
		/// </param>
		/// <param name="asyncState">
		///		State object to be passed to <see cref="ConnectionOrientedClientTransport.OnConnected"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="context"/> is null.
		/// </exception>
		/// <remarks>
		///		When to complete connecting operation, 
		///		<see cref="ConnectionOrientedClientTransport.OnConnected"/> will be called via context to notify consumers on upper layers.
		/// </remarks>
		protected internal void Connect( ConnectingClientSocketAsyncEventArgs context, object asyncState )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			this.VerifyIsNotDisposed();

			Contract.EndContractBlock();

			context.InternalUserToken = asyncState;

			this.ConnectCore( context );
		}

		/// <summary>
		///		Initiate asynchrnous connecting operation.
		/// </summary>
		/// <param name="context">
		///		Context information.
		/// </param>
		protected abstract void ConnectCore( ConnectingClientSocketAsyncEventArgs context );

		/// <summary>
		///		Respond to nofication to finish asynchronous connecting operation via context.
		/// </summary>
		/// <param name="context">Context which notifies this callback.</param>
		/// <param name="completedSynchronously">If connecting is completed synchrnously then true.</param>
		/// <remarks>
		///		When a derived class override this method, it must call base implementation of this method.
		/// </remarks>
		protected virtual void OnConnected( ConnectingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			if ( !context.ConnectionOrientedTransport.ConnectedSocket.ReceiveAsync( context.Transport.ReceivingContext ) )
			{
				this.OnReceived( context.Transport.ReceivingContext, true );
			}

			context.ConnectionOrientedTransport.OnConnected( context, completedSynchronously, context.InternalUserToken );
		}

		#endregion -- Connect --

		#region -- Send --

		public void Send( SendingClientSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			this.SendCore( context );
		}
		
		protected abstract void SendCore( SendingClientSocketAsyncEventArgs context );

		protected virtual void OnSent( SendingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			if ( context.SocketError != SocketError.Success )
			{
				if ( context.MessageId != null )
				{
					this.HandleError( context.LastOperation, context.MessageId.Value, context.SocketError );
				}
				else
				{
					context.OnMessageSent( new SocketException( ( int )context.SocketError ), completedSynchronously );
				}

				return;
			}

			var buffer = context.BufferList as IDisposable;
			if ( buffer != null )
			{
				try { }
				finally
				{
					context.BufferList = null;
					buffer.Dispose();
				}
			}

			if ( context.MessageId == null )
			{
				context.OnMessageSent( null, completedSynchronously );
			}
		}


		public void SendTo( SendingClientSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			this.SendToCore( context );
		}

		protected abstract void SendToCore( SendingClientSocketAsyncEventArgs context );

		#endregion  -- Send --

		#region  -- Receive --

		protected internal void Receive( ReceivingClientSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			this.ReceiveCore( context );
		}

		protected abstract void ReceiveCore( ReceivingClientSocketAsyncEventArgs context );

		protected virtual void OnReceived( ReceivingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			var buffer = context.BufferList as IDisposable;
			if ( buffer != null )
			{
				try { }
				finally
				{
					context.BufferList = null;
					buffer.Dispose();
				}
			}
		}

		protected abstract int FeedMore( ReceivingClientSocketAsyncEventArgs context );

		public void ReceiveFrom( ReceivingClientSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			this.ReceiveFromCore( context );
		}

		protected abstract void ReceiveFromCore( ReceivingClientSocketAsyncEventArgs context );

		#endregion  -- Receive --

		#region -- Registration --

		public void RegisterTransport( EndPoint remoteEndPoint, ClientTransport transport )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			var context = this.CreateReceivingContext( transport );
			context.RemoteEndPoint = remoteEndPoint;
			this.RegisterTransportReceiveCallback( transport, context );
		}

		public void RegisterTransport( EndPoint remoteEndPoint, ConnectionOrientedClientTransport transport, object asyncState )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this.RegisterTransportReceiveCallback( transport, this.CreateReceivingContext( transport ) );
			this.RegisterTransportConnectCallback( transport, this.CreateConnectingContext( transport, remoteEndPoint ), asyncState );
		}

		protected abstract void RegisterTransportReceiveCallback( ClientTransport transport, ReceivingClientSocketAsyncEventArgs context );

		protected abstract void RegisterTransportConnectCallback( ConnectionOrientedClientTransport transport, ConnectingClientSocketAsyncEventArgs context, object asyncState );

		public void UnregisterTransport( ClientTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this.UnregisterTransportReceiveCallback( transport );
		}

		public void UnregisterTransport( ConnectionOrientedClientTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this.UnregisterTransportReceiveCallback( transport );
			this.UnregisterTransportConnectCallback( transport );
		}

		protected abstract void UnregisterTransportReceiveCallback( ClientTransport transport );

		protected abstract void UnregisterTransportConnectCallback( ConnectionOrientedClientTransport transport );
	
		#endregion -- Registration --
	}
}
