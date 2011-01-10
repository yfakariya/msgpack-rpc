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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Globalization;

namespace MsgPack.Collections
{
	/// <summary>
	///		Simple implementation for <see cref="BufferPool"/>.
	/// </summary>
	/// <threadsafety static="true" instance="true" />
	public sealed class ConcurrentBufferPool : BufferPool
	{
		// TODO: min/max threashold
		// TODO: debugging feature

		// Should not be allocated to LOH.
		private const int _defaultChunkSize = 32 * 1024;
		private readonly int _chunkSize;

		private readonly ConcurrentDictionary<int, byte[]> _chunks = new ConcurrentDictionary<int, byte[]>();
		private readonly ConcurrentBag<ArraySegment<byte>> _freeList = new ConcurrentBag<ArraySegment<byte>>();

		public ConcurrentBufferPool() : this( _defaultChunkSize ) { }

		public ConcurrentBufferPool( int chunkSize )
		{
			this._chunkSize = chunkSize;
		}

#if DEBUG
		private enum OperationType
		{
			Alloc,
			Free,
			Marker
		}

		private struct DebugRecord
		{
			public readonly OperationType Operation;
			public readonly int ThreadId;
			public readonly ArraySegment<byte> Segment;
			public readonly string Marker;

			public DebugRecord( OperationType operation, ArraySegment<byte> segment )
			{
				this.Operation = operation;
				this.ThreadId = Thread.CurrentThread.ManagedThreadId;
				this.Segment = segment;
				this.Marker = null;
			}

			public DebugRecord( string marker )
			{
				this.Operation = OperationType.Marker;
				this.ThreadId = Thread.CurrentThread.ManagedThreadId;
				this.Segment = default( ArraySegment<byte> );
				this.Marker = marker;
			}
		}

		private readonly ConcurrentQueue<DebugRecord> _debugRecords = new ConcurrentQueue<DebugRecord>();

		public IEnumerable<string> DebugGetLogRecords()
		{
			foreach ( var record in this._debugRecords )
			{
				if ( record.Operation == OperationType.Marker )
				{
					yield return String.Format( CultureInfo.InvariantCulture, "{0:x4} :{1}", record.ThreadId, record.Marker );
				}
				else if ( record.Operation == OperationType.Alloc )
				{
					yield return String.Format( CultureInfo.InvariantCulture, "{0:x4} :ALLOC: {1:x8}+{2:x8}@{3:x8}", record.ThreadId, record.Segment.Offset, record.Segment.Count, record.Segment.Array.GetHashCode() );
				}
				else
				{
					yield return String.Format( CultureInfo.InvariantCulture, "{0:x4} :FREE : {1:x8}+{2:x8}@{3:x8}", record.ThreadId, record.Segment.Offset, record.Segment.Count, record.Segment.Array.GetHashCode() );
				}
			}
		}

		public void DebugClearLogRecords()
		{
			DebugRecord record;
			while ( this._debugRecords.TryDequeue( out record ) ) ;
		}
#endif

		[Conditional( "DEBUG" )]
		private void TraceAlloc( ArraySegment<byte> segment )
		{
#if DEBUG
			this._debugRecords.Enqueue( new DebugRecord( OperationType.Alloc, segment ) );
#endif
		}

		[Conditional( "DEBUG" )]
		private void TraceFree( ArraySegment<byte> segment )
		{
#if DEBUG
			this._debugRecords.Enqueue( new DebugRecord( OperationType.Free, segment ) );
#endif
		}

		[Conditional( "DEBUG" )]
		internal void TraceMarker( string marker )
		{
			this._debugRecords.Enqueue( new DebugRecord( marker ) );
		}

		protected sealed override BufferChunks BorrowCore( int length )
		{
			var result = new ReadOnlyChunkedPooledBuffer( this );
			bool success = false;
			try
			{
				this.AssignChunks( result, length );

				try
				{
					return result;
				}
				finally
				{
					success = true;
				}
			}
			finally
			{
				if ( !success )
				{
					result.Dispose();
				}
			}
		}

		private void AssignChunks( ReadOnlyChunkedPooledBuffer buffer, int length )
		{
#warning Race condition bug.
			int gotten = 0;
			while ( gotten < length )
			{
				ArraySegment<byte> segment;
				while ( !this._freeList.TryTake( out segment ) )
				{
					// There are no chunks, so chunk allocation is needed.
					var chunk = new byte[ _chunkSize ];
					for (
						int chunkIndex = this._chunks.Count;
						!this._chunks.TryAdd( chunkIndex, chunk );
						chunkIndex++
					) ;
					this._freeList.Add( new ArraySegment<byte>( chunk ) );
				}

				if ( segment.Count > ( length - gotten ) )
				{
					// taken segment is too large, so return back to pool remaining region.
					try { }
					finally
					{
						// take only needed size with new ArraySegment( array, offset, needed_size )
						// return remaining as new ArraySegment( array, needed_size, original_size - needed_size
						ArraySegment<byte> returningSegment;
						ArraySegment<byte> freeSegment;
						segment.Devide( length - gotten, out returningSegment, out freeSegment );
						TraceAlloc( returningSegment );
						buffer.Chunks.Add( returningSegment );
						this._freeList.Add( freeSegment );
						gotten += ( length - gotten );
					}
				}
				else
				{
					TraceAlloc( segment );
					buffer.Chunks.Add( segment );
					gotten += segment.Count;
				}
			}
		}

