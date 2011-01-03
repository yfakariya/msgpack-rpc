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
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using MsgPack.Rpc.Protocols;
using System.Collections.Generic;
using System.Reflection;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Represents pre-defined MsgPack-RPC error metadata.
	/// </summary>
	/// <seealso cref="https://gist.github.com/470667/d33136f74584381bdb58b6444abfcb4a8bbe8abc"/>
	public sealed class RpcError
	{
		#region -- Built-in Errors --

		/// <summary>
		///		Cannot get response from server.
		///		Details are unknown at all, for instance, message might reach server.
		///		It might be success when you retry.
		/// </summary>
		public static readonly RpcError TimeoutError =
			new RpcError(
				"RPCError.TimeoutError",
				-60,
				"Request has been timeout.",
				typeof( RpcTimeoutException ),
				( rpcError, message, debugInformation ) => new RpcTimeoutException( rpcError, message, debugInformation ),
				( info, context ) => new RpcTimeoutException( info, context )
			);

		/// <summary>
		///		Cannot initiate transferring message.
		///		It may was network failure, was configuration issue, or failed to handshake.
		/// </summary>
		public static readonly RpcError TransportError =
			new RpcError(
				"RPCError.ClientError.TransportError",
				-50,
				"Cannot initiate transferring message.",
				typeof( RpcTransportException ),
				( rpcError, message, debugInformation ) => new RpcTransportException( rpcError, message, debugInformation ),
				( info, context ) => new RpcTransportException( info, context )
			);

		/// <summary>
		///		Cannot reach specified remote end point.
		///		This error is transport protocol specific.
		/// </summary>
		public static readonly RpcError NetworkUnreacheableError =
			new RpcError(
				"RPCError.ClientError.TranportError.NetworkUnreacheableError",
				-51,
				"Cannot reach specified remote end point.",
				typeof( RpcTransportException ),
				( rpcError, message, debugInformation ) => new RpcTransportException( rpcError, message, debugInformation ),
				( info, context ) => new RpcTransportException( info, context )
			);

		/// <summary>
		///		Connection was refused explicitly by remote end point.
		///		It should fail when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		public static readonly RpcError ConnectionRefusedError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionRefusedError",
				-52,
				"Connection was refused explicitly by remote end point.",
				typeof( RpcTransportException ),
				( rpcError, message, debugInformation ) => new RpcTransportException( rpcError, message, debugInformation ),
				( info, context ) => new RpcTransportException( info, context )
			);

		/// <summary>
		///		Connection timout was occurred.
		///		It might be success when you retry.
		///		This error is connection oriented transport protocol specific.
		/// </summary>
		public static readonly RpcError ConnectionTimeoutError =
			new RpcError(
				"RPCError.ClientError.TranportError.ConnectionTimeoutError",
				-53,
				"Connection timout was occurred.",
				typeof( RpcTransportException ),
				( rpcError, message, debugInformation ) => new RpcTransportException( rpcError, message, debugInformation ),
				( info, context ) => new RpcTransportException( info, context )
			);

		/// <summary>
		///		Message was refused explicitly by remote end point.
		/// </summary>
		/// <remarks>
		///		<para>
		///			End point issues this error when:
		///			<list type="bullet">
		///				<item>Couild not deserialize the message.</item>
		///				<item>Message structure of deserialized message was wrong as MessagePack-RPC protocol.</item>
		///				<item>Any value of message was wrong as the protocol.</item>
		///			</list>
		///		</para>
		///		<para>
		///			It may be caused when:
		///			<list type="bullet">
		///				<item>Some deserializing issues were occurred.</item>
		///				<item>Unexpected item type was found as the protocol (e.g. arguments field was not array).</item>
		///				<item>Unexpected item value was found as the protocol (e.g. undefined message type field).</item>
		///			</list>
		///		</para>
		///		<para>
		///			The root cause of this issue might be:
		///			<list type="bullet">
		///				<item>There are some bugs on used library in client or server.</item>
		///				<item>Versions of MessagePack library in client and server were not compatible.</item>
		///				<item>Versions of MessagePack-RPC library in client and server were not compatible.</item>
		///				<item>Packet was changed unexpectedly.</item>
		///			</list>
		///		</para>
		/// </remarks>
		public static readonly RpcError MessageRefusedError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError",
				-40,
				"Message was refused explicitly by remote end point.",
				typeof( RpcTransportException ),
				( rpcError, message, debugInformation ) => new RpcProtocolException( rpcError, message, debugInformation ),
				( info, context ) => new RpcProtocolException( info, context )
			);

		/// <summary>
		///		Message was refused explicitly by remote end point due to it was too large.
		///		Structure may be right, but message was simply too large or some portions might be corruptted.
		/// </summary>
		/// <remarks>
		///		<para>
		///			It may be caused when:
		///			<list type="bullet">
		///				<item>Message is too large to be expected by remote end point.</item>
		///			</list>
		///		</para>
		///		<para>
		///			The root cause of this issue might be:
		///			<list type="bullet">
		///				<item>Versions of MessagePack library in client and server were not compatible.</item>
		///				<item>Versions of MessagePack-RPC library in client and server were not compatible.</item>
		///				<item>Packet was changed unexpectedly.</item>
		///				<item>Malicious issuer tried to send invalid message.</item>
		///				<item>Expected value by remote end point was simply too small.</item>
		///			</list>
		///		</para>
		/// </remarks>
		public static readonly RpcError MessageTooLargeError =
			new RpcError(
				"RPCError.ClientError.MessageRefusedError.MessageTooLargeError",
				-41, "Message is too large.",
				typeof( RpcMessageTooLongException ),
				( rpcError, message, debugInformation ) => new RpcMessageTooLongException( rpcError, message, debugInformation ),
				( info, context ) => new RpcMessageTooLongException( info, context )
			);

		/// <summary>
		///		Failed to call specified method.
		///		Message was certainly reached and the structure was right, but failed to call method.
		/// </summary>
		public static readonly RpcError CallError =
			new RpcError(
				"RPCError.ClientError.CallError",
				-20,
				"Failed to call specified method.",
				typeof( RpcMethodInvocationException ),
				( rpcError, message, debugInformation ) => new RpcException( rpcError, message, debugInformation ),
				( info, context ) => new RpcMethodInvocationException( info, context )
			);

		/// <summary>
		///		Specified method was not found.
		/// </summary>
		public static readonly RpcError NoMethodError =
			new RpcError(
				"RPCError.ClientError.CallError.NoMethodError",
				-21,
				"Specified method was not found.",
				typeof( RpcMissingMethodException ),
				( rpcError, message, debugInformation ) => new RpcException( rpcError, message, debugInformation ),
				( info, context ) => new RpcMissingMethodException( info, context )
			);

		/// <summary>
		///		Some argument(s) were wrong.
		/// </summary>
		public static readonly RpcError ArgumentError =
			new RpcError(
				"RPCError.ClientError.CallError.ArgumentError",
				-22,
				"Some argument(s) were wrong.",
				typeof( RpcArgumentException ),
				( rpcError, message, debugInformation ) => new RpcException( rpcError, message, debugInformation ),
				( info, context ) => new RpcArgumentException( info, context )
			);

		/// <summary>
		///		Server cannot process received message.
		///		Other server might process your request.
		/// </summary>
		public static readonly RpcError ServerError =
			new RpcError(
				"RPCError.ServerError",
				-30,
				"Server cannot process received message.",
				typeof( RpcServerUnavailableException ),
				( rpcError, message, debugInformation ) => new RpcServerUnavailableException( rpcError, message, debugInformation ),
				( info, context ) => new RpcServerUnavailableException( info, context )
			);

		/// <summary>
		///		Server is busy.
		///		Other server may process your request.
		/// </summary>
		public static readonly RpcError ServerBusyError =
			new RpcError(
				"RPCError.ServerError.ServerBusyError",
				-31,
				"Server is busy.",
				typeof( RpcServerUnavailableException ),
				( rpcError, message, debugInformation ) => new RpcServerUnavailableException( rpcError, message, debugInformation ),
				( info, context ) => new RpcServerUnavailableException( info, context )
			);

		/// <summary>
		///		Internal runtime error in remote end point.
		/// </summary>
		public static readonly RpcError RemoteRuntimeError =
			new RpcError(
				"RPCError.RemoteRuntimeError",
				-10,
				"Remote end point failed to process request.",
				typeof( RpcException ),
				( rpcError, message, debugInformation ) => new RpcException( rpcError, message, debugInformation ),
				( info, context ) => new RpcException( info, context )
			);

		#endregion -- Built-in Errors --

		private static readonly Dictionary<string, RpcError> _identifierDictionary = new Dictionary<string, RpcError>();
		private static readonly Dictionary<int, RpcError> _errorCodeDictionary = new Dictionary<int, RpcError>();

		static RpcError()
		{
			foreach ( FieldInfo field in
				typeof( RpcError ).FindMembers(
					MemberTypes.Field,
					BindingFlags.Static | BindingFlags.Public,
					( member, criteria ) => ( member as FieldInfo ).FieldType.Equals( criteria ),
					typeof( RpcError )
				)
			)
			{
				var builtInError = field.GetValue( null ) as RpcError;
				_identifierDictionary.Add( builtInError.Identifier, builtInError );
				_errorCodeDictionary.Add( builtInError.ErrorCode, builtInError );
			}
		}

		private readonly string _identifier;

		public string Identifier
		{
			get { return this._identifier; }
		}

		private readonly int _errorCode;

		public int ErrorCode
		{
			get { return this._errorCode; }
		}

		private readonly string _defaultMessageInvariant;

		public string DefaultMessageInvariant
		{
			get { return _defaultMessageInvariant; }
		}

		public string DefaultMessage
		{
			get
			{
				// TODO: localiation key: Idnentifier ".DefaultMessage"
				return this.DefaultMessageInvariant;
			}
		}

		private readonly Type _exceptionType;
		private readonly Func<SerializationInfo, StreamingContext, RpcException> _exceptionDeserializer;
		private readonly Func<RpcError, string, string, RpcException> _exceptionFactory;

		internal RpcException ToException( RpcError rpcError, string message, string debugInformation )
		{
			return this._exceptionFactory( rpcError, message, debugInformation );
		}

		private RpcError( string identifier, int errorCode, string defaultMessageInvariant, Type exceptionType, Func<RpcError, string, string, RpcException> exceptionFactory, Func<SerializationInfo, StreamingContext, RpcException> exceptionDeserializer )
		{
			this._identifier = identifier;
			this._errorCode = errorCode;
			this._defaultMessageInvariant = defaultMessageInvariant;
			this._exceptionType = exceptionType;
			this._exceptionFactory = exceptionFactory;
			this._exceptionDeserializer = exceptionDeserializer;
		}

		public static RpcError CustomError( string identifier, int errorCode )
		{
			if ( identifier == null )
			{
				throw new ArgumentNullException( "identifier" );
			}

			if ( String.IsNullOrWhiteSpace( identifier ) )
			{
				throw new ArgumentException( "'identifier' cannot be empty.", "identifier" );
			}

			if ( errorCode < 0 )
			{
				throw new ArgumentOutOfRangeException( "errorCode", errorCode, "Application error code must be grator than or equal to 0." );
			}

			Contract.EndContractBlock();

			return
				new RpcError(
					identifier.StartsWith( "RPCError.", StringComparison.Ordinal ) ? identifier : "RPCError." + identifier,
					errorCode,
					"Application throw exception.",
					typeof( RpcFaultException ),
					( rpcError, message, debugInformation ) => new RpcFaultException( rpcError, message, debugInformation ),
					( info, context ) => new RpcFaultException( info, context )
				);
		}

		private const string _unexpectedErrorIdentifier = "RPCError.RemoteError.UnexpectedError";
		private const int _unexpectedErrorCode = Int32.MaxValue;

#warning Factories should be deleted.
		internal static readonly RpcError Unexpected =
			new RpcError(
				_unexpectedErrorIdentifier,
				_unexpectedErrorCode,
				"Unexpected RPC error is occurred.",
				typeof( UnexpcetedRpcException ),
				null,
				null
			);

		public static RpcError FromIdentifier( string identifier, int? errorCode )
		{
			RpcError result;
			if ( errorCode != null && _errorCodeDictionary.TryGetValue( errorCode.Value, out result ) )
			{
				return result;
			}

			if ( identifier != null && _identifierDictionary.TryGetValue( identifier, out result ) )
			{
				return result;
			}

			return CustomError( String.IsNullOrWhiteSpace( identifier ) ? _unexpectedErrorIdentifier : identifier, errorCode ?? _unexpectedErrorCode );
		}
	}
}
