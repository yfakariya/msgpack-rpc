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
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols.Connection
{
	// FIXME: Implement TransportPool!

	[Obsolete] // Naive connection pooling is useless. 
	public sealed class ConnectionPool
	{
		private enum LeaseStatus
		{
			Uninitialized = 0,
			Initializing,
			Ready,
			InUse
		}

		private readonly ConcurrentDictionary<EndPoint, LeaseStatus> _leasedStatusTable = new ConcurrentDictionary<EndPoint, LeaseStatus>();
		private readonly ConcurrentDictionary<EndPoint, Socket> _pooledSockets = new ConcurrentDictionary<EndPoint, Socket>();
		private readonly ConcurrentDictionary<EndPoint, ManualResetEventSlim> _eventTable = new ConcurrentDictionary<EndPoint, ManualResetEventSlim>();
		//private readonly int _maximum;
		//private readonly int _minimum;

		public ConnectionPool()
		{
			// FIXME : improvement
		}

		public RpcClientConnection Borrow( EndPoint remoteEndPoint, RpcTransportProtocol transportProtocol )
		{
			if ( remoteEndPoint == null )
			{
				throw new ArgumentNullException( "remoteEndPoint" );
			}

			Contract.EndContractBlock();

			// TODO: stat based strategy
			// TODO: timeout
			this._eventTable.GetOrAdd( remoteEndPoint, _ => new ManualResetEventSlim( true ) ).Wait();

			var socket = this._pooledSockets.GetOrAdd( remoteEndPoint, _ => new Socket( transportProtocol.AddressFamily, transportProtocol.SocketType, transportProtocol.ProtocolType ) );
			return new PooledRpcClientConnection( socket, this );
		}

		public void Return( Socket socket )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			Contract.EndContractBlock();

			this._eventTable[ socket.RemoteEndPoint ].Set();
		}
	}
}
