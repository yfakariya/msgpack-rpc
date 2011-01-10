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
using System.Diagnostics.Contracts;

namespace MsgPack.Collections
{
	/// <summary>
	///		Ad-hoc based naive implementation of buffer pool.
	/// </summary>
	/// <remarks>
	///		This implementation always allocates new byte array and returns its wrapper.
	///		A performance of this class relys on underlying GC.
	/// </remarks>
	public sealed class AdHocPseudoBufferPool : BufferPool
	{
		private const int _defaultChunkSize = 32 * 1024;
		private readonly int _chunkSize;

		public AdHocPseudoBufferPool() : this( _defaultChunkSize ) { }

		public AdHocPseudoBufferPool( int chunkSize )
		{
			this._chunkSize = chunkSize;
		}

		protected sealed override ChunkBuffer BorrowCore( int length )
		{
			return new SimpleMultiArrayBuffer( length, _chunkSize );
		}

		protected sealed override void ReturnCore( ChunkBuffer buffer )
		{
			// nop
		}

		/// <summary>
		///		Simple wrapper for single continuous byte array.
		/// </summary>
		private sealed class SimpleMultiArrayBuffer : ChunkBuffer
		{
			private readonly List<ArraySegment<byte>> _chunks;
			private readonly int _chunkSize;

			public sealed override int Count
			{
				get { return this._chunks.Count; }
			}

			private long _totalLength;

			public sealed override long TotalLength
			{
				get { return this._totalLength; }
			}

			public SimpleMultiArrayBuffer( int length, int chunkSize )
			{
				this._chunks = new List<ArraySegment<byte>>();
				this._chunkSize = chunkSize;
				this.AllocateSegments( length );
			}

			private SimpleMultiArrayBuffer( List<ArraySegment<byte>> chunks, int chunkSize )
			{
				this._chunks = chunks;
				this._chunkSize = chunkSize;
			}

			private void AllocateSegments( long length )
			{
				for ( long i = 0; i < length / _chunkSize; i++ )
				{
					this._chunks.Add( new ArraySegment<byte>( new byte[ this._chunkSize ] ) );
					this._totalLength += this._chunkSize;
				}

				if ( ( length % _chunkSize ) > 0 )
				{
					this._chunks.Add( new ArraySegment<byte>( new byte[ length % this._chunkSize ] ) );
					this._totalLength += length % this._chunkSize;
				}
			}

			protected sealed override ArraySegment<byte> GetAt( int index )
			{
				if ( index < 0 || index >= this._chunks.Count )
				{
					throw new ArgumentOutOfRangeException( "index" );
				}

				return this._chunks[ index ];
			}

			public sealed override void Feed( ArraySegment<byte> newSegment )
			{
				this._chunks.Add( newSegment );
			}

			protected sealed override void SwapCore( IList<ArraySegment<byte>> newContents )
			{
				this._chunks.Clear();
				this._chunks.AddRange( newContents );
			}

			public sealed override bool Contains( ArraySegment<byte> item )
			{
				return this._chunks.Contains( item );
			}

			public sealed override int IndexOf( ArraySegment<byte> item )
			{
				return this._chunks.IndexOf( item );
			}

			public sealed override IEnumerator<ArraySegment<byte>> GetEnumerator()
			{
				return this._chunks.GetEnumerator();
			}

			protected sealed override void CopyToCore( ArraySegment<byte>[] array, int arrayIndex )
			{
				this._chunks.CopyTo( array, arrayIndex );
			}

			protected sealed override ChunkBuffer ReallocateCore( long requiredAdditionalLength )
			{
				this.AllocateSegments( requiredAdditionalLength );
				return this;
			}

			protected sealed override ChunkBuffer SubChunksCore( long newOffset, long newTotalLength )
			{
				int startSegmentIndex = -1;
				int startOffsetInSegment = -1;
				int endSegmentIndex = -1;
				int endCountInSegment = -1;

				this.FindNewSegment( newOffset, newTotalLength, ref startSegmentIndex, ref startOffsetInSegment, ref endSegmentIndex, ref endCountInSegment );
				Contract.Assert( startSegmentIndex >= 0 );
				Contract.Assert( startOffsetInSegment >= 0 );
				Contract.Assert( endSegmentIndex >= 0 );
				Contract.Assert( endCountInSegment >= 0 );

				var newChunks = new List<ArraySegment<byte>>( this._chunks.Skip( startSegmentIndex ).Take( endSegmentIndex - startSegmentIndex + 1 ) );
				var headSegment = newChunks[ 0 ];
				newChunks[ 0 ] = new ArraySegment<byte>( headSegment.Array, startOffsetInSegment, headSegment.Count - startOffsetInSegment );
				var tailSegment = newChunks[ newChunks.Count - 1 ];

				if ( newChunks.Count == 1 )
				{
					Contract.Assume( headSegment.Array == tailSegment.Array );
					newChunks[ newChunks.Count - 1 ] = new ArraySegment<byte>( tailSegment.Array, tailSegment.Offset, endCountInSegment - startOffsetInSegment );
				}
				else
				{
					Contract.Assume( headSegment.Array != tailSegment.Array );
					Contract.Assume( tailSegment.Count >= endCountInSegment, tailSegment.Count + ">=" + endCountInSegment );
					newChunks[ newChunks.Count - 1 ] = new ArraySegment<byte>( tailSegment.Array, tailSegment.Offset, endCountInSegment );
				}

				return new SimpleMultiArrayBuffer( newChunks, this._chunkSize );
			}

			private void FindNewSegment( long offset, long count, ref int startSegmentIndex, ref int startOffsetInSegment, ref int endSegmentIndex, ref int endCountInSegment )
			{
				long position = 0;
				for ( int segmentIndex = 0; segmentIndex < this._chunks.Count; segmentIndex++ )
				{
					var segment = this._chunks[ segmentIndex ];
					for ( int offsetInSegment = 0; offsetInSegment < segment.Count; offsetInSegment++ )
					{
						if ( position == offset )
						{
							Contract.Assert( startSegmentIndex == -1 );
							Contract.Assert( startOffsetInSegment == -1 );
							startSegmentIndex = segmentIndex;
							startOffsetInSegment = offsetInSegment;
						}

						if ( position == offset + count )
						{
							Contract.Assert( endSegmentIndex == -1 );
							Contract.Assert( endCountInSegment == -1 );
							endSegmentIndex = segmentIndex;
							endCountInSegment = offsetInSegment;
							return;
						}

						position++;
					}
				}
			}
		}
	}
}
