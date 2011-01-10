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
using System.Globalization;

namespace MsgPack.Collections
{
	internal static class MessagePackObjectDictionary
	{
		public static bool TryGetString( MessagePackObject mayBeDictionary, MessagePackObject key, Func<string, Exception> createException, out string result )
		{
			return TryGetValue( mayBeDictionary, key, value => value.AsString(), createException, "UTF-8 string", out result );
		}

		public static bool TryGetTimeSpan( MessagePackObject mayBeDictionary, MessagePackObject key, Func<string, Exception> createException, out TimeSpan result )
		{
			return TryGetValue( mayBeDictionary, key, value => new TimeSpan( value.AsInt64() ), createException, typeof( Int64 ).FullName, out result );
		}

		public static bool TryGetArray( MessagePackObject mayBeDictionary, MessagePackObject key, Func<string, Exception> createException, out IList<MessagePackObject> result )
		{
			return TryGetValue( mayBeDictionary, key, value => value.AsList(), createException, typeof( IList<MessagePackObject> ).FullName, out result );
		}

		public static bool TryGetObject( MessagePackObject mayBeDictionary, MessagePackObject key, Func<string, Exception> createException, out MessagePackObject result )
		{
			return TryGetValue( mayBeDictionary, key, value => value, createException, typeof( MessagePackObject ).FullName, out result );
		}

		private static bool TryGetValue<T>( MessagePackObject mayBeDictionary, MessagePackObject key, Func<MessagePackObject, T> convert, Func<string, Exception> createException, string typeName, out T result )
		{
			if ( !mayBeDictionary.IsDictionary )
			{
				if ( createException != null )
				{
					throw createException( "Message is not map." );
				}

				result = default( T );
				return false;
			}

			MessagePackObject value;
			if ( !mayBeDictionary.AsDictionary().TryGetValue( key, out value ) )
			{
				if ( createException != null )
				{
					throw createException( String.Format( CultureInfo.CurrentCulture, "Message does not contain '{0}'.", key.AsString() ) );
				}

				result = default( T );
				return false;
			}

			try
			{
				result = convert( value );
				return true;
			}
			catch ( InvalidOperationException )
			{
				if ( createException != null )
				{
					throw createException( String.Format( CultureInfo.CurrentCulture, "Message is not {0}.", typeName ) );
				}

				result = default( T );
				return false;
			}
		}
	}
}
