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
using System.Globalization;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Represents buffer feeding result.
	/// </summary>
	/// <param name="buffer">Buffer to be feeded.</param>
	/// <returns>Count of feeded bytes.</returns>
	public struct BufferFeeding : IEquatable<BufferFeeding>
	{
		private readonly ChunkBuffer _reallocatedBuffer;

		public ChunkBuffer ReallocatedBuffer
		{
			get { return this._reallocatedBuffer; }
		}

		private readonly int _feeded;

		public int Feeded
		{
			get { return this._feeded; }
		}

		public BufferFeeding( int feeded )
			: this( feeded, null ) { }

		public BufferFeeding (int feeded, ChunkBuffer reallocatedBuffer)
		{
			this._feeded = feeded;
			this._reallocatedBuffer = reallocatedBuffer;
		}

		public bool Equals( BufferFeeding other )
		{
			return this._feeded == other._feeded && this._reallocatedBuffer == other._reallocatedBuffer;
		}

		public override bool Equals( object obj )
		{
			if ( !( obj is BufferFeeding ) )
			{
				return false;
			}
			else
			{
				return this.Equals( ( BufferFeeding )obj );
			}
		}

		public override int GetHashCode()
		{
			return this._feeded ^ ( this._reallocatedBuffer == null ? 0 : this._reallocatedBuffer.GetHashCode() );
		}

		public override string ToString()
		{
			if ( this._reallocatedBuffer == null )
			{
				return this._feeded.ToString();
			}
			else
			{
				return this._feeded.ToString() + "ReallocatedTo:" + this._reallocatedBuffer.ToString() + "(" + this._reallocatedBuffer.GetHashCode() + ")";
			}
		}

		public static bool operator ==(BufferFeeding left,  BufferFeeding right)
		{
			return left.Equals( right );
		}

		public static bool operator !=( BufferFeeding left, BufferFeeding right )
		{
			return !left.Equals( right );
		}
	}
}
