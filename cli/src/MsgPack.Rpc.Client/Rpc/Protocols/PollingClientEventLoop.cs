using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using MsgPack.Collections.Concurrent;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace MsgPack.Rpc.Protocols
{
	public sealed class PollingClientEventLoop : ClientEventLoop
	{
		private readonly BlockingCollection<ClientSocketAsyncEventArgs> _dummy;
		private readonly NotifiableBlockingCollection<ClientSocketAsyncEventArgs> _connectingQueue;
		private readonly NotifiableBlockingCollection<ClientSocketAsyncEventArgs> _sendingQueue;
		private readonly Thread _inBoundPollingThread;

		public PollingClientEventLoop( RpcClientOptions options, EventHandler<RpcTransportErrorEventArgs> errorHandler, CancellationTokenSource cancellationTokenSource )
			: base( options, errorHandler, cancellationTokenSource )
		{
			throw new NotImplementedException();
		}
		
		protected sealed override void ConnectCore( ConnectingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void SendCore( SendingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void ReceiveCore( ReceivingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override int FeedMore( ReceivingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void SendToCore( SendingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void ReceiveFromCore( ReceivingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void RegisterTransportReceiveCallback( ClientTransport transport, ReceivingClientSocketAsyncEventArgs context )
		{
			throw new NotImplementedException();
		}

		protected sealed override void RegisterTransportConnectCallback( ConnectionOrientedClientTransport transport, ConnectingClientSocketAsyncEventArgs context, object asyncState )
		{
			throw new NotImplementedException();
		}

		protected sealed override void UnregisterTransportReceiveCallback( ClientTransport transport )
		{
			throw new NotImplementedException();
		}

		protected sealed override void UnregisterTransportConnectCallback( ConnectionOrientedClientTransport transport )
		{
			throw new NotImplementedException();
		}
	}
}
