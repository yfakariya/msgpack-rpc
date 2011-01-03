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

namespace MsgPack.Collections
{
	[TestFixture]
	public sealed class AdHocPseudoBufferPoolTest
	{
		[Test]
		public void TestBorrow()
		{
			Random random = new Random();
			var target = new AdHocPseudoBufferPool( 4 );
			for ( int i = 0; i < 10000; i++ )
			{
				int length = random.Next( 1, 256 );
				using ( var result = target.Borrow( length ) )
				{
					Assert.NotNull( result );
					Assert.AreEqual( length, result.Sum( item => item.Count ) );
					result.Fill( Enumerable.Range( 1, length ).Select( item => ( byte )( item % 256 ) ) );
					CollectionAssert.AreEqual( Enumerable.Range( 1, length ).Select( item => ( byte )( item % 256 ) ), result.ReadAll() );
					target.Return( result );
				}
			}
		}

		[Test]
		public void TestReallocate()
		{
			Random random = new Random();
			var target = new AdHocPseudoBufferPool( 4 );
			for ( int i = 0; i < 10000; i++ )
			{
				int length = random.Next( 1, 256 );
				using ( var result = target.Borrow( length ) )
				{
					Assert.NotNull( result );
					Assert.AreEqual( length, result.Sum( item => item.Count ) );
					int newLength = random.Next( 1, 256 );
					using ( var reallocated = result.Reallocate( newLength ) )
					{
						Assert.AreSame( result, reallocated );
						Assert.GreaterOrEqual( length + newLength, reallocated.Sum( item => item.Count ) );
						result.Fill( Enumerable.Range( 1, length + newLength ).Select( item => ( byte )( item % 256 ) ) );
						CollectionAssertEx.StartsWith( Enumerable.Range( 1, length ).Select( item => ( byte )( item % 256 ) ), reallocated.ReadAll() );
					}
				}
			}
		}

		[Test]
		public void TestSubChanks()
		{
			Random random = new Random();
			var target = new AdHocPseudoBufferPool( 4 );

			using ( var result = target.Borrow( 8 ) )
			{
				Assert.NotNull( result );
				Assert.AreEqual( 8, result.Sum( item => item.Count ) );
				result.Fill( Enumerable.Range( 1, 8 ).Select( item => ( byte )( item % 256 ) ) );

				using ( var subChunk = result.SubChunks( 1, 2 ) )
				{
					Assert.AreNotSame( result, subChunk );
					Assert.AreEqual( 2, subChunk.Sum( item => item.Count ) );
					CollectionAssertEx.StartsWith( Enumerable.Range( 1, 8 ).Select( item => ( byte )( item % 256 ) ).Skip( 1 ).Take( 2 ), subChunk.ReadAll() );
				}

				using ( var subChunk = result.SubChunks( 1, 4 ) )
				{
					Assert.AreNotSame( result, subChunk );
					Assert.AreEqual( 4, subChunk.Sum( item => item.Count ) );
					CollectionAssertEx.StartsWith( Enumerable.Range( 1, 8 ).Select( item => ( byte )( item % 256 ) ).Skip( 1 ).Take( 4 ), subChunk.ReadAll() );
				}

				using ( var subChunk = result.SubChunks( 2, 4 ) )
				{
					Assert.AreNotSame( result, subChunk );
					Assert.AreEqual( 4, subChunk.Sum( item => item.Count ) );
					CollectionAssertEx.StartsWith( Enumerable.Range( 1, 8 ).Select( item => ( byte )( item % 256 ) ).Skip( 2 ).Take( 4 ), subChunk.ReadAll() );
				}

				using ( var subChunk = result.SubChunks( 3, 4 ) )
				{
					Assert.AreNotSame( result, subChunk );
					Assert.AreEqual( 4, subChunk.Sum( item => item.Count ) );
					CollectionAssertEx.StartsWith( Enumerable.Range( 1, 8 ).Select( item => ( byte )( item % 256 ) ).Skip( 3 ).Take( 4 ), subChunk.ReadAll() );
				}
			}

			for ( int i = 0; i < 10000; i++ )
			{
				int length = random.Next( 1, 256 );
				using ( var result = target.Borrow( length ) )
				{
					Assert.NotNull( result );
					Assert.AreEqual( length, result.Sum( item => item.Count ) );
					result.Fill( Enumerable.Range( 1, length ).Select( item => ( byte )( item % 256 ) ) );
					int offset = random.Next( length - 1 );
					int count = random.Next( length - offset );
					try
					{
						using ( var subChunk = result.SubChunks( offset, count ) )
						{
							Assert.AreNotSame( result, subChunk );
							Assert.AreEqual( count, subChunk.Sum( item => item.Count ) );
							CollectionAssertEx.StartsWith( Enumerable.Range( 1, length ).Select( item => ( byte )( item % 256 ) ).Skip( offset ).Take( count ), subChunk.ReadAll() );
						}
					}
					catch
					{
						Console.Error.WriteLine( "{0} -> ({1},{2})", length, offset, count );
						throw;
					}
				}
			}
		}
	}
}
