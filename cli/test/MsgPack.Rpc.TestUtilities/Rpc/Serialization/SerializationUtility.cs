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
using MsgPack.Collections;
using MsgPack.Rpc.Protocols;

namespace MsgPack.Rpc.Serialization
{
	public static class SerializationUtility
	{
		public static RpcOutputBuffer SerializeRequest( int messageId, String method, params object[] arguments )
		{
			RpcOutputBuffer result = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error =
				new RequestMessageSerializer(
						Arrays<IFilterProvider<RequestMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<RequestMessageDeserializationFilter>>.Empty,
						null
				).Serialize( messageId, method, arguments, result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}

		public static RpcOutputBuffer SerializeNotification( String method, params object[] arguments )
		{
			RpcOutputBuffer result = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error =
				new RequestMessageSerializer(
						Arrays<IFilterProvider<RequestMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<RequestMessageDeserializationFilter>>.Empty,
						null
				).Serialize( null, method, arguments, result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}

		public static RequestMessage DeserializeRequestOrNotification( IEnumerable<byte> input )
		{
			RequestMessage result;
			var error =
				new RequestMessageSerializer(
						Arrays<IFilterProvider<RequestMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<RequestMessageDeserializationFilter>>.Empty,
						null
				).Deserialize( input, out result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}

		public static RpcOutputBuffer SerializeResponse( int messageId )
		{
			var result = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error =
				new ResponseMessageSerializer(
						Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
						null
				).Serialize( messageId, null, true, null, result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}


		public static RpcOutputBuffer SerializeResponse( int messageId, object returnValue )
		{
			var result = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error =
				new ResponseMessageSerializer(
						Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
						null
				).Serialize( messageId, returnValue, false, null, result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}

		public static RpcOutputBuffer SerializeResponse( int messageId, RpcException exception )
		{
			var result = new RpcOutputBuffer( ChunkBuffer.CreateDefault() );
			var error =
				new ResponseMessageSerializer(
						Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
						null
				).Serialize( messageId, null, false, exception, result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}

		public static ResponseMessage DeserializeResponse( IEnumerable<byte> input )
		{
			ResponseMessage result;
			var error =
				new ResponseMessageSerializer(
						Arrays<IFilterProvider<ResponseMessageSerializationFilter>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>.Empty,
						Arrays<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>.Empty,
						Arrays<IFilterProvider<ResponseMessageDeserializationFilter>>.Empty,
						null
				).Deserialize( input, out result );
			if ( !error.IsSuccess )
			{
				throw error.ToException();
			}

			return result;
		}
	}
}
