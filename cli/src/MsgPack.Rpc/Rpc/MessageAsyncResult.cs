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
using System.Threading;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Common <see cref="IAsyncResult"/> implementation for MsgPack-RPC async invocation.
	/// </summary>
	internal class MessageAsyncResult : WrapperAsyncResult, IAsyncSessionErrorSink, IResponseHandler
	{
		private readonly int? _messageId;

		public int? MessageId
		{
			get { return this._messageId; }
		}

		private Exception _error;

		public Exception Error
		{
			get { return this._error; }
		}

		public void OnError( Exception error, bool completedSynchronously )
		{
			try { }
			finally
			{
				Interlocked.Exchange( ref this._error, error );
				base.Complete( completedSynchronously );
			}
		}

		private Protocols.ResponseMessage? _response;

		public void HandleResponse( Protocols.ResponseMessage response )
		{
			try { }
			finally
			{
				this._response = response;
				Thread.MemoryBarrier();
				base.Complete( false );
			}
		}

		public void HandleError( RpcErrorMessage error )
		{
			this.OnError( RpcException.FromRpcError( error ), false );
		}

		public MessageAsyncResult( Object owner, int? messageId, AsyncCallback asyncCallback, object asyncState )
			: base( owner, asyncCallback, asyncState )
		{
			this._messageId = messageId;
		}
	}
}
