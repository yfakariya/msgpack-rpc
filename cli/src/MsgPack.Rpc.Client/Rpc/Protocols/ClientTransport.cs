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
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Threading;
using System.Globalization;
using MsgPack.Rpc.Serialization;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Define interface of client protocol binding.
	/// </summary>
	public abstract class ClientTransport : IDisposable
	{
		private readonly RequestMessageSerializer _requestSerializer;
		private readonly ResponseMessageSerializer _responseSerializer;

		private readonly ClientEventLoop _eventLoop;

		protected ClientEventLoop EventLoop
		{
			get { return this._eventLoop; }
		}

		private readonly ReceivingClientSocketAsyncEventArgs _receivingContext;

		public ReceivingClientSocketAsyncEventArgs ReceivingContext
		{
			get { return this._receivingContext; }
		}

		private readonly RpcSocket _socket;

		public RpcSocket Socket
		{
			get { return this._socket; }
		}

		private readonly CountdownEvent _sessionTableLatch = new CountdownEvent( 0 );
		private readonly TimeSpan _drainTimeout;

		private readonly ConcurrentDictionary<int, IResponseHandler> _sessionTable = new ConcurrentDictionary<int, IResponseHandler>();

		private bool _disposed;

		protected ClientTransport( RpcSocket socket, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			if ( eventLoop == null )
			{
				throw new ArgumentNullException( "eventLoop" );
			}

			Contract.EndContractBlock();

			this._socket = socket;
			this._eventLoop = eventLoop;
			this._requestSerializer = ClientServices.RequestSerializerFactory.Create( socket.Protocol, options );
			this._responseSerializer = ClientServices.ResponseDeserializerFactory.Create( socket.Protocol, options );
			this._receivingContext = eventLoop.CreateReceivingContext( this );
			this._drainTimeout = options == null ? TimeSpan.FromSeconds( 3 ) : options.DrainTimeout ?? TimeSpan.FromSeconds( 3 );
		}

		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			this._disposed = true;
		}

		protected void Drain()
		{
			this._sessionTableLatch.Wait( this._drainTimeout, this.EventLoop.CancellationToken );
		}

		public virtual SendingClientSocketAsyncEventArgs CreateNewSendingContext( int? messageId, Action<SendingClientSocketAsyncEventArgs, Exception, bool> onMessageSent )
		{
			return this._eventLoop.CreateSendingContext( this, messageId, onMessageSent );
		}

		public void Send( MessageType type, int? messageId, String method, IList<object> arguments, SendingClientSocketAsyncEventArgs sendingContext, IResponseHandler responseHandler )
		{
			switch ( type )
			{
				case MessageType.Request:
				case MessageType.Notification:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException( "type", type, "'type' must be 'Request' or 'Notificatiion'." );
				}
			}

			if ( method == null )
			{
				throw new ArgumentNullException( "method" );
			}

			if ( String.IsNullOrWhiteSpace( method ) )
			{
				throw new ArgumentException( "'method' cannot be empty.", "method" );
			}

			if ( arguments == null )
			{
				throw new ArgumentNullException( "arguments" );
			}

			if ( sendingContext == null )
			{
				throw new ArgumentNullException( "sendingContext" );
			}

			if ( this._disposed )
			{
				throw new ObjectDisposedException( this.GetType().Name + ":" + this._socket.RemoteEndPoint + "(" + this._socket.Protocol + ")" );
			}

			Contract.EndContractBlock();

			RpcErrorMessage serializationError = this._requestSerializer.Serialize( messageId, method, arguments, sendingContext.SendingBuffer );
			if ( !serializationError.IsSuccess )
			{
				throw new RpcTransportException( serializationError.Error, serializationError.Detail );
			}

			if ( messageId.HasValue )
			{
				try { }
				finally
				{
					this._sessionTableLatch.AddCount();
					if ( !this._sessionTable.TryAdd( messageId.Value, responseHandler ) )
					{
						throw new InvalidOperationException(
							String.Format( CultureInfo.CurrentCulture, "Message ID:{0} is already used.", messageId.Value )
						);
					}
				}
			}

			this.SendCore( sendingContext );
		}

		protected abstract void SendCore( SendingClientSocketAsyncEventArgs context );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns>If success to derialize from buffer then true. If buffer does not have enough data to deserialize then false.</returns>
		public void Receive( ReceivingClientSocketAsyncEventArgs context )
		{
			if ( context == null )
			{
				throw new ArgumentNullException( "context" );
			}

			Contract.EndContractBlock();

			ResponseMessage result;
			var error = this._responseSerializer.Deserialize( context.ReceivingBuffer, out result );
			this.OnResponse( result, error );
		}

		private void OnResponse( ResponseMessage response, RpcErrorMessage error )
		{
			IResponseHandler handler;
			bool removed;
			try { }
			finally
			{
				removed = this._sessionTable.TryRemove( response.MessageId, out handler );
				this._sessionTableLatch.Signal();
			}

			if ( removed )
			{
				if ( error.IsSuccess )
				{
					handler.HandleResponse( response, false );
				}
				else
				{
					handler.HandleError( error, false );
				}
			}
		}
	}
}
