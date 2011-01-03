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
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Stores context information of response message deserialization.
	/// </summary>
	public sealed class ResponseMessageDeserializationContext : MessageDeserializationContext
	{
		private int _messageId;

		public int MessageId
		{
			get { return this._messageId; }
			internal set { this._messageId = value; }
		}

		private MessagePackObject _error;

		public MessagePackObject Error
		{
			get { return this._error; }
			set { this._error = value; }
		}

		private MessagePackObject? _deserializedResult;

		public MessagePackObject? DeserializedResult
		{
			get { return this._deserializedResult; }
		}

		public void SetDeserializedResult( MessagePackObject returnValueOrError )
		{
			this._deserializedResult = returnValueOrError;
		}

		internal ResponseMessageDeserializationContext( RpcInputBuffer buffer, int? maxLength )
			: base( buffer, maxLength ) { }
	}
}
