﻿#region -- License Terms --
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
using System.Diagnostics.Contracts;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of request or notification message serialization.
	/// </summary>
	public sealed class RequestMessageSerializationContext : MessageSerializationContext
	{
		private string _methodName;

		public string MethodName
		{
			get { return this._methodName; }
			set
			{
				if ( value == null )
				{
					throw new ArgumentNullException( "value" );
				}

				if ( String.IsNullOrWhiteSpace( value ) )
				{
					throw new ArgumentException( "'value' cannot be empty.", "value" );
				}

				this._methodName = value;
			}
		}

		private readonly IList<object> _arguments;

		public IList<object> Arguments
		{
			get { return this._arguments; }
		}

		internal RequestMessageSerializationContext( RpcOutputBuffer buffer, string methodName, IList<object> arguments )
			: base( buffer )
		{
			if ( methodName == null )
			{
				throw new ArgumentNullException( "methodName" );
			}

			if ( String.IsNullOrWhiteSpace( methodName ) )
			{
				throw new ArgumentException( "'methodName' cannot be empty.", "methodName" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			Contract.EndContractBlock();

			this._methodName = methodName;
			this._arguments = arguments;
		}
	}

}