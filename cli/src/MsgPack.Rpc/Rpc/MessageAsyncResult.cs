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
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Common <see cref="IAsyncResult"/> implementation for MsgPack-RPC async invocation.
	/// </summary>
	internal class MessageAsyncResult : AsyncResult, IAsyncSessionErrorSink, IResponseHandler
	{
		private readonly int? _messageId;

		/// <summary>
		///		Get ID of message.
		/// </summary>
		/// <value>ID of message.</value>
		public int? MessageId
		{
			get { return this._messageId; }
		}

		private ResponseMessage? _response;

		/// <summary>
		///		Complete this invocation as success.
		/// </summary>
		/// <param name="response">
		///		Replied response.
		///	</param>
		/// <param name="completedSynchronously">
		///		When operation is completed same thread as initiater then true.
		/// </param>
		public void HandleResponse( ResponseMessage response, bool completedSynchronously )
		{
			try { }
			finally
			{
				this._response = response;
				Thread.MemoryBarrier();
				base.Complete( completedSynchronously );
			}
		}

		/// <summary>
		///		Complete this invocation as error.
		/// </summary>
		/// <param name="error">
		///		Occurred RPC error.
		///	</param>
		/// <param name="completedSynchronously">
		///		When operation is completed same thread as initiater then true.
		/// </param>
		public void HandleError( RpcErrorMessage error, bool completedSynchronously )
		{
			this.OnError( RpcException.FromRpcError( error ), completedSynchronously );
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="owner">
		///		Owner of asynchrnous invocation. This value will not be null.
		/// </param>
		/// <param name="messageId">ID of message.</param>
		/// <param name="asyncCallback">
		///		Callback of asynchrnous invocation which should be called in completion.
		///		This value can be null.
		/// </param>
		/// <param name="asyncState">
		///		State object of asynchrnous invocation which will be passed to <see cref="AsyncCallback"/>.
		///		This value can be null.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="owner"/> is null.
		/// </exception>
		public MessageAsyncResult( Object owner, int? messageId, AsyncCallback asyncCallback, object asyncState )
			: base( owner, asyncCallback, asyncState )
		{
			this._messageId = messageId;
		}
	}
}
