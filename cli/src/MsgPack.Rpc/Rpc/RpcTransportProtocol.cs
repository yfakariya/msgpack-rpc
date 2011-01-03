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
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents underlying transportation protocol of MessagePack-RPC.
	/// </summary>
	public struct RpcTransportProtocol : IEquatable<RpcTransportProtocol>, IFormattable
	{
		public static readonly RpcTransportProtocol TcpIp = new RpcTransportProtocol( Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		public static readonly RpcTransportProtocol TcpIpV4 = new RpcTransportProtocol( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		public static readonly RpcTransportProtocol TcpIpV6 = new RpcTransportProtocol( AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp );
		public static readonly RpcTransportProtocol UdpIp = new RpcTransportProtocol( Socket.OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		public static readonly RpcTransportProtocol UdpIpV4 = new RpcTransportProtocol( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		public static readonly RpcTransportProtocol UdpIpV6 = new RpcTransportProtocol( AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp );

		private readonly AddressFamily _addressFamily;

		public AddressFamily AddressFamily
		{
			get { return this._addressFamily; }
		}

		private readonly SocketType _socketType;

		public SocketType SocketType
		{
			get { return this._socketType; }
		}

		private readonly ProtocolType _protocolType;

		public ProtocolType ProtocolType
		{
			get { return this._protocolType; }
		}

		public RpcTransportProtocol( AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType )
		{
			this._addressFamily = addressFamily;
			this._socketType = socketType;
			this._protocolType = protocolType;
		}

		public static RpcTransportProtocol ForSocket( Socket socket )
		{
			if ( socket == null )
			{
				throw new ArgumentNullException( "socket" );
			}

			Contract.EndContractBlock();

			return new RpcTransportProtocol( socket.AddressFamily, socket.SocketType, socket.ProtocolType );
		}

		public Socket CreateSocket()
		{
			return new Socket( this._addressFamily, this._socketType, this._protocolType );
		}

		public override string ToString()
		{
			return this.ToString( "G" );
		}

		public string ToString( string format )
		{
			return
				"{AddressFamily=" +
				this._addressFamily.ToString( format ) +
				", SocketType=" +
				this._socketType.ToString( format ) +
				", ProtocolType=" +
				this._protocolType.ToString( format ) +
				"}";
		}

		public string ToString( string format, IFormatProvider formatProvider )
		{
			return this.ToString( format );
		}

		public override int GetHashCode()
		{
			return this._addressFamily.GetHashCode() ^ this._protocolType.GetHashCode() ^ this._socketType.GetHashCode();
		}

		public override bool Equals( object obj )
		{
			if ( !( obj is RpcTransportProtocol ) )
			{
				return false;
			}
			else
			{
				return this.Equals( ( RpcTransportProtocol )obj );
			}
		}

		public bool Equals( RpcTransportProtocol other )
		{
			return
				this._addressFamily == other._addressFamily
				&& this._protocolType == other._protocolType
				&& this._socketType == other._socketType;
		}

		public static bool operator ==( RpcTransportProtocol left, RpcTransportProtocol right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( RpcTransportProtocol left, RpcTransportProtocol right )
		{
			return !left.Equals( right );
		}
	}
}
