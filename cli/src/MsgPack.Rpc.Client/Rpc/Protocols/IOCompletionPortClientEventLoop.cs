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
using System.Net;
using System.Collections.Concurrent;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		<see cref="ClientEventLoop"/> implementation taking into account of I/O Completion Port of Windows NT Kernel.
	/// </summary>
	public sealed class IOCompletionPortClientEventLoop : ClientEventLoop
	{
		public IOCompletionPortClientEventLoop( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource )
			: base( options, errorHandler, cancellationTokenSource )
		{
		}

		protected override void ConnectCore( ConnectingClientSocketAsyncEventArgs context )
		{
			if ( !context.Transport.Socket.ConnectAsync( context ) )
			{
				this.OnConnected( context, true );
			}
		}

		protected sealed override void OnConnected( ConnectingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			Contract.Assume( context is ClientSocketAsyncEventArgs );

			if ( context.ConnectByNameError != null )
			{
				// error 
				( context as ClientSocketAsyncEventArgs ).ConnectionOrientedTransport.OnConnectError( RpcError.NetworkUnreacheableError, context.ConnectByNameError, completedSynchronously, context.InternalUserToken );
				return;
			}

			base.OnConnected( context, completedSynchronously );
		}

		protected sealed override void SendCore( SendingClientSocketAsyncEventArgs context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
			{
				// TODO : trace
				return;
			}

			if ( !context.SendAsync() )
			{
				context.OnSent();
			}
		}

		protected sealed override void OnSent( SendingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			base.OnSent( context, completedSynchronously );
		}

		protected sealed override void SendToCore( SendingClientSocketAsyncEventArgs context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
			{
				// TODO : trace
				return;
			}

			if ( !context.SendToAsync() )
			{
				context.OnSent();
			}
		}

		protected sealed override void ReceiveCore( ReceivingClientSocketAsyncEventArgs context )
		{
			if ( !context.ReceiveAsync() )
			{
				context.OnReceived();
			}
		}

		protected sealed override void ReceiveFromCore( ReceivingClientSocketAsyncEventArgs context )
		{
			if ( !context.ReceiveFromAsync() )
			{
				context.OnReceived();
			}
		}

		protected sealed override void OnReceived( ReceivingClientSocketAsyncEventArgs context, bool completedSynchronously )
		{
			if ( context.SocketError != SocketError.Success )
			{
				this.HandleError( context.LastOperation, context.SocketError );
				return;
			}

			var receivingContext = context as ReceivingClientSocketAsyncEventArgs;
			Contract.Assume( receivingContext != null );

			if ( !context.CancellationToken.IsCancellationRequested )
			{
				receivingContext.Transport.Receive( receivingContext );
			}

			base.OnReceived( context, completedSynchronously );
		}

		protected sealed override int FeedMore( ReceivingClientSocketAsyncEventArgs context )
		{
			return context.ConnectSocket.Receive( context );
		}

		protected sealed override void RegisterTransportConnectCallback( ConnectionOrientedClientTransport transport, ConnectingClientSocketAsyncEventArgs context, object asyncState )
		{
			this.Connect( context, asyncState );
		}

		private readonly ConcurrentDictionary<ClientTransport, ReceivingClientSocketAsyncEventArgs> _receiveCallbackTable = new ConcurrentDictionary<ClientTransport, ReceivingClientSocketAsyncEventArgs>();

		protected sealed override void RegisterTransportReceiveCallback( ClientTransport transport, ReceivingClientSocketAsyncEventArgs context )
		{
			if ( !this._receiveCallbackTable.TryAdd( transport, context ) )
			{
				throw new InvalidOperationException( transport + " is already registered." );
			}

			this.Receive( context );
		}

		protected sealed override void UnregisterTransportConnectCallback( ConnectionOrientedClientTransport transport )
		{
			// nop.
		}

		protected sealed override void UnregisterTransportReceiveCallback( ClientTransport transport )
		{
			ReceivingClientSocketAsyncEventArgs context;
			if ( this._receiveCallbackTable.TryRemove( transport, out context ) )
			{
				context.ConnectSocket.Shutdown( SocketShutdown.Receive );
			}
		}
	}
}