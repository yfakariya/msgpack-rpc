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

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Define convinience factory methods for test.
	///	</summary>
	public static class SerializerFactory
	{
		/// <summary>
		///		Create <see cref="RequestMessageSerializer"/> with specified tracer(s).
		/// </summary>
		/// <param name="serializingRequestTracer">Tracer for serializing request.</param>
		/// <param name="serializedRequestTracer">Tracer for serialized request binary stream.</param>
		/// <param name="deserializingRequestTracer">Tracer for deserializing request binary stream.</param>
		/// <param name="deserializedRequestTracer">Tracer for deserialized request.</param>
		/// <returns><see cref="RequestMessageSerializer"/> with specified tracer(s).</returns>
		public static RequestMessageSerializer CreateRequestMessageSerializerWithTracer(
			SerializingRequestTracingFilterProvider serializingRequestTracer,
			SerializedRequestTracingFilterProvider serializedRequestTracer,
			DeserializingRequestTracingFilterProvider deserializingRequestTracer,
			DeserializedRequestTracingFilterProvider deserializedRequestTracer
			)
		{
			return
				new RequestMessageSerializer(
					serializingRequestTracer == null ? new IFilterProvider<RequestMessageSerializationFilter>[] { serializingRequestTracer } : null,
					serializedRequestTracer == null ? new IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>[] { serializedRequestTracer } : null,
					deserializingRequestTracer == null ? new IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>[] { deserializingRequestTracer } : null,
					deserializedRequestTracer == null ? new IFilterProvider<RequestMessageDeserializationFilter>[] { deserializedRequestTracer } : null,
					null
				);
		}

		/// <summary>
		///		Create <see cref="ResponseMessageSerializer"/> with specified tracer(s).
		/// </summary>
		/// <param name="serializingResponseTracer">Tracer for serializing response.</param>
		/// <param name="serializedResponseTracer">Tracer for serialized response binary stream.</param>
		/// <param name="deserializingResponseTracer">Tracer for deserializing response binary stream.</param>
		/// <param name="deserializedResponseTracer">Tracer for deserialized response.</param>
		/// <returns><see cref="ResponseMessageSerializer"/> with specified tracer(s).</returns>
		public static ResponseMessageSerializer CreateResponseMessageSerializerWithTracer(
			SerializingResponseTracingFilterProvider serializingResponseTracer,
			SerializedResponseTracingFilterProvider serializedResponseTracer,
			DeserializingResponseTracingFilterProvider deserializingResponseTracer,
			DeserializedResponseTracingFilterProvider deserializedResponseTracer
			)
		{
			return
				new ResponseMessageSerializer(
					serializingResponseTracer == null ? new IFilterProvider<ResponseMessageSerializationFilter>[] { serializingResponseTracer } : null,
					serializedResponseTracer == null ? new IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>[] { serializedResponseTracer } : null,
					deserializingResponseTracer == null ? new IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>[] { deserializingResponseTracer } : null,
					deserializedResponseTracer == null ? new IFilterProvider<ResponseMessageDeserializationFilter>[] { deserializedResponseTracer } : null,
					null
				);
		}
	}
}
