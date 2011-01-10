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

namespace MsgPack.Rpc.Protocols.Connection
{
	/// <summary>
	///		Represents abstract network connection.
	/// </summary>
	public abstract class RpcClientConnection : IDisposable
	{
		/// <summary>
		///		Get underlying socket.
		/// </summary>
		/// <value>Underlying socket.</value>
		/// <exception cref="ObjectDisposedException">Already closed.</exception>
		public abstract Socket Socket
		{
			get;
		}

		protected RpcClientConnection()
		{
		}

		/// <summary>
		///		Cleanup underlying managed and unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		///		Cleanup unmanaged resources, and optionally release managed resources.
		/// </summary>
		/// <param name="disposing">
		///		If managed resources also are needed to release then true.
		/// </param>
		protected abstract void Dispose( bool disposing );
	}
}
