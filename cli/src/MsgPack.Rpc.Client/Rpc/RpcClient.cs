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
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading.Tasks;
using MsgPack.Rpc.Protocols;
using System.Net;
using System.Threading;
using MsgPack.Collections;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Entry point of MessagePack-RPC client.
	/// </summary>
	/// <remarks>
	///		If you favor implicit (transparent) invocation model, you can use <see cref="PrcProxy"/> instead.
	/// </remarks>
	public sealed class RpcClient : IDisposable
	{
		private readonly ClientTransport _transport;

		public ClientTransport Transport
		{
			get { return this._transport; }
		}

		// TODO: It is too big overhead of ConcurrentDictionary since concurrency not very high.
		//		 If so, using Dictionary and Monitor might improve performance.
		private readonly ConcurrentDictionary<int, RequestMessageAsyncResult> _responseAsyncResults;

		internal RpcClient( ClientTransport transport )
		{
			if ( transport == null )
			{
				throw new ArgumentNullException( "transport" );
			}

			Contract.EndContractBlock();

			this._transport = transport;
			this._responseAsyncResults = new ConcurrentDictionary<int, RequestMessageAsyncResult>();
		}

		public static RpcClient CreateTcp( EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			using ( var waitHandle = new ManualResetEventSlim() )
			{
				RpcTransportException failure = null;
				var transport =
					new TcpClientTransport(
							ClientServices.SocketFactory(
								( options.ForceIPv4.GetValueOrDefault() ? RpcTransportProtocol.TcpIpV4 : RpcTransportProtocol.TcpIp ).CreateSocket()
							),
							eventLoop,
							options,
							( _0, _1, _2 ) => waitHandle.Set(),
							( error, exception, _0, _1 ) =>
							{
								failure = new RpcTransportException( error, "Failed to connect.", null, exception );
								waitHandle.Set();
							}
					);
				transport.Connect( remoteEndPoint, eventLoop.CreateConnectingContext( transport, remoteEndPoint ), null );
				waitHandle.Wait();
				if ( failure != null )
				{
					throw failure;
				}

				return new RpcClient( transport );
			}
		}

		public static IAsyncResult BeginCreateTcp( EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options, AsyncCallback asyncCallback, object asyncState )
		{
			var asyncResult = new CreateTcpAsyncResult( null, asyncCallback, asyncState );
			asyncResult.Transport =
					new TcpClientTransport(
							ClientServices.SocketFactory(
								( options.ForceIPv4.GetValueOrDefault() ? RpcTransportProtocol.TcpIpV4 : RpcTransportProtocol.TcpIp ).CreateSocket()
							),
							eventLoop,
							options,
							( e, completedSynchronously, state ) => ( state as CreateTcpAsyncResult ).OnConnected( e, completedSynchronously ),
							( error, exception, completedSynchronously, state ) => ( state as CreateTcpAsyncResult ).OnError( new RpcTransportException( error, "Failed to connect.", null, exception ), completedSynchronously )
					);
			asyncResult.Transport.Connect(
				remoteEndPoint,
				eventLoop.CreateConnectingContext( asyncResult.Transport, remoteEndPoint ),
				asyncResult
			);
			return asyncResult;
		}

		public static RpcClient EndCreateTcp( IAsyncResult ar )
		{
			var asyncResult = AsyncResult.Verify<CreateTcpAsyncResult>( ar, null );
			asyncResult.Finish();
			return new RpcClient( asyncResult.Transport );
		}

		public static RpcClient CreateUdp( EndPoint remoteEndPoint, ClientEventLoop eventLoop, RpcClientOptions options )
		{
			var transport =
				new UdpClientTransport(
					ClientServices.SocketFactory(
						( options.ForceIPv4.GetValueOrDefault() ? RpcTransportProtocol.UdpIpV4 : RpcTransportProtocol.UdpIp ).CreateSocket()
					),
					remoteEndPoint,
					eventLoop,
					options
				);
			return new RpcClient( transport );
		}

		public void Dispose()
		{
			this._transport.Dispose();
		}

		public MessagePackObject? Call( string methodName, params object[] arguments )
		{
			return this.EndCall( this.BeginCall( methodName, arguments, null, null ) );
		}

		public Task<MessagePackObject> CallAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[], MessagePackObject>( this.BeginCall, this.EndCall, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		public IAsyncResult BeginCall( string methodName, object[] arguments, AsyncCallback asyncCallback, object asyncState )
		{
			var messageId = MessageIdGenerator.Currrent.NextId();
			var asyncResult = new RequestMessageAsyncResult( this, messageId, asyncCallback, asyncState );

			bool isSent = false;
			try
			{
				try { }
				finally
				{
					if ( !this._responseAsyncResults.TryAdd( messageId, asyncResult ) )
					{
						throw new InvalidOperationException( String.Format( CultureInfo.CurrentCulture, "Message ID '{0}' is used.", messageId ) );
					}

					this.Transport.Send(
						MessageType.Request,
						messageId,
						methodName,
						arguments ?? Arrays<object>.Empty,
						this._transport.CreateNewSendingContext(
							messageId,
							( e, error, completedSynchronously ) =>
							{
								if ( error != null )
								{
									RequestMessageAsyncResult ar;
									if ( this._responseAsyncResults.TryRemove( messageId, out ar ) )
									{
										ar.OnError( error, completedSynchronously );
									}
								}
							}
						),
						asyncResult
					);
					isSent = true;
				}
			}
			finally
			{
				if ( !isSent )
				{
					// Remove response handler since sending is failed.
					RequestMessageAsyncResult disposal;
					this._responseAsyncResults.TryRemove( messageId, out disposal );
				}
			}

			return asyncResult;
		}

		private void OnReceived( ResponseMessage responseMessage, bool completedSynchronously, RequestMessageAsyncResult asyncResult )
		{
			asyncResult.OnReceived( responseMessage, completedSynchronously );

			if ( asyncResult.AsyncCallback != null )
			{
				asyncResult.AsyncCallback( asyncResult );
			}
		}

		public MessagePackObject EndCall( IAsyncResult asyncResult )
		{
			var requestAsyncResult = AsyncResult.Verify<RequestMessageAsyncResult>( asyncResult, this );

			// Wait for completion
			if ( !requestAsyncResult.IsCompleted )
			{
				asyncResult.AsyncWaitHandle.WaitOne();
			}

			var response = requestAsyncResult.Response;
			requestAsyncResult.Finish();

			// Fetch message
			if ( response.Error != null )
			{
				throw response.Error;
			}

			// Return it.
			return response.ReturnValue;
		}

		public void Notify( string methodName, params object[] arguments )
		{
			this.EndNotify( this.BeginNotify( methodName, arguments, null, null ) );
		}

		public Task NotifyAsync( string methodName, object[] arguments, object asyncState )
		{
			return Task.Factory.FromAsync<string, object[]>( this.BeginNotify, this.EndNotify, methodName, arguments, asyncState, TaskCreationOptions.None );
		}

		public IAsyncResult BeginNotify( string methodName, object[] arguments, AsyncCallback asyncCallback, object asyncState )
		{
			var asyncResult = new NotificationMessageAsyncResult( this, null, asyncCallback, asyncState );
			this.Transport.Send( MessageType.Notification, null, methodName, arguments ?? Arrays<object>.Empty, this._transport.CreateNewSendingContext( null, asyncResult.OnMessageSent ), asyncResult );
			return asyncResult;
		}

		internal void OnFailedNotification( Exception exception, bool completedSynchronously, IAsyncSessionErrorSink errorSink )
		{
			var asyncResult = errorSink as MessageAsyncResult;
			Contract.Assert( asyncResult != null );
			this.OnSentNotify( exception, completedSynchronously, asyncResult );
		}

		private void OnSentNotify( Exception exception, bool completedSynchronously, MessageAsyncResult asyncResult )
		{
			if ( exception == null )
			{
				asyncResult.Complete( completedSynchronously );
			}
			else
			{
				asyncResult.OnError( exception, completedSynchronously );
			}

			if ( asyncResult.AsyncCallback != null )
			{
				asyncResult.AsyncCallback( asyncResult );
			}
		}

		public void EndNotify( IAsyncResult asyncResult )
		{
			var notificationAsyncResult = AsyncResult.Verify<MessageAsyncResult>( asyncResult, this );
			notificationAsyncResult.Finish();
		}
		
		private sealed class CreateTcpAsyncResult : AsyncResult
		{
			public void OnConnected( ConnectingClientSocketAsyncEventArgs e, bool completedSynchronously )
			{
				Contract.Assume( e != null );
				Contract.Assume( e.ConnectSocket != null );
				base.Complete( completedSynchronously );
			}

			public TcpClientTransport Transport
			{
				get;
				set;
			}

			public CreateTcpAsyncResult( object owner, AsyncCallback asyncCallback, object asyncState )
				: base( owner, asyncCallback, asyncState ) { }
		}
	}


	// TODO via DynamicObject


}
