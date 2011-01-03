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
	///		Represents MsgPack-RPC request message or notification message from client to server.
	/// </summary>
	public struct RequestMessage
	{
		public MessageType MessageType
		{
			get { return this._messageId == null ? Protocols.MessageType.Notification : Protocols.MessageType.Request; }
		}

		private readonly int? _messageId;

		public int MessageId
		{
			get { return this._messageId.Value; }
		}

		private readonly string _method;

		public string Method
		{
			get { return this._method; }
		}

		private readonly IList<MessagePackObject> _arguments;

		public IList<MessagePackObject> Arguments
		{
			get { return this._arguments; }
		}

		public RequestMessage( int? messageId, string methodName, IList<MessagePackObject> arguments )
		{
			this._messageId = messageId;
			this._method = methodName;
			this._arguments = arguments ?? Constants.EmptyArguments;
		}
	}
}
