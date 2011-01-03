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
	public sealed class ArraySegmentExtensionsTest
	{
		[Test]
		public void TestDevide_Normal()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegment<int> least;
			ArraySegment<int> most;
			ArraySegmentExtensions.Devide( source, 0, out least, out most );
			Assert.AreEqual( 0, least.Count );
			Assert.AreEqual( 1, least.Offset );
			Assert.AreEqual( 3, most.Count );
			Assert.AreEqual( 1, most.Offset );
			ArraySegmentExtensions.Devide( source, 1, out least, out most );
			Assert.AreEqual( 1, least.Count );
			Assert.AreEqual( 1, least.Offset );
			Assert.AreEqual( 2, most.Count );
			Assert.AreEqual( 2, most.Offset );
			ArraySegmentExtensions.Devide( source, 2, out least, out most );
			Assert.AreEqual( 2, least.Count );
			Assert.AreEqual( 1, least.Offset );
			Assert.AreEqual( 1, most.Count );
			Assert.AreEqual( 3, most.Offset );
			ArraySegmentExtensions.Devide( source, 3, out least, out most );
			Assert.AreEqual( 3, least.Count );
			Assert.AreEqual( 1, least.Offset );
			Assert.AreEqual( 0, most.Count );
			Assert.AreEqual( 4, most.Offset );

			source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5, 6 }, 1, 4 );
			ArraySegmentExtensions.Devide( source, 2, out least, out most );
			Assert.AreEqual( 2, least.Count );
			Assert.AreEqual( 1, least.Offset );
			Assert.AreEqual( 2, most.Count );
			Assert.AreEqual( 3, most.Offset );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestDevied_LeastLengthIsNegative()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegment<int> least;
			ArraySegment<int> most;
			ArraySegmentExtensions.Devide( source, -1, out least, out most );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestDevied_LeastLengthIsTooLarge()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegment<int> least;
			ArraySegment<int> most;
			ArraySegmentExtensions.Devide( source, 4, out least, out most );
		}

		[Test]
		public void TestGet_Normal()
		{
			var backingStore = new int[] { 0, 1, 1, 1, 0 };
			var random = new Random();
			int value1 = random.Next( 1, Byte.MaxValue );
			int value2 = random.Next( 1, Byte.MaxValue );
			int value3 = random.Next( 1, Byte.MaxValue );
			backingStore[ 1 ] = value1;
			backingStore[ 2 ] = value2;
			backingStore[ 3 ] = value3;
			var target = new ArraySegment<int>( backingStore, 1, 3 );
			Assert.AreEqual( value1, target.Get( 0 ) );
			Assert.AreEqual( value2, target.Get( 1 ) );
			Assert.AreEqual( value3, target.Get( 2 ) );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGet_IndexIsTooSmall()
		{
			new ArraySegment<int>( new int[] { 0, 1, 0 }, 1, 1 ).Get( -1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestGet_IndexIsTooLarge()
		{
			new ArraySegment<int>( new int[] { 0, 1, 0 }, 1, 1 ).Get( 1 );
		}


		[Test]
		public void TestSet_Normal()
		{
			var backingStore = new int[] { 0, 1, 1, 1, 0 };
			var random = new Random();
			int value1 = random.Next( 1, Byte.MaxValue );
			int value2 = random.Next( 1, Byte.MaxValue );
			int value3 = random.Next( 1, Byte.MaxValue );
			var target = new ArraySegment<int>( backingStore, 1, 3 );
			target.Set( 0, value1 );
			target.Set( 1, value2 );
			target.Set( 2, value3 );
			Assert.AreEqual( 0, backingStore[ 0 ] );
			Assert.AreEqual( value1, backingStore[ 1 ] );
			Assert.AreEqual( value2, backingStore[ 2 ] );
			Assert.AreEqual( value3, backingStore[ 3 ] );
			Assert.AreEqual( 0, backingStore[ 4 ] );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestSet_IndexIsTooSmall()
		{
			new ArraySegment<int>( new int[] { 0, 1, 0 }, 1, 1 ).Set( -1, 2 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestSet_IndexIsTooLarge()
		{
			new ArraySegment<int>( new int[] { 0, 1, 0 }, 1, 1 ).Set( 1, 2 );
		}

		[Test]
		public void TestSubSegment_Normal()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegment<int> actual = ArraySegmentExtensions.SubSegment( source, 0 );
			Assert.AreEqual( 0, actual.Count );
			Assert.AreEqual( 1, actual.Offset );
			actual = ArraySegmentExtensions.SubSegment( source, 1 );
			Assert.AreEqual( 1, actual.Count );
			Assert.AreEqual( 1, actual.Offset );
			actual = ArraySegmentExtensions.SubSegment( source, 2 );
			Assert.AreEqual( 2, actual.Count );
			Assert.AreEqual( 1, actual.Offset );
			actual = ArraySegmentExtensions.SubSegment( source, 3 );
			Assert.AreEqual( 3, actual.Count );
			Assert.AreEqual( 1, actual.Offset );

			source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5, 6 }, 1, 4 );
			actual = ArraySegmentExtensions.SubSegment( source, 2 );
			Assert.AreEqual( 2, actual.Count );
			Assert.AreEqual( 1, actual.Offset );
		}

		// SubSegment
		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestSubSegment_LeastLengthIsNegative()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegmentExtensions.SubSegment( source, -1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestSubSegment_LeastLengthIsTooLarge()
		{
			ArraySegment<int> source = new ArraySegment<int>( new int[] { 1, 2, 3, 4, 5 }, 1, 3 );
			ArraySegmentExtensions.SubSegment( source, 4 );
		}
	}
}
