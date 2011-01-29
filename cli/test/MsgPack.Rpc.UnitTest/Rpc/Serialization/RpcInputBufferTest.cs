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
using NUnit.Framework;
using MsgPack.Collections;

namespace MsgPack.Rpc.Serialization
{
	[TestFixture]
	[Timeout( 3000 )]
	public sealed class RpcInputBufferTest
	{
		[Test]
		public void TestEnumerateSingleChunk()
		{
			using ( var buffer = ChunkBuffer.CreateDefault() )
			{
				var expected = Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray();
				buffer.Feed( expected.ToArray() );
				using ( var target = new RpcInputBuffer( buffer, ( _0, _1 ) => default( BufferFeeding ), null ) )
				{
					CollectionAssert.AreEqual( expected, target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateSingleChunkWithFeeding()
		{
			bool feeded = false;
			using ( var buffer = ChunkBuffer.CreateDefault() )
			{
				buffer.Feed( Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray() );
				using ( var target =
					new RpcInputBuffer(
						buffer,
						( _0, _1 ) =>
						{
							if ( feeded )
							{
								return default( BufferFeeding );
							}

							feeded = true;
							buffer.Feed( Enumerable.Range( 3, 4 ).Select( item => ( byte )item ).ToArray() );
							return new BufferFeeding( 4 );
						},
						null
				) )
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 6 ).ToArray(), target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunk()
		{
			using ( var buffer = ChunkBuffer.CreateDefault() )
			{
				var expected = Enumerable.Range( 1, 6 ).Select( item => ( byte )item ).ToArray();
				buffer.Feed( expected.ToArray() );
				using ( var target = new RpcInputBuffer( buffer, ( _0, _1 ) => default( BufferFeeding ), null ) )
				{
					CollectionAssert.AreEqual( expected, target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunkWithFeeding()
		{
			bool feeded = false;
			using ( var buffer = ChunkBuffer.CreateDefault() )
			{
				buffer.Feed( new ArraySegment<byte>( Enumerable.Range( 1, 6 ).Select( item => ( byte )item ).ToArray() ) );
				using ( var target =
					new RpcInputBuffer(
						buffer,
						( _0, _1 ) =>
						{
							if ( feeded )
							{
								return default( BufferFeeding );
							}

							feeded = true;
							buffer.Feed( Enumerable.Range( 7, 2 ).Select( item => ( byte )item ).ToArray() );
							return new BufferFeeding( 4 );
						},
						null
					)
				)
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 8 ).ToArray(), target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunkWithFeedingFailed()
		{
			using ( var buffer = ChunkBuffer.CreateDefault() )
			{
				var expected = Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray();
				buffer.Feed( expected.ToArray() );
				using ( var target = new RpcInputBuffer( buffer, ( _0, _1 ) => new BufferFeeding( 0 ), null ) )
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 2 ).ToArray(), target.ToArray() );
				}
			}
		}
	}
}
