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
	public sealed class RpcOutputBufferTest
	{
		[Test]
		public void TestReadWrite()
		{
			using ( var target = new RpcOutputBuffer( ChunkBuffer.CreateDefault() ) )
			{
				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
					stream.Write( Enumerable.Range( 11, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 21, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var stream = target.OpenWriteStream() ) { }

				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 31, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( new byte[ 0 ], 0, 0 );
				}

				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 41, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				CollectionAssert.AreEqual(
					Enumerable.Range( 1, 50 ).Select( i => ( byte )i ).ToArray(),
					target.ReadBytes()
				);
			}
		}

		[Test]
		public void TestSwap()
		{
			using ( var target = new RpcOutputBuffer( ChunkBuffer.CreateDefault() ) )
			{
				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var swapper = target.CreateSwapper() )
				{
					CollectionAssert.AreEqual(
						Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(),
						swapper.ReadBytes()
					);

					swapper.WriteBytes( Enumerable.Range( 11, 30 ).Select( i => ( byte )i ) );
				}

				CollectionAssert.AreEqual(
					Enumerable.Range( 11, 30 ).Select( i => ( byte )i ).ToArray(),
					target.ReadBytes().ToArray()
				);
			}
		}

		[Test]
		public void TestSwap_DoneNothing()
		{
			using ( var target = new RpcOutputBuffer( ChunkBuffer.CreateDefault() ) )
			{
				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var swapper = target.CreateSwapper() ) { }

				CollectionAssert.AreEqual(
					Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(),
					target.ReadBytes().ToArray()
				);
			}
		}

		[Test]
		public void TestSwap_WriteBytesEmpty()
		{
			using ( var target = new RpcOutputBuffer( ChunkBuffer.CreateDefault() ) )
			{
				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var swapper = target.CreateSwapper() )
				{
					swapper.WriteBytes( Enumerable.Empty<byte>() );
				}

				Assert.AreEqual( 0, target.ReadBytes().Count() );
			}
		}
		
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestSwap_WriteBytesNull()
		{
			using ( var target = new RpcOutputBuffer( ChunkBuffer.CreateDefault() ) )
			{
				using ( var stream = target.OpenWriteStream() )
				{
					stream.Write( Enumerable.Range( 1, 10 ).Select( i => ( byte )i ).ToArray(), 0, 10 );
				}

				using ( var swapper = target.CreateSwapper() )
				{
					swapper.WriteBytes( null );
				}
			}
		}
	}
}
