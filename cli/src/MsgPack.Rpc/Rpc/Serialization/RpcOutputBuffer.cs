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
using System.IO;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Serialization
{
	/// <summary>
	///		Represents RPC output buffer. This class is NOT thread safe.
	/// </summary>
	/// <remarks>
	///		It is not thread safe that:
	///		<list type="bullet">
	///			<item>Disposing this instance concurrently.</item>
	///			<item>Use multiple stream returned from <see cref="OpenStream"/> concurrently.</item>
	///		</list>
	/// </remarks>
	public sealed class RpcOutputBuffer : IDisposable
	{
		// Responsible for allocation
		// BufferPool usage is thread safe, but returning and writing is not thread safe.

		private readonly List<ArraySegment<byte>> _chunks;
		private readonly BufferPool _poolForCopy;
		private ChunkBuffer _bufferToBeDisposed;

		internal List<ArraySegment<byte>> Chunks
		{
			get { return this._chunks; }
		}

		internal RpcOutputBuffer( BufferPool poolForCopy )
		{
			Contract.Assert( poolForCopy != null );

			this._poolForCopy = poolForCopy;

			// type(1)+id(1)+method(1)+arg(avg.3)
			this._chunks = new List<ArraySegment<byte>>( 6 );
		}

		public void Dispose()
		{
			if ( this._bufferToBeDisposed != null )
			{
				this._bufferToBeDisposed.Dispose();
				this._bufferToBeDisposed = null;
			}
		}

		public Stream OpenStream( bool isZeroCopy )
		{
			if ( isZeroCopy )
			{
				return new ZeroCopyingRpcOutputBufferStream( this._chunks, this._poolForCopy, buffer => this._bufferToBeDisposed = buffer );
			}
			else
			{
				return new CopyingRpcOutputBufferStream( this._chunks, this._poolForCopy, buffer => this._bufferToBeDisposed = buffer );
			}
		}

		internal RpcOutputBufferSwapper CreateSwapper()
		{
			return new RpcOutputBufferSwapper( this );
		}

		private abstract class RpcOutputBufferStream : Stream
		{
			public sealed override bool CanRead
			{
				get { return false; }
			}

			public sealed override bool CanSeek
			{
				get { return false; }
			}

			public sealed override bool CanWrite
			{
				get { return true; }
			}

			public sealed override long Length
			{
				get { throw new NotSupportedException(); }
			}

			public sealed override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			private readonly List<ArraySegment<byte>> _chunks;

			protected List<ArraySegment<byte>> Chunks
			{
				get { return _chunks; }
			}

			private readonly BufferPool _poolForCopy;

			protected BufferPool PoolForCopy
			{
				get { return this._poolForCopy; }
			}

			private readonly Action<ChunkBuffer> _bufferRegistration;

			private bool _isDisposed;

			protected RpcOutputBufferStream( List<ArraySegment<byte>> chunks, BufferPool poolForCopy, Action<ChunkBuffer> bufferRegistration )
			{
				Contract.Assert( chunks != null );
				Contract.Assert( poolForCopy != null );
				Contract.Assert( bufferRegistration != null );

				this._chunks = chunks;
				this._poolForCopy = poolForCopy;
				this._bufferRegistration = bufferRegistration;
			}

			protected override void Dispose( bool disposing )
			{
				base.Dispose( disposing );
				this._isDisposed = true;
			}

			protected void RegisterBuffer( ChunkBuffer pooledBuffer )
			{
				Contract.Assert( pooledBuffer != null );
				this._bufferRegistration( pooledBuffer );
			}

			public sealed override void Write( byte[] buffer, int offset, int count )
			{
				if ( buffer == null )
				{
					throw new ArgumentNullException( "buffer" );
				}

				if ( offset < 0 || offset >= buffer.Length )
				{
					throw new ArgumentOutOfRangeException( "offset" );
				}

				if ( count < 0 )
				{
					throw new ArgumentOutOfRangeException( "count" );
				}

				if ( buffer.Length < offset + count )
				{
					throw new ArgumentException( "'buffer' too small.", "buffer" );
				}

				if ( this._isDisposed )
				{
					throw new ObjectDisposedException( this.GetType().FullName );
				}

				Contract.EndContractBlock();

				this.WriteCore( buffer, offset, count );
			}

			protected abstract void WriteCore( byte[] buffer, int offset, int count );

			public sealed override void Flush()
			{
				// nop
			}

			public sealed override int Read( byte[] buffer, int offset, int count )
			{
				throw new NotSupportedException();
			}

			public sealed override long Seek( long offset, SeekOrigin origin )
			{
				throw new NotSupportedException();
			}

			public sealed override void SetLength( long value )
			{
				throw new NotSupportedException();
			}
		}

		private sealed class CopyingRpcOutputBufferStream : RpcOutputBufferStream
		{
			private ChunkBuffer _buffer;
			private int _lastSegmentIndex = 0;
			private int _positionInSegment = 0;

			public CopyingRpcOutputBufferStream( List<ArraySegment<byte>> chunks, BufferPool poolForCopy, Action<ChunkBuffer> bufferRegistration )
				: base( chunks, poolForCopy, bufferRegistration ) { }

			protected sealed override void Dispose( bool disposing )
			{
				base.RegisterBuffer( this._buffer );
				for ( int i = 0; i < this._lastSegmentIndex; i++ )
				{
					this.Chunks.Add( this._buffer[ i ] );
				}

				var lastSegment = this._buffer[ this._lastSegmentIndex ];
				//this.Chunks.Add( new ArraySegment<byte>( lastSegment.Array, lastSegment.Offset, this._positionInSegment - lastSegment.Offset ) );
				// bytes after position is preserved since it might be required following filters.
				this.Chunks.Add( lastSegment.SubSegment( this._positionInSegment ) );

				base.Dispose( disposing );
			}

			protected sealed override void WriteCore( byte[] buffer, int offset, int count )
			{
				int copied = 0;
				while ( copied < count )
				{
					if ( this._buffer == null )
					{
						this._buffer = this.PoolForCopy.Borrow( count );
						//this._positionInSegment = this._buffer[ 0 ].Offset;
						this._positionInSegment = 0;
					}
					else
					{
						var lastSegment = this._buffer[ this._lastSegmentIndex ];
						//if ( this._positionInSegment == lastSegment.Offset + lastSegment.Count )
						if ( this._positionInSegment == lastSegment.Count )
						{
							this._buffer = this._buffer.Reallocate( count - copied );
						}

						// If buffer was expanded, index should not be increment.
						lastSegment = this._buffer[ this._lastSegmentIndex ];
						//if ( this._positionInSegment == lastSegment.Offset + lastSegment.Count )
						if ( this._positionInSegment == lastSegment.Count )
						{
							// New segment is allocated instead of expansion.
							this._lastSegmentIndex++;
							this._positionInSegment = 0;
						}
					}

					var targetSegment = this._buffer[ this._lastSegmentIndex ];
					int remain = this._positionInSegment - targetSegment.Offset;
					int copying = remain <= count ? remain : count;
					targetSegment.CopyFrom( this._positionInSegment, buffer, offset, copying );
					this._positionInSegment += copying;
					copied += copying;
				}
			}
		}

		private sealed class ZeroCopyingRpcOutputBufferStream : RpcOutputBufferStream
		{
			public ZeroCopyingRpcOutputBufferStream( List<ArraySegment<byte>> chunks, BufferPool poolForCopy, Action<ChunkBuffer> bufferRegistration )
				: base( chunks, poolForCopy, bufferRegistration ) { }

			protected override void WriteCore( byte[] buffer, int offset, int count )
			{
				this.Chunks.Add( new ArraySegment<byte>( buffer, offset, count ) );
			}
		}

		internal sealed class RpcOutputBufferSwapper : IDisposable
		{
			private readonly List<ArraySegment<byte>> _newChunks;
			private readonly RpcOutputBuffer _enclosing;
			private ChunkBuffer _swappedBuffer;

			public RpcOutputBufferSwapper( RpcOutputBuffer enclosing )
			{
				Contract.Assert( enclosing != null );
				this._enclosing = enclosing;
				this._newChunks = new List<ArraySegment<byte>>( 1 );
			}

			public void Dispose()
			{
				if ( this._newChunks.Count > 0 )
				{
					this._enclosing._chunks.Clear();
					this._enclosing._chunks.AddRange( this._newChunks );
				}

				if ( this._swappedBuffer != null )
				{
					if ( this._enclosing._bufferToBeDisposed != this._swappedBuffer
						&& this._enclosing._bufferToBeDisposed != null )
					{
						// Return old buffer.
						this._enclosing._bufferToBeDisposed.Dispose();
					}

					// Register new buffer as disposal of enclosing out buffer.
					this._enclosing._bufferToBeDisposed = this._swappedBuffer;
				}
			}

			public IEnumerable<byte> ReadBytes()
			{
				foreach ( var segment in this._enclosing._chunks )
				{
					for ( int i = 0; i < segment.Count; i++ )
					{
						yield return segment.Get( i );
					}
				}
			}

			public void WriteBytes( IEnumerable<byte> sequence )
			{
				if ( sequence == null )
				{
					throw new ArgumentNullException( "sequence" );
				}

				Contract.EndContractBlock();

				int segmentSize = this._enclosing._chunks.Sum( item => item.Count );
				this._swappedBuffer = this._enclosing._poolForCopy.Borrow( segmentSize );

				int processed = 0;
				int segmentIndex = 0;
				int positionInSegment = 0;
				var segment = this._swappedBuffer[ segmentIndex ];
				foreach ( var b in sequence )
				{
					if ( positionInSegment == segment.Count )
					{
						if ( segmentIndex == this._swappedBuffer.Count - 1 )
						{
							this._swappedBuffer = this._swappedBuffer.Reallocate( segmentSize - processed );
						}

						if ( segment.Count == positionInSegment )
						{
							segmentIndex++;
							Contract.Assert( this._swappedBuffer.Count > segmentIndex );
							this._newChunks.Add( segment );
							segment = this._swappedBuffer[ segmentIndex ];
							positionInSegment = 0;
						}

					}

					segment.Set( positionInSegment, b );
					processed++;
					positionInSegment++;
				}

				this._newChunks.Add( segment );
			}
		}
	}
}
