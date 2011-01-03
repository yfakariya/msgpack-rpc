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
using MsgPack.Rpc.Protocols;
using System.IO;

#warning Some Contract.Assume should be Contract.Assert
#warning TODO: code clearning and refactoring.

namespace MsgPack.Rpc.Serialization
{
	[TestFixture]
	[Timeout( 3000 )]
	public sealed class ResponseMessageSerializerTest
	{
		[Test]
		public void TestSerialize_NonVoid()
		{
			TestSerializeCore( Environment.TickCount, Guid.NewGuid().ToString(), false, null );
		}

		[Test]
		public void TestSerialize_Void()
		{
			TestSerializeCore( Environment.TickCount, null, true, null );
		}

		[Test]
		public void TestSerialize_Error()
		{
			TestSerializeCore( Environment.TickCount, null, false, new RpcException( RpcError.RemoteRuntimeError, "Failure", Environment.StackTrace ) );
		}

		private static void TestSerializeCore( int id, object returnValue, bool isVoid, RpcException error )
		{
			if ( isVoid )
			{
				Assert.IsNull( returnValue, "Return value should not be specified in void." );
			}

			if ( error != null )
			{
				Assert.IsNull( returnValue, "Return value shoud not be specified in error." );
				Assert.IsFalse( isVoid, "isVoid should be false in error test." );
			}

			// TODO: Mock filters
			var target =
				new ResponseMessageSerializer(
					Constants.EmptyResponseMessageSerializationFilterProviders,
					Constants.EmptySerializedMessageFilterProviders,
					Constants.EmptyDeserializedMessageFilterProviders,
					Constants.EmptyResponseMessageDeserializationFilterProviders,
					null
				);

			var bufferPool = new AdHocPseudoBufferPool();
			var buffer = new RpcOutputBuffer( bufferPool );

			Assert.IsTrue( target.Serialize( id, returnValue, isVoid, error, buffer ).IsSuccess );
			byte[] serialized = buffer.Chunks.ReadAll().ToArray();
			var mpo =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Response ),
						new MessagePackObject( ( uint )id ),
						error == null ? MessagePackObject.Nil : error.RpcError.Identifier,
						returnValue == null 
							? ( error == null ? MessagePackObject.Nil : ToErrorDetail(error) )
							: MessagePackObject.FromObject( returnValue )						
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( mpo );
			CollectionAssert.AreEqual( stream.ToArray(), serialized );
		}

		private static MessagePackObject ToErrorDetail( RpcException error )
		{
			if ( error == null )
			{
				return MessagePackObject.Nil;
			}

			var dictionary = new Dictionary<MessagePackObject, MessagePackObject>( 3 );
			dictionary[ "ErrorCode" ] = error.RpcError.ErrorCode;
			dictionary[ "Description" ] = error.Message;
			if ( error.DebugInformation != null )
			{
				dictionary[ "DebugInformation" ] = error.DebugInformation;
			}

			return new MessagePackObject( dictionary );
		}

		[Test]
		public void TestDeerialize_Normal_NonVoid()
		{
			TestDeserializeNormalCore( Environment.TickCount, Guid.NewGuid().ToString(), false, null );
		}

		[Test]
		public void TestDeerialize_Normal_Void()
		{
			TestDeserializeNormalCore( Environment.TickCount, null, true, null );
		}

		[Test]
		public void TestDeerialize_Normal_Error()
		{
			TestDeserializeNormalCore( Environment.TickCount, null, false, new RpcException( RpcError.RemoteRuntimeError, "Failure", Environment.StackTrace ) );
		}

