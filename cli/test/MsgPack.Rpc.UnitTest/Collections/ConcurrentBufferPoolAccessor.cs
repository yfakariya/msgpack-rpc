using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;

namespace MsgPack.Collections
{
	public sealed class ConcurrentBufferPoolAccessor
	{
		private static readonly FieldInfo _chunks =
			typeof( ConcurrentBufferPool ).GetField( "_chunks", BindingFlags.NonPublic | BindingFlags.Instance );

		private static readonly FieldInfo _freeList =
			typeof( ConcurrentBufferPool ).GetField( "_freeList", BindingFlags.NonPublic | BindingFlags.Instance );

		private readonly ConcurrentBufferPool _underlying;

		public ConcurrentBufferPoolAccessor( ConcurrentBufferPool underlying )
		{
			this._underlying = underlying;
		}

		public ConcurrentDictionary<int, byte[]> Chunks
		{
			get { return _chunks.GetValue( this._underlying ) as ConcurrentDictionary<int, byte[]>; }
		}

		public ConcurrentBag<ArraySegment<byte>> FreeList
		{
			get { return _freeList.GetValue( this._underlying ) as ConcurrentBag<ArraySegment<byte>>; }
		}
	}
}
