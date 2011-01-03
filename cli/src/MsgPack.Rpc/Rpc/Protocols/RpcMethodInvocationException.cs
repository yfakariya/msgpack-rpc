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
	///		Thrown if something wrong during remote method invocation.
	/// </summary>
	[Serializable]
	public class RpcMethodInvocationException : RpcException
	{
		private readonly string _methodName;

		/// <summary>
		///		Get a name of invoking method.
		/// </summary>
		/// <value>
		///		Name of invoking method. This value will not be null nor empty.
		/// </value>
		public string MethodName
		{
			get { return this._methodName; }
		}

		public RpcMethodInvocationException( RpcError rpcError, string methodName ) : this( rpcError, methodName, "Failed to call specified method.", null ) { }
		public RpcMethodInvocationException( RpcError rpcError, string methodName, string message, string debugInformation ) : this( rpcError, methodName, message, debugInformation, null ) { }

		public RpcMethodInvocationException( RpcError rpcError, string methodName, string message, string debugInformation, Exception inner )
			: base( rpcError, message, debugInformation, inner )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty.", "methodName" );
			}

			this._methodName = methodName;
		}

		protected internal RpcMethodInvocationException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._methodName = info.GetString( "MethodName" );
		}

		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			info.AddValue( "MethodName", this._methodName );
		}
	}
}
