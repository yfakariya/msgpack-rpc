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
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;

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

		public RpcSocketAsyncEventArgs CreateSocketContext( EndPoint remoteEndPoint )
		{
			return
				new RpcSocketAsyncEventArgs(
					( e, completedSynchronously ) => BridgeTo<ConnectingContext>( e, completedSynchronously, this.OnConnected ),
					null,
					( e, completedSynchronously ) => BridgeTo<SendingContext>( e, completedSynchronously, this.OnSent ),
					( e, completedSynchronously ) => BridgeTo<ReceivingContext>( e, completedSynchronously, this.OnReceived ),
					this.HandleError,
					this.CancellationToken,
					ClientServices.SocketFactory
				) { RemoteEndPoint = remoteEndPoint };
		}
		
		/// <summary>
		///		Bridge callback of <see cref="RpcSocketAsyncEventArgs"/> to method of this class.
		/// </summary>
		/// <typeparam name="T">Expected concrete type of <see cref="RpcSocketAsyncEventArgs"/>.</typeparam>
		/// <param name="e">Argument of callback.</param>
		/// <param name="handler">Handler of this class.</param>
		private static void BridgeTo<T>( RpcSocketAsyncEventArgs e, bool completedSynchronously, Action<T, bool> handler )
		{
			Contract.Assume( e != null, "e != null, handler:" + handler.Method );
			Contract.Assume( e.UserToken != null, "e.UserToken(<" + typeof( T ).FullName + "> is null. handler:" + handler.Method );
			Contract.Assume( e.UserToken is T, "e.UserToken is " + typeof( T ) + ", Actual:" + e.UserToken.GetType().FullName + " handler:" + handler.Method );
			var context = ( T )e.UserToken;
			Contract.Assume( context != null );
			e.UserToken = null;
			handler( context, completedSynchronously );
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
		protected internal void Connect( ConnectingContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			this.VerifyIsNotDisposed();

			Contract.EndContractBlock();

			context.Client.SocketContext.UserToken = context;
			this.ConnectCore( context );
		}

		/// <summary>
		///		Initiate asynchrnous connecting operation.
		/// </summary>
		/// <param name="context">
		///		Context information.
		/// </param>
		protected abstract void ConnectCore( ConnectingContext context );

		/// <summary>
		///		Respond to nofication to finish asynchronous connecting operation via context.
		/// </summary>
		/// <param name="context">Context which notifies this callback.</param>
		/// <param name="completedSynchronously">If connecting is completed synchrnously then true.</param>
		/// <remarks>
		///		When a derived class override this method, it must call base implementation of this method.
		/// </remarks>
		protected virtual void OnConnected( ConnectingContext context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			context.Client.OnConnected( context, completedSynchronously, context.UserAsyncState );

			//Contract.Assume( context.Transport.SessionContext != null );

			//var receivingContext =
			//    new ReceivingContext(
			//        context.Transport.SessionContext,
			//        new RpcInputBuffer(
			//            ChunkBuffer.CreateDefault(),
			//            ( buffer, state ) => this.FeedMore( state as RpcSocketAsyncEventArgs ),
			//            context.SocketContext
			//        )
			//    );

			//this.RegisterTransportReceiveCallback(
			//    context.Transport,
			//    receivingContext
			//);

			//if ( !receivingContext.SocketContext.ConnectSocket.ReceiveAsync( receivingContext.SocketContext ) )
			//{
			//    this.OnReceived( receivingContext, true );
			//}
		}

		#endregion -- Connect --

		#region -- Send --

		public void Send( SendingContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			Contract.Assume( context.SocketContext.UserToken == null );
			context.SocketContext.UserToken = context;
			this.SendCore( context );
		}

		protected abstract void SendCore( SendingContext context );

		protected virtual void OnSent( SendingContext context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			if ( context.SocketContext.SocketError != SocketError.Success )
			{
				if ( context.MessageId != null )
				{
					this.HandleError( context.SocketContext.LastOperation, context.MessageId.Value, context.SocketContext.SocketError );
				}
				else
				{
					context.OnMessageSent( new SocketException( ( int )context.SocketContext.SocketError ), completedSynchronously );
				}

				return;
			}

			//var buffer = context.SocketContext.BufferList as IDisposable;
			//if ( buffer != null )
			//{
			//    try { }
			//    finally
			//    {
			//        context.SocketContext.BufferList = null;
			//        buffer.Dispose();
			//    }
			//}

			if ( context.MessageId == null )
			{
				context.OnMessageSent( null, completedSynchronously );
			}
			else
			{
				this.Receive(
					new ReceivingContext(
						context.SessionContext,
						new RpcInputBuffer(
							context.SendingBuffer.Chunks,
							( _, state ) => this.FeedMore( state as RpcSocketAsyncEventArgs ),
							context.SocketContext
						),
						new Unpacker()
					)
				);
			}
		}


		public void SendTo( SendingContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			context.SocketContext.UserToken = context;
			this.SendToCore( context );
		}

		protected abstract void SendToCore( SendingContext context );

		#endregion  -- Send --

		#region  -- Receive --

		protected internal void Receive( ReceivingContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			context.SocketContext.UserToken = context;
			this.ReceiveCore( context );
		}

		protected abstract void ReceiveCore( ReceivingContext context );

		protected virtual void OnReceived( ReceivingContext context, bool completedSynchronously )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			// TODO: transport should dispose Buffer .
			//var buffer = context.SocketContext.BufferList as IDisposable;
			//if ( buffer != null )
			//{
			//    try { }
			//    finally
			//    {
			//        context.SocketContext.BufferList = null;
			//        buffer.Dispose();
			//    }
			//}
		}

		protected abstract BufferFeeding FeedMore( RpcSocketAsyncEventArgs context );

		public void ReceiveFrom( ReceivingContext context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();
			context.SocketContext.UserToken = context;
			this.ReceiveFromCore( context );
		}

		protected abstract void ReceiveFromCore( ReceivingContext context );

		#endregion  -- Receive --
	}
}
