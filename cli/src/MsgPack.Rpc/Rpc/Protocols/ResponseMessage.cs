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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Represents MsgPack-RPC response message from server to client.
	/// </summary>
	public struct ResponseMessage
	{
		public MessagePackObject MessageType
		{
			get { return ( int )Protocols.MessageType.Response; }
		}

		private readonly int _messageId;

		public int MessageId
		{
			get { return this._messageId; }
		}

		private readonly RpcException _error;

		public RpcException Error
		{
			get { return this._error; }
		}

		private readonly MessagePackObject _returnValue;

		public MessagePackObject ReturnValue
		{
			get { return this._returnValue; }
		}

		public ResponseMessage( int messageId )
			: this( messageId, MessagePackObject.Nil ) { }

		public ResponseMessage( int messageId, MessagePackObject returnValue )
			: this( messageId, returnValue, null ) { }

		public ResponseMessage( int messageId, RpcException error )
			: this( messageId, MessagePackObject.Nil, error ) { }

		private ResponseMessage( int messageId, MessagePackObject returnValue, RpcException error )
		{
			this._messageId = messageId;
			this._returnValue = returnValue;
			this._error = error;
		}
	}
}
