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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MsgPack;
using MsgPack.Collections;
using MsgPack.Rpc;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Serialization;
using MsgPack.Rpc.TestDoubles;
using NUnit.Framework;
using System.Text;
using System.Diagnostics;

namespace MessagePack.Rpc.Protocols
{
	[TestFixture]
	public class ClientEventLoopTest
	{

		private static int MillisecondsTimeout
		{
			get
			{
				if ( Debugger.IsAttached )
				{
					return Timeout.Infinite;
				}
				else
				{
					return 3000;
				}
			}
		}

		private static IEnumerable<ClientEventLoop> CreateTestTargets(
			Func<RpcClientOptions> optionsFactory,
			Func<EventHandler<RpcTransportErrorEventArgs>> errorHandlerFactory, // FIXME: Should be Action<T>
			Func<CancellationTokenSource> cancellationTokenSourceFactory
		)
		{
			yield return new IOCompletionPortClientEventLoop( optionsFactory(), errorHandlerFactory(), cancellationTokenSourceFactory() );
		}

		private static byte[] CreateDefaultRequest()
		{
			using ( var buffer = new MemoryStream() )
			{
				using ( var packer = Packer.Create( buffer ) )
				{
					packer.Pack(
						new MessagePackObject[]
						{
							0,
							1,
							"Test",
							new MessagePackObject[ 0 ]
						}
					);
					buffer.Flush();
					return buffer.ToArray();
				}
			}
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void TestConstructor_IOCompletion_OptionIsNull()
		{
			new IOCompletionPortClientEventLoop( null, ( sender, e ) => { return; }, null ).Dispose();
		}

		[Test]
		public void TestConstructor_IOCompletion_ErrorHandlerIsNull()
		{
			new IOCompletionPortClientEventLoop( new RpcClientOptions(), null, null ).Dispose();
		}

		[Test]
		public void TestConstructor_IOCompletion_CancellationTokenIsNull()
		{
			using ( var target = new IOCompletionPortClientEventLoop( new RpcClientOptions(), ( sender, e ) => { return; }, null ) )
			{
				// Solely CancellationTokenSource will return valid token.
				Assert.IsTrue( target.CancellationToken.CanBeCanceled );
			}
		}

		[Test]
		public void TestConnect_Success()
		{
			Exception globalError = null;
			foreach ( var target in
				CreateTestTargets(
					() => new RpcClientOptions(),
					() => new EventHandler<RpcTransportErrorEventArgs>( ( sender, e ) =>
					{
						globalError = e.RpcError.Value.ToException();
						return;
					} ),
					() => null
				) )
			{
				using ( var socket
					= new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
				using ( var server = ServerMock.CreateTcp( 57129, 8192 ) )
				using ( var connectCompleted = new ManualResetEventSlim() )
				{
					var socketContext = target.CreateSocketContext( new IPEndPoint( IPAddress.Loopback, 57129 ) );
					var connectClientMock = new AsyncConnectClientMock( socket, socketContext );
					object expectedAsyncState = new object();
					var connectContext = new ConnectingContext( connectClientMock, expectedAsyncState );
					int connected = 0;
					connectClientMock.Connected +=
						( context, completedSynchronously, asyncState ) =>
						{
							Assert.IsFalse( completedSynchronously );
							Assert.AreSame( expectedAsyncState, asyncState );
							connected++;
							connectCompleted.Set();
						};
					Exception errorOnTest = null;
					connectClientMock.ConnectError +=
						( context, error, completedSynchronously, asyncState ) =>
						{
							Assert.AreSame( expectedAsyncState, asyncState );
							errorOnTest = error;
							Console.Error.WriteLine( "Error::CompletedSynchronously:{0}", completedSynchronously );
							connectCompleted.Set();
						};
					target.Connect( connectContext );
					Assert.IsTrue( connectCompleted.Wait( 500 ) );
					if ( errorOnTest != null )
					{
						Assert.Fail( errorOnTest.ToString() );
					}
					if ( globalError != null )
					{
						Assert.Fail( globalError.ToString() );
					}

					Assert.IsNotNull( socketContext.ConnectSocket );
					Assert.AreEqual( 1, connected );
					socket.Shutdown( SocketShutdown.Both );
				}
			}
		}

		[Test]
		public void TestConnect_FailToConnect()
		{
			Exception globalError = null;
			foreach ( var target in
				CreateTestTargets(
					() => new RpcClientOptions(),
					() => new EventHandler<RpcTransportErrorEventArgs>( ( sender, e ) =>
						{
							globalError = e.RpcError.Value.ToException();
							return;
						} ),
					() => null
				) )
			{
				using ( var socket
					= new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
				using ( var server = ServerMock.CreateTcp( 57129, 8192 ) )
				using ( var connectCompleted = new ManualResetEventSlim() )
				{
					var connectClientMock =
						new AsyncConnectClientMock( socket, target.CreateSocketContext( new IPEndPoint( IPAddress.Loopback, 57128 ) ) );
					object expectedAsyncState = new object();
					var connectContext = new ConnectingContext( connectClientMock, expectedAsyncState );
					int connected = 0;
					connectClientMock.Connected +=
						( context, completedSynchronously, asyncState ) =>
						{
							Assert.IsFalse( completedSynchronously );
							Assert.AreSame( expectedAsyncState, asyncState );
							connected++;
							connectCompleted.Set();
						};
					Exception errorOnTest = null;
					connectClientMock.ConnectError +=
						( context, error, completedSynchronously, asyncState ) =>
						{
							Assert.AreSame( expectedAsyncState, asyncState );
							errorOnTest = error;
							Console.Error.WriteLine( "Error::CompletedSynchronously:{0}", completedSynchronously );
							connectCompleted.Set();
						};
					target.Connect( connectContext );
					Assert.IsTrue( connectCompleted.Wait( 5000 ) );
					if ( globalError != null )
					{
						Assert.Fail( globalError.ToString() );
					}

					Assert.AreEqual( 0, connected );
					Assert.NotNull( errorOnTest );
					Assert.IsInstanceOf<SocketException>( errorOnTest, errorOnTest.ToString() );
				}
			}
		}

		[Test]
		public void TestSendAndReceive()
		{
			TestSendAndReceiveCore( 8, 1, "OK" );
		}

		[Test]
		public void TestSendAndReceive_NeedsFeedMore()
		{
			TestSendAndReceiveCore( 8, 1, "LongLongLongMessage" );
		}

		private static void TestSendAndReceiveCore( int bufferSegmentSize, int bufferSegmentCount, String messageToSend )
		{
			Exception globalError = null;
			foreach ( var target in
				CreateTestTargets(
					() => new RpcClientOptions() { BufferSegmentSize = bufferSegmentSize, BufferSegmentCount = bufferSegmentCount },
					() => new EventHandler<RpcTransportErrorEventArgs>( ( sender, e ) =>
					{
						globalError = e.RpcError.Value.ToException();
						return;
					} ),
					() => null
				) )
			{
				using ( var socket
					= new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
				using ( var server = ServerMock.CreateTcp( 57129, 8192 ) )
				using ( var connectCompleted = new ManualResetEventSlim() )
				using ( var receiveCompleted = new ManualResetEventSlim() )
				{
					server.Received +=
						( sender, e ) =>
						{
							Console.WriteLine(
								"Received. Type:{0}, ID:{1}, Method:'{2}', Arguments:'{3}'",
								e.GetRequest().MessageType,
								e.GetRequest().MessageId,
								e.GetRequest().Method,
								e.GetRequest().Arguments.Aggregate(
									new StringBuilder( "[" ),
									( buffer, item ) => ( buffer.Length == 1 ? buffer : buffer.Append( ", " ) ).Append( item )
								).Append( ']' )
							);
							Assert.AreEqual( "Test", e.GetRequest().Method );
							e.Reply( 1, messageToSend );
						};
					var socketContext = target.CreateSocketContext( new IPEndPoint( IPAddress.Loopback, 57129 ) );
					var connectClientMock = new AsyncConnectClientMock( socket, socketContext );
					connectClientMock.Connected +=
						( context, completedSynchronously, asyncState ) =>
						{
							connectCompleted.Set();
						};
					Exception errorOnTest = null;
					connectClientMock.ConnectError +=
						( context, error, completedSynchronously, asyncState ) =>
						{
							errorOnTest = error;
							connectCompleted.Set();
						};
					target.Connect( new ConnectingContext( connectClientMock, null ) );
					Assert.IsTrue( connectCompleted.Wait( 3000 ) );
					if ( errorOnTest != null )
					{
						Assert.Fail( errorOnTest.ToString() );
					}
					if ( globalError != null )
					{
						Assert.Fail( globalError.ToString() );
					}

					Assert.IsNotNull( socketContext.ConnectSocket );
					var receiverMock = new TransportReceiveHandlerMock();
					MessagePackObject? returned = null;
					receiverMock.Received +=
						( receiveContext ) =>
						{
							var message = SerializationUtility.DeserializeResponse( receiveContext.ReceivingBuffer );
							if ( message.Error != null )
							{
								errorOnTest = message.Error;
							}
							else
							{
								returned = message.ReturnValue;
							}

							receiveCompleted.Set();
						};
					var sendingContext =
						new SendingContext(
							new ClientSessionContext(
								receiverMock,
								new RpcClientOptions(),
								socketContext
							),
							new RpcOutputBuffer( ChunkBuffer.CreateDefault() ),
							1,
							( _sendingContext, error, completedSynchronously ) =>
							{
								errorOnTest = error;
							}
						);
					var requestBytes = CreateDefaultRequest();
					sendingContext.SendingBuffer.OpenWriteStream().Write( requestBytes, 0, requestBytes.Length );
					sendingContext.SocketContext.BufferList = sendingContext.SendingBuffer.DebugGetChunk();
					target.Send( sendingContext );
					Assert.IsTrue( receiveCompleted.Wait( MillisecondsTimeout ), "Timeout" );
					Assert.IsTrue( returned.HasValue );
					if ( errorOnTest != null )
					{
						Assert.Fail( errorOnTest.ToString() );
					}
					if ( globalError != null )
					{
						Assert.Fail( globalError.ToString() );
					}

					Assert.AreEqual( messageToSend, returned.Value.AsString() );
					socket.Shutdown( SocketShutdown.Both );
				}
			}
		}

		public void TestSendToAndReceiveFrom()
		{
			// FIXME: Impl
		}

		public void TestSendToAndReceiveFrom_Multiply()
		{
			// FIXME: Impl
		}

		private sealed class TransportReceiveHandlerMock : ITransportReceiveHandler
		{
			public event Action<ReceivingContext> Received;
			public TransportReceiveHandlerMock() { }

			public void OnReceive( ReceivingContext context )
			{
				context.SocketContext.UserToken = context;

				var handler = this.Received;
				if ( handler != null )
				{
					handler( context );
				}
			}

			public ChunkBuffer GetBufferForReceive( SendingContext context )
			{
				return context.SendingBuffer.DebugGetChunk();
			}

			public ChunkBuffer ReallocateReceivingBuffer( ChunkBuffer oldBuffer, long requestedLength, ReceivingContext context )
			{
				return ChunkBuffer.CreateDefault();
			}
		}

		private sealed class AsyncConnectClientMock : IAsyncConnectClient
		{
			public RpcSocket Socket
			{
				get;
				private set;
			}

			public RpcSocketAsyncEventArgs SocketContext
			{
				get;
				private set;
			}

			public event Action<ConnectingContext, bool, object> Connected;

			public event Action<RpcError, Exception, bool, object> ConnectError;

			public AsyncConnectClientMock( Socket socket, RpcSocketAsyncEventArgs context )
			{
				this.Socket = new SimpleRpcSocket( socket );
				this.SocketContext = context;
			}

			public void OnConnected( ConnectingContext context, bool completedSynchronously, object asyncState )
			{
				var handler = this.Connected;
				if ( handler != null )
				{
					handler( context, completedSynchronously, asyncState );
				}
			}

			public void OnConnectError( RpcError rpcError, Exception exception, bool completedSynchronously, object asyncState )
			{
				var handler = this.ConnectError;
				if ( handler != null )
				{
					handler( rpcError, exception, completedSynchronously, asyncState );
				}
			}
		}
	}
}
