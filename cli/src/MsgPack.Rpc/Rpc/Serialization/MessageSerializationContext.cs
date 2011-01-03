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
using System.Linq;
using System.Text;
using MsgPack.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace MsgPack.Rpc.Serialization
{
	public abstract class MessageSerializationContext : SerializationErrorSink
	{
		private RpcOutputBuffer _buffer;
		internal RpcOutputBuffer Buffer
		{
			get { return this._buffer; }
		}

		protected MessageSerializationContext( RpcOutputBuffer buffer )
		{
			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}
			
			Contract.EndContractBlock();

			this._buffer = buffer;
		}
	}
}