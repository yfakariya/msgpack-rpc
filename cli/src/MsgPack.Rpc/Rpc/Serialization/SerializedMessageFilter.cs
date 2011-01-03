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
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Defines interfaces of pre-deserialization or post-serialization filter for MessagePack-RPC.
	/// </summary>
	public abstract class SerializedMessageFilter<TContext>
		where TContext : SerializationErrorSink
	{
		public IEnumerable<byte> Process( IEnumerable<byte> serializedMessage, TContext context )
		{
			if ( serializedMessage == null )
			{
				throw new ArgumentNullException( "serializedMessage" );
			}

			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			return this.ProcessCore( serializedMessage, context );
		}

		protected abstract IEnumerable<byte> ProcessCore( IEnumerable<byte> serializedMessage, TContext context );
	}
}
