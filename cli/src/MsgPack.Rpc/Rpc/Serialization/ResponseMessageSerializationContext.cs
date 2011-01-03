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
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of response message serialization.
	/// </summary>
	public sealed class ResponseMessageSerializationContext : MessageSerializationContext
	{
		private readonly bool _isVoid;

		public bool IsVoid
		{
			get { return this._isVoid; }
		}

		private RpcException _exception;

		public RpcException Exception
		{
			get { return _exception; }
		}

		private object _returnValue;

		public object ReturnValue
		{
			get { return this._returnValue; }
		}

		// for void.
		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer )
			: this( buffer, null, null, true ) { }

		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer, object returnValue )
			: this( buffer, returnValue, null, false ) { }

		internal ResponseMessageSerializationContext( RpcOutputBuffer buffer, RpcException exception, bool isVoid )
			: this( buffer, null, exception, isVoid )
		{
			if ( exception == null )
			{
				throw new ArgumentNullException( "exception" );
			}

			Contract.EndContractBlock();
		}

		private ResponseMessageSerializationContext( RpcOutputBuffer buffer, object returnValue, RpcException exception, bool isVoid )
			: base( buffer )
		{
			this._returnValue = returnValue;
			this._isVoid = isVoid;
			this._exception = exception;
		}


		public void SwallowException()
		{
			if ( !this._isVoid )
			{
				throw new InvalidOperationException( "Must specify alternate return value." );
			}

			Contract.EndContractBlock();

			this._exception = null;
		}

		public void SwallowException( object alternateReturnValue )
		{
			if ( this._isVoid )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to this method return value is void." );
			}

			Contract.EndContractBlock();

			this._exception = null;
			this._returnValue = alternateReturnValue;
		}

		public void ThrowException( RpcException alternateException )
		{
			if ( alternateException == null )
			{
				throw new ArgumentNullException( "alternateException" );
			}

			Contract.EndContractBlock();

			this._returnValue = null;
			this._exception = alternateException;
		}

		public void ChangeReturnValue( object alternateReturnValue )
		{
			if ( this._isVoid )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to this method return value is void." );
			}

			if ( this._exception != null )
			{
				throw new InvalidOperationException( "Cannot specify alternate return value due to an exception was occurred." );
			}

			Contract.EndContractBlock();

			this._returnValue = alternateReturnValue;
		}
	}
}
