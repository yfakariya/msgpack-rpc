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
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;

namespace MsgPack.Rpc.Protocols.Connection
{
	/// <summary>
	///		Connection implementation for pooled ones.
	/// </summary>
	internal sealed class PooledRpcClientConnection : RpcClientConnection
	{
		private readonly ConnectionPool _pool;
		private Socket _socket;
		private readonly String _stringIdentifier;

		// TODO: lifecycle management
		public sealed override Socket Socket
		{
			get
			{
				var socket = this._socket;
				if ( socket == null )
				{
					throw new ObjectDisposedException( this._stringIdentifier );
				}

				return socket;
			}
		}

		public PooledRpcClientConnection( Socket socket, ConnectionPool pool )
		{
			Contract.Assert( socket != null );
			Contract.Assert( pool != null );

			this._pool = pool;
			Interlocked.Exchange( ref this._socket, socket );
			this._stringIdentifier = String.Format( CultureInfo.InvariantCulture, "{0}(pooled)@{1}", socket.RemoteEndPoint, pool );
		}

		protected sealed override void Dispose( bool disposing )
		{
			var socket = this._socket;
			if ( socket != null )
			{
				try { }
				finally
				{
					socket = Interlocked.CompareExchange( ref this._socket, null, socket );
					if ( socket != null )
					{
						this._pool.Return( socket );
					}
				}
			}
		}
	}
}
