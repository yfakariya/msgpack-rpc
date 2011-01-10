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
using MsgPack.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

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
	public partial class RpcException : Exception
	{
		private readonly RpcError _rpcError;

		/// <summary>
		///		Get metadata of error.
		/// </summary>
		/// <value>
		///		Metadata of error. This value will not be null.
		/// </value>
		public RpcError RpcError
		{
			get { return this._rpcError; }
		}

		private readonly string _debugInformation;

		/// <summary>
		///		Get debug information of error.
		/// </summary>
		/// <value>
		///		Debug information of error.
		///		This value may be null for security reason, and its contents are for developers, not end users.
		/// </value>
		public string DebugInformation
		{
			get { return this._debugInformation; }
		}

		/// <summary>
		///		Initialize new instance which represents specified error with specified message..
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcException( RpcError rpcError, string message, string debugInformation ) : this( rpcError, message, debugInformation, null ) { }

		/// <summary>
		///		Initialize new instance which represents specified error with specified message and inner exception.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <param name="inner">
		///		Exception which caused this error.
		/// </param>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcException( RpcError rpcError, string message, string debugInformation, Exception inner )
			: base( message, inner )
		{
			this._rpcError = rpcError ?? RpcError.RemoteRuntimeError;
			this._debugInformation = debugInformation;
		}

		/// <summary>
		///		Initialize new instance with serialized data.
		/// </summary>
		/// <param name="info"><see cref="SerializationInfo"/> which has serialized data.</param>
		/// <param name="context"><see cref="StreamingContext"/> which has context information about transport source or destination.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="info"/> is null.
		/// </exception>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="info"/>.
		/// </exception>
		protected RpcException( SerializationInfo info, StreamingContext context ) : base( info, context ) { }

		public override string ToString()
		{
			// Safe to-string.
			return this.ToString( false );
		}

		public string ToString( bool includesDebugInformation )
		{
			if ( !includesDebugInformation || this._remoteExceptions == null )
			{
				return base.ToString();
			}
			else
			{
				Contract.Assert( this._remoteExceptions != null );

				// <Type>: <Message> ---> <InnerType1>: <InnerMessage1> ---> <InnerType2>: <InnerMessage2> ---> ...
				// <ServerInnerStackTraceN>
				//    --- End of inner exception stack trace ---
				// <ServerInnerStackTrace1>
				//    --- End of inner exception stack trace ---
				// 
				// Server statck trace:
				// <ServerStackTrace>
				// 
				// Exception rethrown at[N]:
				// <ClientInnerStackTraceN>
				//    --- End of inner exception stack trace ---
				// <ClientInnerStackTrace1>
				//    --- End of inner exception stack trace ---
				// <StackTrace>
				StringBuilder stringBuilder = new StringBuilder();
				// Build <Type>: <Message> chain
				this.BuildExceptionMessage( stringBuilder );
				// Build stacktrace chain.
				this.BuildExceptionStackTrace( stringBuilder );

				return stringBuilder.ToString();
			}
		}

		private void BuildExceptionMessage( StringBuilder stringBuilder )
		{
			stringBuilder.Append( this.GetType().FullName ).Append( ": " ).Append( this.Message );

			if ( this.InnerException != null )
			{
				Contract.Assert( this._remoteExceptions == null );

				for ( var inner = this.InnerException; inner != null; inner = inner.InnerException )
				{
					var asRpcException = inner as RpcException;
					if ( asRpcException != null )
					{
						asRpcException.BuildExceptionMessage( stringBuilder );
					}
					else
					{
						stringBuilder.Append( " ---> " ).Append( inner.GetType().FullName ).Append( ": " ).Append( inner.Message );
					}
				}
			}
			else if ( this._remoteExceptions != null )
			{
				foreach ( var remoteException in this._remoteExceptions )
				{
					stringBuilder.Append( " ---> " ).Append( remoteException.TypeName ).Append( ": " ).Append( remoteException.Message );
				}
			}
		}

		private void BuildExceptionStackTrace( StringBuilder stringBuilder )
		{
			if ( this.InnerException != null )
			{
				Contract.Assert( this._remoteExceptions == null );

				for ( var inner = this.InnerException; inner != null; inner = inner.InnerException )
				{
					var asRpcException = inner as RpcException;
					if ( asRpcException != null )
					{
						asRpcException.BuildExceptionStackTrace( stringBuilder );
					}
					else
					{
						BuildGeneralStackTrace( inner, stringBuilder );
					}

					stringBuilder.Append( "   --- End of inner exception stack trace ---" );
				}
			}
			else if ( this._remoteExceptions != null && this._remoteExceptions.Length > 0 )
			{
				for ( int i = 0; i < this._remoteExceptions.Length; i++ )
				{
					if ( i > 0
						&& this._remoteExceptions[ i ].Hop != this._remoteExceptions[ i - 1 ].Hop
						&& this._remoteExceptions[ i ].TypeName == this._remoteExceptions[ i - 1 ].TypeName
					)
					{
						// Serialized -> Deserialized case
						stringBuilder.AppendFormat( "Exception transferred at[{0}]:", this._remoteExceptions[ i - 1 ].Hop );
					}
					else
					{
						// Inner exception case
						stringBuilder.Append( "   --- End of inner exception stack trace ---" );
					}

					foreach ( var frame in this._remoteExceptions[ i ].StackTrace )
					{
						WriteStackFrame( stringBuilder, frame );
					}
				}

				stringBuilder.AppendFormat( "Exception transferred at[{0}]:", this._remoteExceptions[ this._remoteExceptions.Length - 1 ].Hop ).AppendLine();
			}

			BuildGeneralStackTrace( this, stringBuilder );
		}

		private static void BuildGeneralStackTrace( Exception target, StringBuilder stringBuilder )
		{
			var stackTrace = new StackTrace( target, true );
			for ( int i = 0; i < stackTrace.FrameCount; i++ )
			{
				WriteStackFrame( stringBuilder, stackTrace.GetFrame( stackTrace.FrameCount - ( i + 1 ) ) );
			}
		}

		private static void WriteStackFrame( StringBuilder stringBuilder, StackFrame frame )
		{
			const string at = "at";
			stringBuilder.AppendFormat( "   " + at + "{0}", frame.GetMethod() );
			if ( frame.GetFileName() != null )
			{
				stringBuilder.AppendFormat( " " + at + " {0}", frame.GetFileName() );

				if ( frame.GetFileLineNumber() > 0 )
				{
					stringBuilder.AppendFormat( ":line {0}", frame.GetFileLineNumber() );
				}
			}
		}

		private static void WriteStackFrame( StringBuilder stringBuilder, RemoteStackFrame frame )
		{
			const string at = "at";
			stringBuilder.AppendFormat( "   " + at + "{0}", frame.MethodSignature );
			if ( frame.FileName != null )
			{
				stringBuilder.AppendFormat( " " + at + " {0}", frame.FileName );

				if ( frame.FileLineNumber > 0 )
				{
					stringBuilder.AppendFormat( ":line {0}", frame.FileLineNumber );
				}
			}
		}

		private static readonly MessagePackObject _errorCodeKeyUtf8 = MessagePackConvert.EncodeString( "ErrorCode" );

		public static RpcException FromMessage( MessagePackObject error, MessagePackObject errorDetail )
		{
			// TODO: Application specific customization
			// TODO: Application specific exception class

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

				if ( errorDetail.IsTypeOf<IDictionary<MessagePackObject, MessagePackObject>>().GetValueOrDefault() )
				{
					var asDictionary = errorDetail.AsDictionary();
					MessagePackObject value;
					if ( asDictionary.TryGetValue( _errorCodeKeyUtf8, out value ) && value.IsTypeOf<int>().GetValueOrDefault() )
					{
						errorCode = value.AsInt32();
					}
				}

				if ( identifier != null || errorCode != null )
				{
					RpcError rpcError = RpcError.FromIdentifier( identifier, errorCode );
					return rpcError.ToException( errorDetail );
				}
			}

			// Other path.
			return new UnexpcetedRpcException( error, errorDetail );
		}

		internal static RpcException FromRpcError( RpcErrorMessage serializationError )
		{
			return serializationError.Error.ToException( serializationError.Detail );
		}

		//public static RpcException FromSocketException( SocketException inner )
		//{
		//    return FromSocketError( inner.SocketErrorCode, inner );
		//}

		//public static RpcException FromSocketError( SocketError socketError )
		//{
		//    return FromSocketError( socketError, null );
		//}

		//private static RpcException FromSocketError( SocketError socketError, SocketException inner )
		//{
		//    var socketException = inner ?? new SocketException( ( int )socketError );
		//    switch ( socketError )
		//    {
		//        case SocketError.Success:
		//        {
		//            Debug.Fail( "'socketError' is 'Success'." );
		//            throw new InvalidOperationException( "'socketError' is 'Success'." );
		//        }
		//        case SocketError.ConnectionRefused:
		//        {
		//            return new RpcTransportException( RpcError.ConnectionRefusedError, "Connection refused from remote host.", socketException.Message, inner );
		//        }
		//        case SocketError.TimedOut:
		//        {
		//            return new RpcTransportException( RpcError.ConnectionTimeoutError, "Connection timed out.", socketException.Message, inner );
		//        }
		//        case SocketError.HostDown:
		//        {
		//            return new RpcServerUnavailableException( RpcError.ServerBusyError, "Server is down.", socketException.Message, inner );
		//        }
		//        case SocketError.HostNotFound:
		//        case SocketError.HostUnreachable:
		//        case SocketError.NetworkDown:
		//        case SocketError.NoData:
		//        {
		//            return new RpcTransportException( RpcError.NetworkUnreacheableError, "Cannot reach specified remote end point.", socketException.Message, inner );
		//        }
		//        default:
		//        {
		//            return new RpcTransportException( RpcError.TransportError, "Other transportation error is occured.", socketException.Message, inner );
		//        }
		//    }
		//}
	}
}
