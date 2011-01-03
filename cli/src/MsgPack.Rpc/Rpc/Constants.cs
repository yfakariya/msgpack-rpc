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
using System.Diagnostics.Contracts;
using System.Text;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Serialization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Define common constants.
	/// </summary>
	internal static class Constants
	{
		public static readonly MessagePackObject[] EmptyArguments = new MessagePackObject[ 0 ];
		public static readonly object[] EmptyObjects = new object[ 0 ];

		public static readonly Encoding MethodEncoding = new UTF8Encoding( false, true );

		private static readonly List<IFilterProvider<RequestMessageSerializationFilter>> _emptyRequestMessageSerializationFilterProviders = new List<IFilterProvider<RequestMessageSerializationFilter>>( 0 );

		public static List<IFilterProvider<RequestMessageSerializationFilter>> EmptyRequestMessageSerializationFilterProviders
		{
			get
			{
				Contract.Assert( _emptyRequestMessageSerializationFilterProviders.Count == 0 );
				return _emptyRequestMessageSerializationFilterProviders;
			}
		}

		private static readonly List<IFilterProvider<RequestMessageDeserializationFilter>> _emptyRequestMessageDeserializationFilterProviders = new List<IFilterProvider<RequestMessageDeserializationFilter>>( 0 );

		public static List<IFilterProvider<RequestMessageDeserializationFilter>> EmptyRequestMessageDeserializationFilterProviders
		{
			get
			{
				Contract.Assert( _emptyRequestMessageDeserializationFilterProviders.Count == 0 );
				return _emptyRequestMessageDeserializationFilterProviders;
			}
		}

		private static readonly List<IFilterProvider<ResponseMessageSerializationFilter>> _emptyResponseMessageSerializationFilterProviders = new List<IFilterProvider<ResponseMessageSerializationFilter>>( 0 );

		public static List<IFilterProvider<ResponseMessageSerializationFilter>> EmptyResponseMessageSerializationFilterProviders
		{
			get
			{
				Contract.Assert( _emptyResponseMessageSerializationFilterProviders.Count == 0 );
				return _emptyResponseMessageSerializationFilterProviders;
			}
		}

		private static readonly List<IFilterProvider<ResponseMessageDeserializationFilter>> _emptyResponseMessageDeserializationFilterProviders = new List<IFilterProvider<ResponseMessageDeserializationFilter>>( 0 );

		public static List<IFilterProvider<ResponseMessageDeserializationFilter>> EmptyResponseMessageDeserializationFilterProviders
		{
			get
			{
				Contract.Assert( _emptyResponseMessageDeserializationFilterProviders.Count == 0 );
				return _emptyResponseMessageDeserializationFilterProviders;
			}
		}

		private static readonly List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> _emptySerializedMessageFilterProviders = new List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>>( 0 );

		public static List<IFilterProvider<SerializedMessageFilter<MessageSerializationContext>>> EmptySerializedMessageFilterProviders
		{
			get
			{
				Contract.Assert( _emptyDeserializedMessageFilterProviders.Count == 0 );
				return _emptySerializedMessageFilterProviders;
			}
		}

		private static readonly List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> _emptyDeserializedMessageFilterProviders = new List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>>( 0 );

		public static List<IFilterProvider<SerializedMessageFilter<MessageDeserializationContext>>> EmptyDeserializedMessageFilterProviders
		{
			get
			{
				Contract.Assert( _emptyDeserializedMessageFilterProviders.Count == 0 );
				return _emptyDeserializedMessageFilterProviders;
			}
		}
	}
}
