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
	///		Thrown if some arguments are wrong like its type was not match, its value was out of range, its value was null but it is not illegal, so on.
	/// </summary>
	[Serializable]
	public sealed class RpcArgumentException : RpcMethodInvocationException
	{
		private readonly string _parameterName;

		/// <summary>
		///		Get name of parameter causing this exception.
		/// </summary>
		/// <value>
		///		Name of parameter causing this exception. This value will not be null nor empty.
		/// </value>
		public string ParameterName
		{
			get { return this._parameterName; }
		}

		public RpcArgumentException( RpcError rpcError, string methodName, string parameterName ) : this( rpcError, methodName, parameterName, "The value of argument is invalid.", null ) { }
		public RpcArgumentException( RpcError rpcError, string methodName, string parameterName, string message, string debugInformation ) : this( rpcError, methodName, parameterName, message, debugInformation, null ) { }
		public RpcArgumentException( RpcError rpcError, string methodName, string parameterName, string message, string debugInformation, Exception inner )
			: base( rpcError, methodName, message, debugInformation, inner )
		{
			if ( parameterName == null )
			{
				throw new ArgumentNullException( "parameterName" );
			}

			if ( String.IsNullOrWhiteSpace( parameterName ) )
			{
				throw new ArgumentException( "'parameterName' cannot be empty.", "parameterName" );
			}

			this._parameterName = parameterName;
		}

		internal RpcArgumentException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._parameterName = info.GetString( "ParameterName" );
		}

		public sealed override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );
			info.AddValue( "ParameterName", this._parameterName );
		}
	}
}
