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
	public sealed class ArraySegmentsExtensionsTest
	{
		[Test]
		public void TestReadAll_Normal()
		{
			var source =
				new List<ArraySegment<int>>()
				{
					new ArraySegment<int>( new int[]{ -1, 0, 1, 2, -1 }, 1, 3 ),
					new ArraySegment<int>( new int[]{ -1, 3, 4, 5, 6, 7, -1 }, 1, 5 ),
					new ArraySegment<int>( new int[]{ -1 }, 1, 0 ),
					new ArraySegment<int>( new int[ 0 ], 0, 0 ),
					new ArraySegment<int>( new int[]{ -1, 8, 9 , -1 }, 1, 2 )
				};

			CollectionAssert.AreEqual(
				Enumerable.Range( 0, 10 ),
				ArraySegmentsExtensions.ReadAll( source )
			);
		}

		[Test]
		public void TestReadAll_Empty()
		{
			CollectionAssert.IsEmpty( ArraySegmentsExtensions.ReadAll( new ArraySegment<int>[ 0 ] ) );
		}

		[Test]
		public void TestFill_Normal()
		{
			var source =
				new ArraySegment<int>[]
				{
					new ArraySegment<int>( new int[ 5 ], 1, 3 ),
					new ArraySegment<int>( new int[ 7 ], 1, 5 ),
					new ArraySegment<int>( new int[ 1 ], 1, 0 ),
					new ArraySegment<int>( new int[ 0 ], 0, 0 ),
					new ArraySegment<int>( new int[ 4 ], 1, 2 )
				};

			var expected = RandomSequnce( 1, Int32.MaxValue, 10 ).ToArray();

			Assert.AreEqual( 10L, ArraySegmentsExtensions.Fill( source, expected ) );
			Assert.AreEqual( 0, source[ 0 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 0 ], source[ 0 ].Array[ 1 ] );
			Assert.AreEqual( expected[ 1 ], source[ 0 ].Array[ 2 ] );
			Assert.AreEqual( expected[ 2 ], source[ 0 ].Array[ 3 ] );
			Assert.AreEqual( 0, source[ 0 ].Array[ 4 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 3 ], source[ 1 ].Array[ 1 ] );
			Assert.AreEqual( expected[ 4 ], source[ 1 ].Array[ 2 ] );
			Assert.AreEqual( expected[ 5 ], source[ 1 ].Array[ 3 ] );
			Assert.AreEqual( expected[ 6 ], source[ 1 ].Array[ 4 ] );
			Assert.AreEqual( expected[ 7 ], source[ 1 ].Array[ 5 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 6 ] );
			Assert.AreEqual( 0, source[ 2 ].Array[ 0 ] );
			Assert.AreEqual( 0, source[ 4 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 8 ], source[ 4 ].Array[ 1 ] );
			Assert.AreEqual( expected[ 9 ], source[ 4 ].Array[ 2 ] );
			Assert.AreEqual( 0, source[ 4 ].Array[ 3 ] );
		}

		[Test]
		public void TestFill_Underflow()
		{
			var source =
				new ArraySegment<int>[]
				{
					new ArraySegment<int>( new int[ 5 ], 1, 3 ),
					new ArraySegment<int>( new int[ 5 ], 1, 3 )
				};

			var expected = RandomSequnce( 1, Int32.MaxValue, 4 ).ToArray();

			Assert.AreEqual( 4L, ArraySegmentsExtensions.Fill( source, expected ) );
			Assert.AreEqual( 0, source[ 0 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 0 ], source[ 0 ].Array[ 1 ] );
			Assert.AreEqual( expected[ 1 ], source[ 0 ].Array[ 2 ] );
			Assert.AreEqual( expected[ 2 ], source[ 0 ].Array[ 3 ] );
			Assert.AreEqual( 0, source[ 0 ].Array[ 4 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 3 ], source[ 1 ].Array[ 1 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 2 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 3 ] );
			Assert.AreEqual( 0, source[ 1 ].Array[ 4 ] );
		}

		[Test]
		public void TestFill_Overflow()
		{
			var source =
				new ArraySegment<int>[]
				{
					new ArraySegment<int>( new int[ 5 ], 1, 3 )
				};

			var expected = RandomSequnce( 1, Int32.MaxValue, 4 ).ToArray();

			Assert.AreEqual( 3L, ArraySegmentsExtensions.Fill( source, expected ) );
			Assert.AreEqual( 0, source[ 0 ].Array[ 0 ] );
			Assert.AreEqual( expected[ 0 ], source[ 0 ].Array[ 1 ] );
			Assert.AreEqual( expected[ 1 ], source[ 0 ].Array[ 2 ] );
			Assert.AreEqual( expected[ 2 ], source[ 0 ].Array[ 3 ] );
			Assert.AreEqual( 0, source[ 0 ].Array[ 4 ] );
		}

		[Test]
		public void TestFill_Empty()
		{
			var value = new Random().Next();
			var source = new ArraySegment<int>[] { new ArraySegment<int>( new int[]{ value }, 0, 1 ) };
			Assert.AreEqual( 0L, ArraySegmentsExtensions.Fill( source, Enumerable.Empty<int>() ) );
			Assert.AreEqual( value, source[ 0 ].Array[ 0 ] );
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestFill_Null()
		{
			var value = new Random().Next();
			var source = new ArraySegment<int>[] { new ArraySegment<int>( new int[] { value }, 0, 1 ) };
			Assert.AreEqual( 0L, ArraySegmentsExtensions.Fill( source, null ) );
		}

		private static IEnumerable<int> RandomSequnce( int min, int max, int length )
		{
			Random random = new Random();
			for ( int i = 0; i < length; i++ )
			{
				yield return random.Next( min, max );
			}
		}
	}
}
