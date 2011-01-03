using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MsgPack.Collections
{
	[TestFixture]
	public sealed class ByteArraySegmentExtensionsTest
	{
		[Test]
		public void TestCopyFrom_Full()
		{
			var backingArray = new byte[ 12 ];
			var values = new byte[ 10 ];
			var segment = new ArraySegment<byte>( backingArray, 1, 10 );
			var random = new Random();
			random.NextBytes( values );

			if ( values[ 0 ] == 0 )
			{
				values[ 0 ] = ( byte )random.Next( 1, Byte.MaxValue );
			}

			if ( values[ 9 ] == 0 )
			{
				values[ 9 ] = ( byte )random.Next( 1, Byte.MaxValue );
			}

			ByteArraySegmentExtensions.CopyFrom( segment, 0, values, 0, 10 );
			Assert.AreEqual( 0, backingArray[ 0 ] );
			for ( int i = 0; i < 10; i++ )
			{
				Assert.AreEqual( values[ i ], backingArray[ i + 1 ] );
			}
			Assert.AreEqual( 0, backingArray[ 11 ] );
		}

		[Test]
		public void TestCopyFrom_Partial()
		{
			var backingArray = new byte[ 12 ];
			var values = new byte[ 10 ];
			var segment = new ArraySegment<byte>( backingArray, 1, 10 );
			var random = new Random();
			random.NextBytes( values );

			if ( values[ 0 ] == 0 )
			{
				values[ 0 ] = ( byte )random.Next( 1, Byte.MaxValue );
			}

			if ( values[ 9 ] == 0 )
			{
				values[ 9 ] = ( byte )random.Next( 1, Byte.MaxValue );
			}

			ByteArraySegmentExtensions.CopyFrom( segment, 1, values, 1, 8 );
			Assert.AreEqual( 0, backingArray[ 0 ] );
			Assert.AreEqual( 0, backingArray[ 1 ] );
			for ( int i = 0; i < 8; i++ )
			{
				Assert.AreEqual( values[ i + 1 ], backingArray[ i + 2 ] );
			}
			Assert.AreEqual( 0, backingArray[ 10 ] );
			Assert.AreEqual( 0, backingArray[ 11 ] );
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestCopyFrom_SourceOffsetIsTooSmall()
		{
			ByteArraySegmentExtensions.CopyFrom( new ArraySegment<byte>( new byte[ 1 ], 0, 1 ), -1, new byte[ 1 ], 0, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentOutOfRangeException ) )]
		public void TestCopyFrom_SourceOffsetIsTooLarge()
		{
			ByteArraySegmentExtensions.CopyFrom( new ArraySegment<byte>( new byte[ 1 ], 0, 1 ), 1, new byte[ 1 ], 0, 1 );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void TestCopyFrom_SourceIsTooSmall()
		{
			ByteArraySegmentExtensions.CopyFrom( new ArraySegment<byte>( new byte[ 1 ], 0, 1 ), 0, new byte[ 2 ], 0, 2 );
		}
	}
}
