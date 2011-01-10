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
using MsgPack.Rpc.Protocols;
using System.Diagnostics.Contracts;
using System.Threading;

namespace MsgPack.Rpc
{
	/// <summary>
	///		<see cref="IAsyncResult"/> implementation for async RPC.
	/// </summary>
	internal sealed class RequestMessageAsyncResult : MessageAsyncResult
	{
		private ResponseMessage _response;

		public ResponseMessage Response
		{
			get
			{
				if ( !this.IsCompleted )
				{
					throw new InvalidOperationException( "Operation has not been completed yet." );
				}

				this.Finish();
				return this._response;
			}
		}

		internal void OnReceived( ResponseMessage responseMessage, bool completedSynchronously )
		{
			Contract.Assert( responseMessage.MessageId == this.MessageId );

			try { }
			finally
			{
				this._response = responseMessage;
				Thread.MemoryBarrier();
				base.Complete( completedSynchronously );
			}
		}

		public RequestMessageAsyncResult( Object owner, int messageId, AsyncCallback asyncCallback, object asyncState )
			: base( owner, messageId, asyncCallback, asyncState ) { }
	}
}
