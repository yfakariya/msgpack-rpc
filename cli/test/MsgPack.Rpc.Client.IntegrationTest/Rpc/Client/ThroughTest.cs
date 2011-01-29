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
using System.Net;
using MsgPack.Rpc.Protocols;
using NUnit.Framework;
using MsgPack.Rpc.TestDoubles;
using System.Threading;
using System.Net.Sockets;

namespace MsgPack.Rpc.Client
{
	/// <summary>
	///		Simple through test.
	/// </summary>
	[TestFixture]
	public sealed class ThroughTest
	{
		[Test]
		public void TestTcpSend()
		{
			using ( var server = ServerMock.CreateTcp( 57129, 8192 ) )
			using( var serverDone = new ManualResetEventSlim())
			{
				Exception exceptionOnServer = null;
				int? requestId = null;
				server.Received +=
					( sender, e ) =>
					{
						try
						{
							var request = e.GetRequest();
							requestId = request.MessageId;
							Assert.AreEqual( MessageType.Request, request.MessageType );
							Assert.AreEqual( "Test", request.Method );
							Assert.AreEqual( 3, request.Arguments.Count, "Invalid argument count." );
							Assert.AreEqual( true, request.Arguments[ 0 ].AsBoolean() );
							Assert.AreEqual( "Test", request.Arguments[ 1 ].AsString() );
							Assert.IsTrue( request.Arguments[ 2 ].IsArray );
							var argments = request.Arguments[ 2 ].AsList();
							Assert.AreEqual( 1, argments[ 0 ].AsInt32() );
							Assert.AreEqual( 2, argments[ 1 ].AsInt32() );
							Assert.AreEqual( 3, argments[ 2 ].AsInt32() );
							e.Reply( requestId, new MessagePackObject( "Hello, world!" ) );
						}
						catch ( SocketException ex )
						{
							if ( ex.SocketErrorCode != SocketError.OperationAborted )
							{
								Console.Error.WriteLine( "{0}:{1}", ex.SocketErrorCode, ex );
								exceptionOnServer = ex;
							}
						}
						catch ( Exception ex )
						{
							Console.Error.WriteLine( ex );
							exceptionOnServer = ex;
						}
						finally
						{
							serverDone.Set();
						}
					};
				var options = new RpcClientOptions();
				options.SetForceIPv4( true );
				RpcErrorMessage? error = null;
				using ( var eventLoop = new IOCompletionPortClientEventLoop( options, ( sender, e ) => error = e.RpcError, null ) )
				{
					RpcClient client = RpcClient.CreateTcp( new IPEndPoint( IPAddress.Loopback, 57129 ), eventLoop, options );
					var ar = client.BeginCall( "Test", new object[] { true, "Test", new int[] { 1, 2, 3 } }, null, null );

					serverDone.Wait(); 
					
					if ( exceptionOnServer != null )
					{
						throw new AssertionException( "Server error.", exceptionOnServer );
					}

					var result = client.EndCall( ar );

					if ( error != null )
					{
						Assert.Fail( error.Value.ToString() );
					}

					Assert.AreEqual( "Hello, world!", result.AsString() );
				}
			}
		}
	}
}