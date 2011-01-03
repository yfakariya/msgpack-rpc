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
using System.Net.Sockets;
using System.Threading;

namespace MsgPack.Rpc.Protocols
{
	/// <summary>
	///		Provides basic feature of all event loop implementations.
	/// </summary>
	/// <remarks>
	///		This class is thread safe, and derived classes should be thread safe.
	/// </remarks>
	public abstract class EventLoop
	{
		private EventHandler<RpcTransportErrorEventArgs> _transportError;

		public event EventHandler<RpcTransportErrorEventArgs> TransportError
		{
			add
			{
				for (
					EventHandler<RpcTransportErrorEventArgs> oldValue = this._transportError, newValue = null, currentValue = null;
					oldValue != currentValue;
					oldValue = currentValue
				)
				{
					newValue = Delegate.Combine( oldValue, value ) as EventHandler<RpcTransportErrorEventArgs>;
					currentValue = Interlocked.CompareExchange( ref this._transportError, newValue, oldValue );
				}
			}
			remove
			{
				for (
					EventHandler<RpcTransportErrorEventArgs> oldValue = this._transportError, newValue = null, currentValue = null;
					oldValue != currentValue;
					oldValue = currentValue
				)
				{
					newValue = Delegate.Remove( oldValue, value ) as EventHandler<RpcTransportErrorEventArgs>;
					currentValue = Interlocked.CompareExchange( ref this._transportError, newValue, oldValue );
				}
			}
		}

		protected virtual bool OnTransportError( RpcTransportErrorEventArgs e )
		{
			if ( e == null )
			{
				throw new ArgumentNullException( "e" );
			}

			EventHandler<RpcTransportErrorEventArgs> handler =
				Interlocked.CompareExchange( ref this._transportError, null, null );
			if ( handler != null )
			{
				handler( this, e );
				return true;
			}

			return false;
		}

		protected EventLoop( EventHandler<RpcTransportErrorEventArgs> errorHandler )
		{
			if ( errorHandler != null )
			{
				this.TransportError += errorHandler;
			}
		}

		public void HandleError( SocketAsyncOperation operation, SocketError error )
		{
			if ( error == System.Net.Sockets.SocketError.Success )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, error ) ) )
			{
				throw new SocketException( ( int )error );
			}
		}

		public void HandleError( RpcTransportOperation operation, RpcErrorMessage rpcError )
		{
			if ( rpcError.IsSuccess )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, rpcError ) ) )
			{
				throw new RpcException( rpcError.Error, rpcError.Description, rpcError.DebugInformation );
			}
		}

		public void HandleError( RpcTransportOperation operation, int messageId, RpcErrorMessage rpcError )
		{
			if ( rpcError.IsSuccess )
			{
				return;
			}

			if ( !this.OnTransportError( new RpcTransportErrorEventArgs( operation, messageId, rpcError ) ) )
			{
				throw new RpcException( rpcError.Error, rpcError.Description, rpcError.DebugInformation );
			}
		}
	}

}