		protected sealed override void ReturnCore( BufferChunks buffer )
		{
			var recoginizedBuffer = buffer as ReadOnlyChunkedPooledBuffer;
			if ( recoginizedBuffer == null || recoginizedBuffer.Source != this )
			{
				throw new ArgumentException( "Unrecoginized buffer.", "buffer" );
			}

			this.ReturnSegments( recoginizedBuffer.Chunks );
		}

		private void ReturnSegments( IEnumerable<ArraySegment<byte>> segments )
		{
			try { }
			finally
			{
				foreach ( var segmentGroup in segments.GroupBy( item => item.Array ) )
				{
					var chunk = segmentGroup.Key;
					var orderedSegments = segmentGroup.OrderBy( item => item.Offset );
					ArraySegment<byte> concatinated = orderedSegments.First();
					foreach ( var segment in orderedSegments.Skip( 1 ) )
					{
						int lastTailIndex = concatinated.Offset + concatinated.Count;
						if ( segment.Offset == lastTailIndex )
						{
							Contract.Assume( segment.Array == concatinated.Array );
							concatinated = new ArraySegment<byte>( concatinated.Array, concatinated.Offset, concatinated.Count + segment.Count );
						}
						else
						{
							Contract.Assume( segment.Offset > lastTailIndex, segment.Offset + ">" + lastTailIndex );
							TraceFree( concatinated );
							this._freeList.Add( concatinated );
							concatinated = segment;
						}
					}

					TraceFree( concatinated );
					this._freeList.Add( concatinated );
				}
			}
		}

		internal sealed class ReadOnlyChunkedPooledBuffer : BufferChunks
		{
			private int _disposed;
			private readonly ConcurrentBufferPool _source;

			internal BufferPool Source
			{
				get { return this._source; }
			}

			private readonly List<ArraySegment<byte>> _chunks;

			internal List<ArraySegment<byte>> Chunks
			{
				get { return this._chunks; }
			}

			public sealed override int Count
			{
				get { return this._chunks.Count; }
			}

			public ReadOnlyChunkedPooledBuffer( ConcurrentBufferPool source )
			{
				this._source = source;
				this._chunks = new List<ArraySegment<byte>>( 1 );
			}

			~ReadOnlyChunkedPooledBuffer()
			{
				this.Dispose( false );
			}

			protected sealed override void Dispose( bool disposing )
			{
				try { }
				finally
				{
					if ( Interlocked.CompareExchange( ref this._disposed, 1, 0 ) == 0 )
					{
						this._source.Return( this );
					}
				}
			}

			protected sealed override ArraySegment<byte> GetAt( int index )
			{
				return this._chunks[ index ];
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

			protected sealed override BufferChunks ReallocateCore( int requiredAdditionalLength )
			{
				this._source.AssignChunks( this, requiredAdditionalLength );
				return this;
			}

			protected sealed override BufferChunks ShrinkCore( int remainingSegmentIndex, int remainingStartIndexInSegment )
			{
				var oldLastSegment = this._chunks[ remainingSegmentIndex ];
				ArraySegment<byte> lastGarbageSegment;
				ArraySegment<byte> newLastSegment;
				oldLastSegment.Devide( remainingStartIndexInSegment, out lastGarbageSegment, out newLastSegment );

				this._source.ReturnSegments( this.GetGarbageSegments( remainingSegmentIndex, lastGarbageSegment ) );
				if ( remainingSegmentIndex > 0 )
				{
					this._chunks.RemoveRange( 0, remainingSegmentIndex - 1 );
				}

				this._chunks[ 0 ] = newLastSegment;
				return this;
			}

			private IEnumerable<ArraySegment<byte>> GetGarbageSegments( int remainingSegmentIndex, ArraySegment<byte> lastGarbageSegment )
			{
				Contract.Assume( this._chunks[ remainingSegmentIndex ].Array == lastGarbageSegment.Array );
				Contract.Assume( this._chunks[ remainingSegmentIndex ].Offset == lastGarbageSegment.Offset );

				for ( int i = 0; i < remainingSegmentIndex; i++ )
				{
					yield return this._chunks[ i ];
				}

				yield return lastGarbageSegment;
			}
		}
	}
}
