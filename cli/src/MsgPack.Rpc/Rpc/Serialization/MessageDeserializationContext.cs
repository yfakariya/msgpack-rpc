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
using System.Diagnostics.Contracts;
using System.Globalization;

namespace MsgPack.Rpc.Serialization
{
	public abstract class MessageDeserializationContext : SerializationErrorSink
	{
		private readonly RpcInputBuffer _buffer;

		private int _processed;
		private readonly int? _maxLength;

		protected MessageDeserializationContext( RpcInputBuffer buffer, int? maxLength )
		{
			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			if ( maxLength != null && maxLength <= 0 )
			{
				throw new ArgumentOutOfRangeException( "maxLength" );
			}

			Contract.EndContractBlock();

			this._buffer = buffer;
			this._maxLength = maxLength;
		}

		/// <summary>
		///		Read bytes from buffer.
		/// </summary>
		/// <returns>Iterator for read bytes from input buffer.</returns>
		internal IEnumerable<byte> ReadBytes()
		{
			int limit = this._maxLength ?? Int32.MaxValue;
			foreach ( var b in this._buffer )
			{
				if ( ( ++this._processed ) > limit )
				{
					this.SetSerializationError( new RpcErrorMessage( RpcError.MessageTooLargeError, "Incoming stream too large.", String.Format( CultureInfo.CurrentCulture, "MaxLength:{0:#,##0} bytes.", limit ) ) );
					yield break;
				}

				yield return b;
			}
		}
	}
}
