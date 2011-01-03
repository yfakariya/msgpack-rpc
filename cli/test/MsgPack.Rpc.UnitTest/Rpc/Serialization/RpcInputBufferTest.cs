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
	[Timeout(3000)]
	public sealed class RpcInputBufferTest
	{
		[Test]
		public void TestEnumerateSingleChunk()
		{
			var pool = new AdHocPseudoBufferPool( 2 );
			using ( var buffer = pool.Borrow( 2 ) )
			{
				var expected = Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray();
				buffer.Fill( expected );
				using ( var target = new RpcInputBuffer( buffer, 2, _ => default( BufferFeeding ) ) )
				{
					CollectionAssert.AreEqual( expected, target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateSingleChunkWithFeeding()
		{
			var pool = new AdHocPseudoBufferPool( 4 );
			bool feeded = false;
			using ( var buffer = pool.Borrow( 4 ) )
			{
				buffer.Fill( Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray() );
				using ( var target =
					new RpcInputBuffer(
						buffer,
						2,
						oldBufer =>
						{
							if ( feeded )
							{
								return default( BufferFeeding );
							}

							feeded = true;
							var reallocated = oldBufer.Reallocate( 2 );
							Assert.AreSame( oldBufer, reallocated );
							buffer.Fill( Enumerable.Range( 3, 4 ).Select( item => ( byte )item ).ToArray(), 2 );
							return new BufferFeeding( 4 );
						}
				) )
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 6 ).ToArray(), target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunk()
		{
			var pool = new AdHocPseudoBufferPool( 2 );
			using ( var buffer = pool.Borrow( 6 ) )
			{
				var expected = Enumerable.Range( 1, 6 ).Select( item => ( byte )item ).ToArray();
				buffer.Fill( expected );
				using ( var target = new RpcInputBuffer( buffer, 6, _ => default( BufferFeeding ) ) )
				{
					CollectionAssert.AreEqual( expected, target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunkWithFeeding()
		{
			var pool = new AdHocPseudoBufferPool( 2 );
			bool feeded = false;
			using ( var buffer = pool.Borrow( 6 ) )
			{
				buffer.Fill( Enumerable.Range( 1, 6 ).Select( item => ( byte )item ).ToArray() );
				using ( var target =
					new RpcInputBuffer(
						buffer,
						4,
						oldBufer =>
						{
							if ( feeded )
							{
								return default( BufferFeeding );
							}

							feeded = true;
							var reallocated = oldBufer.Reallocate( 2 );
							Assert.AreSame( oldBufer, reallocated );
							buffer.Fill( Enumerable.Range( 7, 2 ).Select( item => ( byte )item ).ToArray(), 6 );
							return new BufferFeeding( 4 );
						}
				) )
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 8 ).ToArray(), target.ToArray() );
				}
			}
		}

		[Test]
		public void TestEnumerateMultipleChunkWithFeedingFailed()
		{
			var pool = new AdHocPseudoBufferPool( 2 );
			using ( var buffer = pool.Borrow( 2 ) )
			{
				var expected = Enumerable.Range( 1, 2 ).Select( item => ( byte )item ).ToArray();
				buffer.Fill( expected );
				using ( var target = new RpcInputBuffer( buffer, 3, _ => new BufferFeeding( 0 ) ) )
				{
					CollectionAssert.AreEqual( Enumerable.Range( 1, 2 ).ToArray(), target.ToArray() );
				}
			}
		}
	}
}
