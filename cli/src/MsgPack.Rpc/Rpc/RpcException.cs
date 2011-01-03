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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using MsgPack.Rpc.Protocols;
using System.Text;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Rperesents MessagePack-RPC related exception.
	/// </summary>
	/// <remarks>
	///		<para>
	///		</para>
	///		<para>
	///			There is no specification to represent error in MessagePack-RPC,
	///			but de-facto is map which has following structure:
	///			<list type="table">
	///				<listheader>
	///					<term>Key</term>
	///					<description>Value</description>
	///				</listheader>
	///				<item>
	///					<term>ErrorCode</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="Int32"/></para>
	///						<para><strong>Value:</strong>
	///						Error code to identify error type.
	///						</para>
	///					</description>
	///					<term>Description</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="String"/></para>
	///						<para><strong>Value:</strong>
	///						Description of message.
	///						<note>
	///							Note that this value should not contain any sensitive information.
	///							Since detailed error information might be exploit for clackers,
	///							this value should not contain such information.
	///						</note>
	///						</para>
	///					</description>
	///					<term>DebugInformation</term>
	///					<description>
	///						<para><strong>Type:</strong><see cref="String"/></para>
	///						<para><strong>Value:</strong>
	///						Detailed information to debug.
	///						This value is optional.
	///						Server should send this information only when target end point (client) is certainly localhost 
	///						or server is explicitly configured as testing environment.
	///						</para>
	///					</description>
	///				</item>
	///			</list>
	///		</para>
	/// </remarks>
	[Serializable]
	public class RpcException : Exception
	{
		private readonly RpcError _rpcError;

		public RpcError RpcError
		{
			get { return this._rpcError; }
		}

		private readonly string _debugInformation;

		public string DebugInformation
		{
			get { return this._debugInformation; }
		}

		//public RpcException() : this( null, "Unexpected exception occurred in MessagePack-RPC.", null ) { }

		public RpcException( RpcError rpcError, string message, string debugInformation ) : this( rpcError, message, debugInformation, null ) { }
		public RpcException( RpcError rpcError, string message, string debugInformation, Exception inner )
			: base( message, inner )
		{
			this._rpcError = rpcError ?? RpcError.RemoteRuntimeError;
			this._debugInformation = debugInformation;
		}

		protected internal RpcException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }

		protected internal RpcException( MessagePackObject serializedException )
			: base( GetMessage( serializedException ) )
		{
			// TODO: for DOS atack...

			var map = serializedException.AsDictionary();
			MessagePackObject value;
			if ( map.TryGetValue( "__DebugInformation", out value ) )
			{
				this._debugInformation = value.AsString();
			}

			this._rpcError = _errorDictionary[ map[ "RpcError.Identifier" ].AsString() ];

		}

		private static string GetMessage( MessagePackObject serializedException )
		{
			// TODO: for DOS atack...
			try
			{
				return serializedException.AsDictionary()[ "Message" ].AsString();
			}
			catch ( Exception )
			{
				throw new SerializationException();
			}
		}


		public override string ToString()
		{
			// Safe to-string.
			return this.ToString( false );
		}

		public string ToString( bool includesDebugInformation )
		{
			if ( !includesDebugInformation )
			{
				return this.Message;
			}
			else
			{
				return
					String.Format(
						CultureInfo.CurrentCulture,
						"{1}: [{2}({3})] {4}{0}{5}",
						Environment.NewLine,
						this.GetType().FullName,
						this._rpcError.Identifier,
						this._rpcError.ErrorCode,
						this.Message,
						this.DebugInformation
					);
			}
		}

		private static readonly Dictionary<string, RpcError> _errorDictionary =
			typeof( RpcError ).GetFields( BindingFlags.Public | BindingFlags.Static )
			.Where( field => field.FieldType == typeof( RpcError ) )
			.Select( field => field.GetValue( null ) )
			.OfType<RpcError>()
			.ToDictionary( item => item.Identifier );

		private static readonly MessagePackObject _errorCodeKey = Encoding.UTF8.GetBytes( "ErrorCode" );
		private static readonly MessagePackObject _descriptionKey = Encoding.UTF8.GetBytes( "Description" );
		private static readonly MessagePackObject _debugInformationKey = Encoding.UTF8.GetBytes( "DebugInformation" );

		public static RpcException FromMessage( MessagePackObject error, MessagePackObject errorDetail )
		{
			if ( error.IsNil )
			{
				throw new ArgumentException( "'error' must not be nil.", "error" );
			}

			// Recommeded path
			if ( error.IsTypeOf<byte[]>().GetValueOrDefault() )
			{
				string identifier = null;
				try
				{
					identifier = error.AsString();
				}
				catch ( InvalidOperationException ) { }

				int? errorCode = null;
				string description = null;
				string debugInformation = null;

				if ( errorDetail.IsTypeOf<IDictionary<MessagePackObject, MessagePackObject>>().GetValueOrDefault() )
				{
					var asDictionary = errorDetail.AsDictionary();
					MessagePackObject value;
					if ( asDictionary.TryGetValue( _errorCodeKey, out value ) && value.IsTypeOf<int>().GetValueOrDefault() )
					{
						errorDetail = value.AsInt32();
					}

					if ( asDictionary.TryGetValue( _descriptionKey, out value ) && value.IsTypeOf<byte[]>().GetValueOrDefault() )
					{
						try
						{
							description = value.AsString();
						}
						catch ( InvalidOperationException ) { }
					}

					if ( asDictionary.TryGetValue( _debugInformationKey, out value ) && value.IsTypeOf<byte[]>().GetValueOrDefault() )
					{
						try
						{
							debugInformation = value.AsString();
						}
						catch ( InvalidOperationException ) { }
					}
				}

				if ( identifier != null || errorCode != null )
				{
					RpcError rpcError = RpcError.FromIdentifier( identifier, errorCode );
					return new RpcException( rpcError, description, debugInformation );
				}
			}

			// Other path.
			return new UnexpcetedRpcException( error, errorDetail );
		}
		
		internal static RpcException FromRpcError( RpcErrorMessage serializationError )
		{
			return serializationError.Error.ToException( serializationError.Error, serializationError.Description, serializationError.DebugInformation );
		}


		public static RpcException FromSocketException( SocketException inner )
		{
			return FromSocketError( inner.SocketErrorCode, inner );
		}

		public static RpcException FromSocketError( SocketError socketError )
		{
			return FromSocketError( socketError, null );
		}

		private static RpcException FromSocketError( SocketError socketError, SocketException inner )
		{
			var socketException = inner ?? new SocketException( ( int )socketError );
			switch ( socketError )
			{
				case SocketError.Success:
				{
					Debug.Fail( "'socketError' is 'Success'." );
					throw new InvalidOperationException( "'socketError' is 'Success'." );
				}
				case SocketError.ConnectionRefused:
				{
					return new RpcTransportException( RpcError.ConnectionRefusedError, "Connection refused from remote host.", socketException.Message, inner );
				}
				case SocketError.TimedOut:
				{
					return new RpcTransportException( RpcError.ConnectionTimeoutError, "Connection timed out.", socketException.Message, inner );
				}
				case SocketError.HostDown:
				{
					return new RpcServerUnavailableException( RpcError.ServerBusyError, "Server is down.", socketException.Message, inner );
				}
				case SocketError.HostNotFound:
				case SocketError.HostUnreachable:
				case SocketError.NetworkDown:
				case SocketError.NoData:
				{
					return new RpcTransportException( RpcError.NetworkUnreacheableError, "Cannot reach specified remote end point.", socketException.Message, inner );
				}
				default:
				{
					return new RpcTransportException( RpcError.TransportError, "Other transportation error is occured.", socketException.Message, inner );
				}
			}
		}
	}
}
