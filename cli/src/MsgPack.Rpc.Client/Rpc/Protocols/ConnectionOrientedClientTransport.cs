using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Net;

namespace MsgPack.Rpc.Protocols
{
	public abstract class ConnectionOrientedClientTransport : ClientTransport
	{
		private RpcSocket _connectedSocket;
		public RpcSocket ConnectedSocket
		{
			get { return this._connectedSocket; }
		}

		protected ConnectionOrientedClientTransport( RpcSocket socket, ClientEventLoop eventLoop, RpcClientOptions options )
			: base( socket, eventLoop, options ) { }

		protected override void Dispose( bool disposing )
		{
			// TODO: trace
			this.EventLoop.UnregisterTransport( this );
			base.Dispose( disposing );
		}

		public void Connect( EndPoint remoteEndPoint, ConnectingClientSocketAsyncEventArgs newContext, object asyncState )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			if ( newContext == null )
			{
				throw new ArgumentNullException( "newContext" );
			}

			if ( newContext.RemoteEndPoint != null )
			{
				throw new ArgumentException( "RemoteEndPoint already set.", "newContext" );
			}

			Contract.EndContractBlock();

			newContext.RemoteEndPoint = remoteEndPoint;
			this.EventLoop.RegisterTransport( remoteEndPoint, this, asyncState );
		}

		public void OnConnected( ConnectingClientSocketAsyncEventArgs newContext, bool completedSynchronously, object asyncState )
		{
			if ( newContext == null )
			{
				throw new ArgumentNullException( "newContext" );
			}

			if ( newContext.ConnectSocket == null )
			{
				throw new ArgumentException( "newContext must have valid connect socket.", "newContext" );
			}

			Contract.EndContractBlock();

			this._connectedSocket = newContext.ConnectSocket;
			this.OnConnectedCore( newContext, completedSynchronously, asyncState );
		}

		protected abstract void OnConnectedCore( ConnectingClientSocketAsyncEventArgs newContext, bool completedSynchronously, object asyncState );

		public void OnConnectError( RpcError error, Exception exception, bool completedSynchronously, object asyncState )
		{
			if ( error == null )
			{
				throw new ArgumentNullException( "error" );
			}

			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			Contract.EndContractBlock();

			this.OnConnectErrorCore( error, exception, completedSynchronously, asyncState );
		}

		protected abstract void OnConnectErrorCore( RpcError error, Exception exception, bool completedSynchronously, object asyncState );
	}
}