		private static void TestDeserializeNormalCore( int id, object returnValue, bool isVoid, RpcException error )
		{
			if ( isVoid )
			{
				Assert.IsNull( returnValue, "Return value should not be specified in void." );
			}

			if ( error != null )
			{
				Assert.IsNull( returnValue, "Return value shoud not be specified in error." );
				Assert.IsFalse( isVoid, "isVoid should be false in error test." );
			}

			// TODO: Mock filters
			var target =
				new ResponseMessageSerializer(
					Constants.EmptyResponseMessageSerializationFilterProviders,
					Constants.EmptySerializedMessageFilterProviders,
					Constants.EmptyDeserializedMessageFilterProviders,
					Constants.EmptyResponseMessageDeserializationFilterProviders,
					null
				);

			var bufferPool = new AdHocPseudoBufferPool();

			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Response ),
						new MessagePackObject( ( uint )id ),
						error == null ? MessagePackObject.Nil : error.RpcError.Identifier,
						returnValue == null 
							? ( error == null ? MessagePackObject.Nil : ToErrorDetail(error) )
							: MessagePackObject.FromObject( returnValue )					
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			using ( var underlying = bufferPool.Borrow( serialized.Length ) )
			{
				underlying.Fill( serialized );
				using ( var buffer = new RpcInputBuffer( underlying, serialized.Length, FeedingNotRequired ) )
				{
					ResponseMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( ( int )MessageType.Response, actual.MessageType.AsInt32() );
					Assert.AreEqual( id, actual.MessageId );
					if ( isVoid || returnValue == null )
					{
						Assert.IsTrue( actual.ReturnValue.IsNil );
					}
					else
					{
						Assert.AreEqual( returnValue.ToString(), actual.ReturnValue.AsString() );
					}

					if ( error == null )
					{
						Assert.IsNull( actual.Error );
					}
					else
					{
						Assert.AreEqual( expected.AsList()[ 2 ].AsString(), actual.Error.RpcError.Identifier );
						Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "ErrorCode" ].AsInt32(), actual.Error.RpcError.ErrorCode );
						Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "Description" ].AsString(), actual.Error.Message );
						if ( expected.AsList()[ 3 ].AsDictionary().ContainsKey( "DebugInformation" ) )
						{
							Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "DebugInformation" ].AsString(), actual.Error.DebugInformation );
						}
						else
						{
							Assert.IsNull( actual.Error.DebugInformation );
						}
					}
				}
			}
		}

		[Test]
		public void TestDeserialize_Devided_NonVoid()
		{
			TestDeserializeDeviedCore( Environment.TickCount, Guid.NewGuid().ToString(), false, null );
		}

		[Test]
		public void TestDeserialize_Devided_Void()
		{
			TestDeserializeDeviedCore( Environment.TickCount, null, true, null );
		}

		[Test]
		public void TestDeserialize_Devided_Error()
		{
			TestDeserializeDeviedCore( Environment.TickCount, null, false, new RpcException( RpcError.RemoteRuntimeError, "Failure", Environment.StackTrace ) );
		}

		private static void TestDeserializeDeviedCore( int id, object returnValue, bool isVoid, RpcException error )
		{
			if ( isVoid )
			{
				Assert.IsNull( returnValue, "Return value should not be specified in void." );
			}

			if ( error != null )
			{
				Assert.IsNull( returnValue, "Return value shoud not be specified in error." );
				Assert.IsFalse( isVoid, "isVoid should be false in error test." );
			}
			// TODO: Mock filters
			var target =
				new ResponseMessageSerializer(
					Constants.EmptyResponseMessageSerializationFilterProviders,
					Constants.EmptySerializedMessageFilterProviders,
					Constants.EmptyDeserializedMessageFilterProviders,
					Constants.EmptyResponseMessageDeserializationFilterProviders,
					null
				);

			var bufferPool = new AdHocPseudoBufferPool();

			var expected =
				new MessagePackObject(
					new MessagePackObject[]
					{
						new MessagePackObject( ( int )MessageType.Response ),
						new MessagePackObject( ( uint )id ),
						error == null ? MessagePackObject.Nil : error.RpcError.Identifier,
						returnValue == null 
							? ( error == null ? MessagePackObject.Nil : ToErrorDetail(error) )
							: MessagePackObject.FromObject( returnValue )						
					}
				);
			var stream = new MemoryStream();
			Packer.Create( stream ).Pack( expected );
			var serialized = stream.ToArray();
			var packets = Segmentate( serialized, 10 ).ToArray();
			int indexOfPackets = 0;
			using ( var underlying = bufferPool.Borrow( 10 ) )
			{
				Assert.AreEqual( Math.Min( serialized.Length, 10L ), underlying.Fill( packets[ 0 ] ) );
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

							var reallocated = item.Reallocate( 10 );

							return new BufferFeeding( ( int )reallocated.Fill( packets[ indexOfPackets ], 10 * indexOfPackets ), reallocated );
						}
					) )
				{
					ResponseMessage actual;
					var result = target.Deserialize( buffer, out actual );
					Assert.IsTrue( result.IsSuccess, result.ToString() );
					Assert.AreEqual( ( int )MessageType.Response, actual.MessageType.AsInt32() );
					Assert.AreEqual( id, actual.MessageId );
					if ( isVoid || returnValue == null )
					{
						Assert.IsTrue( actual.ReturnValue.IsNil );
					}
					else
					{
						Assert.AreEqual( returnValue.ToString(), actual.ReturnValue.AsString() );
					}

					if ( error == null )
					{
						Assert.IsNull( actual.Error );
					}
					else
					{
						Assert.AreEqual( expected.AsList()[ 2 ].AsString(), actual.Error.RpcError.Identifier );
						Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "ErrorCode" ].AsInt32(), actual.Error.RpcError.ErrorCode );
						Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "Description" ].AsString(), actual.Error.Message );
						if ( expected.AsList()[ 3 ].AsDictionary().ContainsKey( "DebugInformation" ) )
						{
							Assert.AreEqual( expected.AsList()[ 3 ].AsDictionary()[ "DebugInformation" ].AsString(), actual.Error.DebugInformation );
						}
						else
						{
							Assert.IsNull( actual.Error.DebugInformation );
						}
					}
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

