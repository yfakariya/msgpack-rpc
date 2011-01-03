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
using System.Runtime.Serialization;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Thrown if incoming MsgPack-RPC message exceeds the quota.
	/// </summary>
	[Serializable]
	public sealed class RpcMessageTooLongException : RpcProtocolException
	{
		/// <summary>
		///		Initialize new instance with system default message.
		/// </summary>
		/// <param name="rpcError">Metadata of MsgPack-RPC error.</param>
		public RpcMessageTooLongException( RpcError rpcError ) : this( rpcError, "Message is too large.",null ) { }
		public RpcMessageTooLongException( RpcError rpcError, string message, string debugInformation ) : base( rpcError, message, debugInformation ) { }
		public RpcMessageTooLongException( RpcError rpcError, string message, string debugInformation, Exception inner ) : base( rpcError, message, debugInformation, inner ) { }
		internal RpcMessageTooLongException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
	}
}
