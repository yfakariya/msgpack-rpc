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

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents MsgPack-RPC error instance.
	/// </summary>
	public struct RpcErrorMessage
	{
		public static readonly RpcErrorMessage Success = new RpcErrorMessage();

		public bool IsSuccess
		{
			get { return this._error == null; }
		}

		private readonly RpcError _error;

		public RpcError Error
		{
			get
			{
				if ( this._error == null )
				{
					throw new InvalidOperationException( "Operation success." );
				}

				return this._error;
			}
		}

		private readonly string _description;

		public string Description
		{
			get { return this._description ?? this.Error.DefaultMessage; }
		}

		private readonly string _debugInformation;

		public string DebugInformation
		{
			get { return this._debugInformation ?? String.Empty; }
		}

		public RpcErrorMessage( RpcError error, string description )
			: this( error, description, null ) { }

		public RpcErrorMessage( RpcError error, string description, string debugInformation )
		{
			if ( error == null )
			{
				throw new ArgumentNullException( "error" );
			}

			Contract.EndContractBlock();

			this._error = error;
			this._description = description;
			this._debugInformation = debugInformation;
		}

		public override string ToString()
		{
			if ( this.IsSuccess )
			{
				return String.Empty;
			}
			else if ( String.IsNullOrEmpty( this._debugInformation ) )
			{
				return String.Format( CultureInfo.CurrentCulture, "{0}({1}):{2}", this._error.Identifier, this._error.ErrorCode, this._description ?? this._error.DefaultMessage );
			}
			else
			{
				return String.Format( CultureInfo.CurrentCulture, "{1}({2}):{3}{0}{4}", Environment.NewLine, this._error.Identifier, this._error.ErrorCode, this._description ?? this._error.DefaultMessage, this._debugInformation );
			}
		}
	}
}
