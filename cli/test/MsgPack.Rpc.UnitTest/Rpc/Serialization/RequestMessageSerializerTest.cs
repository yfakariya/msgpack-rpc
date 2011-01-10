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
using System.IO;
using System.Linq;
using MsgPack.Collections;
using MsgPack.Rpc.Protocols;
using NUnit.Framework;

namespace MsgPack.Rpc.Serialization
{
	[TestFixture]
	[Timeout( 3000 )]
	public sealed class RequestMessageSerializerTest
	{
		[Test]
		public void TestSerialize_Request()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );

			var id = Environment.TickCount;
			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var buffer = new RpcOutputBuffer();
			Assert.IsTrue( target.Serialize( id, method, args, buffer ).IsSuccess );
			byte[] serialized = buffer.ReadBytes().ToArray();
			var mpo =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Request ),
						new MessagePackObject( ( uint )id ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( mpo );
			CollectionAssert.AreEqual( stream.ToArray(), serialized );
		}

		[Test]
		public void TestSerialize_Notify()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );

			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var buffer = new RpcOutputBuffer();
			Assert.IsTrue( target.Serialize( null, method, args, buffer ).IsSuccess );
			byte[] serialized = buffer.ReadBytes().ToArray();
			var mpo =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Notification ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( mpo );
			CollectionAssert.AreEqual( stream.ToArray(), serialized );
		}

		[Test]
		public void TestDeerialize_Request_Normal()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );

			var id = Environment.TickCount;
			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Request ),
						new MessagePackObject( ( uint )id ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			using ( var underlying = ChunkBuffer.CreateDefault() )
			{
				underlying.Feed( new ArraySegment<byte>( serialized ) );
				using ( var buffer = new RpcInputBuffer( underlying, serialized.Length, FeedingNotRequired ) )
				{
					RequestMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( MessageType.Request, actual.MessageType );
					Assert.AreEqual( id, actual.MessageId );
					Assert.AreEqual( method, actual.Method );
					Assert.AreEqual( 1, actual.Arguments[ 0 ].AsInt32() );
					Assert.AreEqual( "String", actual.Arguments[ 1 ].AsString() );
					Assert.IsTrue( actual.Arguments[ 2 ].IsNil );
					Assert.IsTrue( actual.Arguments[ 3 ].AsBoolean() );
				}
			}
		}

		[Test]
		public void TestDeerialize_Notification_Normal()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );

			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Notification ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			using ( var underlying = ChunkBuffer.CreateDefault() )
			{
				underlying.Feed( new ArraySegment<byte>( serialized ) );
				using ( var buffer = new RpcInputBuffer( underlying, serialized.Length, FeedingNotRequired ) )
				{
					RequestMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( MessageType.Notification, actual.MessageType );
					Assert.AreEqual( method, actual.Method );
					Assert.AreEqual( 1, actual.Arguments[ 0 ].AsInt32() );
					Assert.AreEqual( "String", actual.Arguments[ 1 ].AsString() );
					Assert.IsTrue( actual.Arguments[ 2 ].IsNil );
					Assert.IsTrue( actual.Arguments[ 3 ].AsBoolean() );
				}
			}
		}

		[Test]
		public void TestDeserialize_Request_Devided()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );
			;
			var id = Environment.TickCount;
			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Request ),
						new MessagePackObject( ( uint )id ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			var packets = Segmentate( serialized, 10 ).ToArray();
			int indexOfPackets = 0;
			using ( var underlying = ChunkBuffer.CreateDefault() )
			{
				underlying.Feed( new ArraySegment<byte>( packets[ 0 ] ) );
				using ( var buffer =
					new RpcInputBuffer(
						underlying,
						10,
						item =>
						{
							indexOfPackets++;
							if ( indexOfPackets >= packets.Length )
							{
								return default( BufferFeeding );
								//Assert.Fail( "Over requesting." );
							}

							item.Feed( new ArraySegment<byte>( packets[ indexOfPackets ] ) );
							return new BufferFeeding( packets[ indexOfPackets ].Length );
						}
					) )
				{
					RequestMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( MessageType.Request, actual.MessageType );
					Assert.AreEqual( id, actual.MessageId );
					Assert.AreEqual( method, actual.Method );
					Assert.AreEqual( 1, actual.Arguments[ 0 ].AsInt32() );
					Assert.AreEqual( "String", actual.Arguments[ 1 ].AsString() );
					Assert.IsTrue( actual.Arguments[ 2 ].IsNil );
					Assert.IsTrue( actual.Arguments[ 3 ].AsBoolean() );
				}
			}
		}

		[Test]
		public void TestDeserialize_Notification_Devided()
		{
			// TODO: Mock filters
			var target = new RequestMessageSerializer( null, null, null, null, null );

			var method = Guid.NewGuid().ToString();
			var args = new object[] { 1, "String", null, true };
			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Notification ),
						new MessagePackObject( method ),
						new MessagePackObject[]
						{
							new MessagePackObject( 1 ),
							new MessagePackObject( "String" ),
							MessagePackObject.Nil,
							new MessagePackObject( true )
						}
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			var packets = Segmentate( serialized, 10 ).ToArray();
			int indexOfPackets = 0;
			using ( var underlying = ChunkBuffer.CreateDefault() )
			{
				underlying.Feed( new ArraySegment<byte>( packets[ 0 ] ) );
				using ( var buffer =
					new RpcInputBuffer(
						underlying,
						10,
						item =>
						{
							indexOfPackets++;
							if ( indexOfPackets >= packets.Length )
							{
								Assert.Fail( "Over requesting." );
							};

							item.Feed( new ArraySegment<byte>( packets[ indexOfPackets ] ) );
							return new BufferFeeding( packets[ indexOfPackets ].Length );
						}
					) )
				{
					RequestMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( MessageType.Notification, actual.MessageType );
					Assert.AreEqual( method, actual.Method );
					Assert.AreEqual( 1, actual.Arguments[ 0 ].AsInt32() );
					Assert.AreEqual( "String", actual.Arguments[ 1 ].AsString() );
					Assert.IsTrue( actual.Arguments[ 2 ].IsNil );
					Assert.IsTrue( actual.Arguments[ 3 ].AsBoolean() );
				}
			}
		}

		private static BufferFeeding FeedingNotRequired( ChunkBuffer buffer )
		{
			Assert.Fail( "Feeding must not required." );
			return default( BufferFeeding );
		}

		private static IEnumerable<byte[]> Segmentate( IEnumerable<byte> source, int length )
		{
			using ( var iterator = source.GetEnumerator() )
			{
				List<byte> buffer = new List<byte>( length );
				while ( true )
				{
					for ( int i = 0; i < length; i++ )
					{
						if ( !iterator.MoveNext() )
						{
							yield return buffer.ToArray();
							yield break;
						}

						buffer.Add( iterator.Current );
					}

					yield return buffer.ToArray();
					buffer.Clear();
				}
			}
		}
	}
}
