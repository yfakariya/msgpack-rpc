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
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Exception thrown when some error ocurred above transport layer in remote server.
	/// </summary>
	/// <remarks>
	///		In MessagePack-RPC, it is possible that remote server is not even CLI environment.
	///		For example, it might be JVM environment, native C++, Ruby runtime, native D language, or so.
	///		Therefore, it is impossible to represent application-specific error as <see cref="Exception"/> since an exception is environment-specific representation of a error.
	///		The solution is to pack error information to Message-Pack map representation.
	///		So, this class wraps the map as CLI <see cref="Exception"/> to interoperate MessagePack-RPC and CLI environment.
	/// </remarks>
	public class RpcFaultException : RpcException
	{
		private readonly MessagePackObject? _remoteErrorInfo;

		/// <summary>
		///		Get MsgPack-RPC custom error information.
		/// </summary>
		/// <value>
		///		MsgPack-RPC custom error information. This value may be null.
		/// </value>
		/// <remarks>
		///		<para>
		///			When this exception was thrown by local process, this property will be null.
		///			You can examine standard exception properties (such as <see cref="InnerException"/>, <see cref="Source"/>, <see cref="TargetSite"/>, <see cref="StackTrace"/>)
		///			to retrieve detailed information.
		///			When this exception was throws by MessagePack-RPC client runtime to represent remote application error,
		///			this property will include remote error information as map.
		///		</para>
		///		<note>
		///			It is highly recommended that you do not include detailed information this property except testing environment.
		///		</note>
		///		<para>
		///			You should expose error map schema for client to allow handling application error gracefully.
		///		</para>
		///		<note>
		///			This concept is simlar to <see cref="System.ServiceModel.FaultException"/>.
		///		</note>
		/// </remarks>
		public MessagePackObject? RemoteErrorInfo
		{
			get { return this._remoteErrorInfo; }
		}

		public RpcFaultException( RpcError rpcError ) : base( rpcError, "MessagePack-RPC destination server thrown exception.", null ) { }
		public RpcFaultException( RpcError rpcError, string message, string debugInformation ) : base( rpcError, message, debugInformation ) { }
		public RpcFaultException( RpcError rpcError, string message, string debugInformation, Exception inner ) : base( rpcError, message, debugInformation, inner ) { }
		protected internal RpcFaultException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }
		protected internal RpcFaultException( MessagePackObject serializedException )
			:base(serializedException)
		{
			this._remoteErrorInfo = serializedException;
		}
	}
}
