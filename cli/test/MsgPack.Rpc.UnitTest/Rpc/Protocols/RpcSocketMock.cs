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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MsgPack.Rpc.Protocols
{
	public sealed class RpcSocketMock : RpcSocket
	{
		private bool _isConnected;
		private bool _isAcceptted;
		private bool _isListening;

		private readonly Socket sock;

		public override RpcTransportProtocol Protocol
		{
			get { throw new NotImplementedException(); }
		}

		public override EndPoint RemoteEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		public override EndPoint LocalEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		protected override bool ConnectAsyncCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		protected override bool AcceptAsyncCore( RpcSocketAsyncEventArgs e )
		{
			if ( this._isConnected )
			{
				throw new InvalidOperationException("Socket is connected to remote endpoint.");
			}

			if ( !this._isListening )
			{
				throw new InvalidOperationException( "Socket does not listen to remote connection request." );
			}

			throw new NotImplementedException();
		}

		protected override bool SendAsyncCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		protected override bool SendToAsyncCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		protected override bool ReceiveAsyncCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		protected override bool ReceiveFromAsyncCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		protected override int ReceiveCore( RpcSocketAsyncEventArgs e )
		{
			throw new NotImplementedException();
		}

		public override void Shutdown( SocketShutdown how )
		{
			throw new NotImplementedException();
		}
	}
}
