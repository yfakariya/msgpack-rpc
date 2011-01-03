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
using System.Diagnostics;
using System.Net.Sockets;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Reprsents event data of <see cref="EventLoop.OnError"/> event.
	/// </summary>
	public sealed class RpcTransportErrorEventArgs : EventArgs
	{
		private readonly int? _messageId;

		public int? MessageId
		{
			get { return this._messageId; }
		}

		private readonly RpcTransportOperation _operation;

		public RpcTransportOperation Operation
		{
			get { return this._operation; }
		}

		private readonly SocketError? _socketErrorCode;

		public SocketError? SocketErrorCode
		{
			get { return _socketErrorCode; }
		}

		private readonly RpcErrorMessage? _rpcError;

		public RpcErrorMessage? RpcError
		{
			get { return this._rpcError; }
		}

		public RpcTransportErrorEventArgs( SocketAsyncOperation operation, SocketError socketErrorCode )
		{
			this._operation = ToRpcTransportOperation( operation );
			this._socketErrorCode = socketErrorCode;
		}

		private static RpcTransportOperation ToRpcTransportOperation( SocketAsyncOperation operation )
		{
			switch ( operation )
			{
				case SocketAsyncOperation.Accept:
				{
					return RpcTransportOperation.Accept;
				}
				case SocketAsyncOperation.Connect:
				{
					return RpcTransportOperation.Bind;
				}
				case SocketAsyncOperation.Disconnect:
				{
					return RpcTransportOperation.Disconnect;
				}
				case SocketAsyncOperation.Receive:
				case SocketAsyncOperation.ReceiveFrom:
				case SocketAsyncOperation.ReceiveMessageFrom:
				{
					return RpcTransportOperation.Receive;
				}
				case SocketAsyncOperation.Send:
				case SocketAsyncOperation.SendPackets:
				case SocketAsyncOperation.SendTo:
				{
					return RpcTransportOperation.Send;
				}
				case SocketAsyncOperation.None:
				{
					// Should not pass None.
					Debug.Fail( "SocketAsyncOperation.None" );
					return RpcTransportOperation.None;
				}
				default:
				{
					Debug.WriteLine( "Unepcted SocketAsyncOperation:" + operation );
					return RpcTransportOperation.Unknown;
				}
			}
		}

		public RpcTransportErrorEventArgs( RpcTransportOperation operation, RpcErrorMessage rpcError )
		{
			this._operation = operation;
			this._rpcError = rpcError;
		}

		public RpcTransportErrorEventArgs( RpcTransportOperation operation, int messageId, RpcErrorMessage rpcError )
		{
			this._operation = operation;
			this._messageId = messageId;
			this._rpcError = rpcError;
		}
	}
}
