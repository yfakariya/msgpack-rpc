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
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Defines interfaces of post-deserialization filter for MessagePack-RPC request or notification message.
	/// </summary>
	public abstract class RequestMessageDeserializationFilter
	{
		/// <summary>
		///		Process request message before deserialization.
		/// </summary>
		/// <param name="context">Context information of deserializing message.</param>
		internal void Process( RequestMessageDeserializationContext context )
		{
			Contract.Assert( context != null );
			
			this.ProcessCore( context );
		}

		/// <summary>
		///		Process request message before deserialization.
		/// </summary>
		/// <param name="context">Context information of deserializing message.</param>
		protected abstract void ProcessCore( RequestMessageDeserializationContext context );
	}
}
