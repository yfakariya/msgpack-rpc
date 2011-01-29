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
using System.Net.Sockets;
using System.IO;
using MsgPack.Rpc.Serialization;
using MsgPack.Collections;
using MsgPack.Rpc.Protocols;
using System.Net;

namespace MsgPack.Rpc.TestDoubles
{
	public sealed class ServerMock : IDisposable
	{
		private readonly Socket _socket;
		private readonly int _bufferSize;
		private bool _isDisposed;
		public event EventHandler<ServerMockReceivedEventArgs> Received;

		private void OnReceived( ServerMockReceivedEventArgs e )
		{
			var handler = this.Received;
			if ( handler != null )
			{
				handler( this, e );
			}
		}

		private ServerMock( Socket socket, int bufferSize )
		{
			this._socket = socket;
			this._bufferSize = bufferSize;
			this.Accept();
		}

		private void Accept()
		{
			var context = new SocketAsyncEventArgs();
			context.Completed += ContextOperationCompleted;

			if ( this._socket.IsBound )
			{
				if ( !this._socket.AcceptAsync( context ) )
				{
					this.ContextOperationCompleted( context, context );
				}
			}
		}

		public void Dispose()
		{
			if ( this._isDisposed )
			{
				return;
			}

			this._isDisposed = true;
			if ( !this._socket.IsBound )
			{
				// maybe UDP
				this._socket.Shutdown( SocketShutdown.Both );
			}
			this._socket.Close();
		}

		private void ContextOperationCompleted( object sender, SocketAsyncEventArgs e )
		{
			switch ( e.LastOperation )
			{
				case SocketAsyncOperation.Accept:
				{
					if ( e.SocketError != SocketError.Success )
					{
						this.OnReceived( new ServerMockReceivedEventArgs( e ) );
						return;
					}
					else
					{
						e.SetBuffer( new byte[ this._bufferSize ], 0, this._bufferSize );
						if ( !e.AcceptSocket.ReceiveAsync( e ) )
						{
							goto case SocketAsyncOperation.Receive;
						}
						else
						{
							break;
						}
					}
				}
				case SocketAsyncOperation.Receive:
				{
					this.OnReceived( new ServerMockReceivedEventArgs( e ) );
					break;
				}
				case SocketAsyncOperation.ReceiveFrom:
				{
					throw new NotImplementedException();
				}
				default:
				{
					throw new InvalidOperationException( e.LastOperation.ToString() );
				}
			}

			this._socket.Listen( 16 );
			this.Accept();
		}

		public static ServerMock CreateTcp( int port, int bufferSize )
		{
			Socket socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			socket.Bind( new IPEndPoint( IPAddress.Loopback, port ) );
			socket.Listen( 16 );
			return new ServerMock( socket, bufferSize );
		}

		// TODO: UDP
	}
}
