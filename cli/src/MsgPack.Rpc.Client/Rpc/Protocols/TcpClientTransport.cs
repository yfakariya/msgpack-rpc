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
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	// TODO: Extract abstract class and implemnt UDP Transport.
	/// <summary>
	///		TCP binding of <see cref="IClinentTransport"/>.
	/// </summary>
	internal sealed class TcpClientTransport : ConnectionOrientedClientTransport
	{
		private readonly ManualResetEventSlim _connectedDone;
		private readonly Action<ConnectingClientSocketAsyncEventArgs, bool, object> _onConnected;
		private readonly Action<RpcError, Exception, bool, object> _onConnectError;
		private volatile Tuple<RpcError, Exception> _connectError;
		private TimeSpan _connectTimeout;
		private bool _disposed;

		public TcpClientTransport( RpcSocket socket, ClientEventLoop eventLoop, RpcClientOptions options, Action<ConnectingClientSocketAsyncEventArgs, bool, object> onConnected, Action<RpcError, Exception, bool, object> onConnectError )
			: base( socket, eventLoop, options )
		{
			if ( socket.Protocol.ProtocolType != ProtocolType.Tcp )
			{
				throw new ArgumentException( "socket must be connected TCP socket.", "socketContext" );
			}

			Contract.EndContractBlock();

			this._connectedDone = new ManualResetEventSlim();
			this._onConnected = onConnected;
			this._onConnectError = onConnectError;
			this._connectTimeout = options == null ? TimeSpan.FromSeconds( 5 ) : options.ConnectTimeout ?? TimeSpan.FromSeconds( 5 );
		}

		protected sealed override void Dispose( bool disposing )
		{
			bool disposed = this._disposed;
			if ( !disposed )
			{
				this.Socket.Shutdown( SocketShutdown.Both );
				this.Drain();
				this._connectedDone.Dispose();
			}

			this._disposed = true;
			base.Dispose( disposing );
		}

		protected sealed override void OnConnectedCore( ConnectingClientSocketAsyncEventArgs newContext, bool completedSynchronously, object asyncState )
		{
			this._connectError = null;
			this._connectedDone.Set();
			if ( this._onConnected != null )
			{
				this._onConnected( newContext, completedSynchronously, asyncState );
			}
		}

		protected sealed override void OnConnectErrorCore( RpcError error, Exception exception, bool completedSynchronously, object asyncState )
		{
			this._connectError = Tuple.Create( error, exception );
			this._connectedDone.Set();
			if ( this._onConnectError != null )
			{
				this._onConnectError( error, exception, completedSynchronously, asyncState );
			}
		}

		protected sealed override void SendCore( SendingClientSocketAsyncEventArgs context )
		{
			if ( !this._connectedDone.IsSet )
			{
				this._connectedDone.Wait( this._connectTimeout );
			}

			if ( this._connectError != null )
			{
				new RpcTransportException( this._connectError.Item1, "Failed to connect destination.", null, this._connectError.Item2 );
			}

			this.EventLoop.Send( context );
		}
	}
}
