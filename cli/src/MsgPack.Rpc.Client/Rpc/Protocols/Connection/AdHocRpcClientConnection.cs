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
using System.Globalization;
using System.Net.Sockets;

namespace MsgPack.Rpc.Protocols.Connection
{
	/// <summary>
	///		Represents ad-hoc (not pooled) connection.
	/// </summary>
	internal sealed class AdHocRpcClientConnection : RpcClientConnection
	{
		private Socket _socket;
		private readonly String _stringIdentifier;

		public sealed override Socket Socket
		{
			get
			{
				if ( this._socket == null )
				{
					throw new ObjectDisposedException( this._stringIdentifier );
				}

				return this._socket;
			}
		}

		public AdHocRpcClientConnection( Socket socket )
		{
			Contract.Assert( socket != null );

			this._socket = socket;
			this._stringIdentifier = String.Format( CultureInfo.InvariantCulture, "{0}(ad-hoc)", socket.RemoteEndPoint );
		}

		protected sealed override void Dispose( bool disposing )
		{
			var socket = this._socket;

			if ( socket != null )
			{
				socket.Shutdown( SocketShutdown.Both );
				socket.Close();
				this._socket = null;
			}
		}
	}
}
