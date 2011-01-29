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

namespace MsgPack.Rpc.Serialization
{
	// FIXME: refactor

	/// <summary>
	///		Represents RPC input buffer.
	/// </summary>
	public sealed class RpcInputBuffer : IEnumerable<byte>, IDisposable
	{
		// This class is responsible for lifecycle of 'hot' segments of PooledBuffer.

		private readonly Func<ChunkBuffer, object, BufferFeeding> _feeding;
		private readonly object _feedingState;
		private ChunkBuffer _chunks;

		internal ChunkBuffer Chunks
		{
			get { return this._chunks; }
		}

		private int _readingPositionInSegment;
		private int _segmentIndex;
		private int _position;
		private long _length;

		internal RpcInputBuffer( ChunkBuffer buffer, Func<ChunkBuffer, object, BufferFeeding> feeding, object feedingState )
		{
			Contract.Assert( buffer != null );
			Contract.Assert( feeding != null );

			this._chunks = buffer;
			this._length = buffer.TotalLength;
			this._feeding = feeding;
			this._feedingState = feedingState;
			this._segmentIndex = 0;
			this._readingPositionInSegment = -1;
			this._position = -1;
		}

		/// <summary>
		///		Cleanup internal resources.
		/// </summary>
		public void Dispose()
		{
			this._chunks.Dispose();
		}

#warning TODO: impl
		public int Remaining { get { throw new NotImplementedException(); } }

		/// <summary>
		///		Get <see cref="Iterator"/> to enumerate content of this buffer.
		/// </summary>
		/// <returns>
		///		 <see cref="Iterator"/> to enumerate content of this buffer.
		/// </returns>
		public Iterator GetEnumerator()
		{
			return new Iterator( this );
		}

		IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		///		Enumerate bytes from <see cref="RpcInputBuffer"/>.
		/// </summary>
		/// <remarks>
		///		You must call <see cref="Dispose"/> the end of iteration.
		///		Note that C#'s foreach statement (and other languages' equivelants) invoke <see cref="Dispose"/> 
		///		in the end of loop automatically.
		/// </remarks>
		public struct Iterator : IEnumerator<byte>
		{
			private readonly RpcInputBuffer _enclosing;
			private bool _isDisposed;

			/// <summary>
			///		Get current byte.
			/// </summary>
			/// <value>
			///		Current byte.
			/// </value>
			/// <exception cref="ObjectDisposedException">
			///		This iterator is disposed.
			/// </exception>
			/// <exception cref="InvalidOperationException">
			///		This iterator is in before head.
			/// </exception>
			public byte Current
			{
				get
				{
					if ( this._isDisposed )
					{
						throw new ObjectDisposedException( typeof( Iterator ).FullName );
					}

					return this._enclosing.Chunks[ this._enclosing._segmentIndex ].Get( this._enclosing._readingPositionInSegment );
				}
			}

			object System.Collections.IEnumerator.Current
			{
				get { return this.Current; }
			}

			internal Iterator( RpcInputBuffer enclosing )
			{
				this._enclosing = enclosing;
				this._isDisposed = false;
			}

			/// <summary>
			///		Cleanup internal resources.
			/// </summary>
			public void Dispose()
			{
				this._isDisposed = true;
				//this._enclosing._buffer.Shrink( this._enclosing._segmentIndex, this._enclosing._readingPositionInSegment );
			}

			/// <summary>
			///		Try move this iterator to next position.
			/// </summary>
			/// <returns>
			///		If this iterator succeeded to move next then true.
			///		If this iterator exceeds range then false.
			/// </returns>
			/// <exception cref="ObjectDisposedException">
			///		This iterator is disposed.
			/// </exception>
			public bool MoveNext()
			{
				if ( this._isDisposed )
				{
					throw new ObjectDisposedException( typeof( Iterator ).FullName );
				}

				this._enclosing._readingPositionInSegment++;
				this._enclosing._position++;

				if ( this._enclosing.Chunks.Count == 0 || this._enclosing._position == this._enclosing._length )
				{
					if ( !this.RequestMoreData() )
					{
						return false;
					}
				}

				var segment = this._enclosing.Chunks[ this._enclosing._segmentIndex ];
				while ( segment.Count == this._enclosing._readingPositionInSegment )
				{
					if ( this._enclosing.Chunks.Count == this._enclosing._segmentIndex + 1 )
					{
						if ( !this.RequestMoreData() )
						{
							return false;
						}

						// Retry segment boundary checking.
						segment = this._enclosing.Chunks[ this._enclosing._segmentIndex ];
						continue;
					}
					else
					{
						// Shift to next segement.
						this._enclosing._segmentIndex++;
						this._enclosing._readingPositionInSegment = 0;
						break;
					}
				}

				return segment.Count >= this._enclosing._readingPositionInSegment + 1;
			}

			private bool RequestMoreData()
			{
				// Reach to initial buffer length, so try to get more.
				var feedingResult = this._enclosing._feeding( this._enclosing._chunks, this._enclosing._feedingState );
				if ( feedingResult.Feeded == 0 )
				{
					// Extra data does not exist.
					return false;
				}

				if ( feedingResult.ReallocatedBuffer != null )
				{
					// Reallocation implementation swapped buffer instance.
					this._enclosing._chunks = feedingResult.ReallocatedBuffer;
				}

				// Add extra data length to _length.
				this._enclosing._length += feedingResult.Feeded;
				return true;
			}

			void System.Collections.IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}
		}
	}

}
