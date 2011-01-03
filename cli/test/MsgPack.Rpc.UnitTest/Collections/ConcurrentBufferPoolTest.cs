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
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace MsgPack.Collections
{
	[TestFixture]
	public sealed class ConcurrentBufferPoolTest
	{
		[Test]
		[Timeout( 15000 )]
		public void TestBorrowAndReturn_Sequencial()
		{
			var target = new ConcurrentBufferPool();

			TestBorrowAndRetrunCore( target );
		}

		private static void TestBorrowAndRetrunCore( ConcurrentBufferPool target )
		{
			Random random = new Random();
			var written = new List<byte>();

			for ( int i = 0; i < 100; i++ )
			{
				written.Clear();
				int length = random.Next( 256 * 1024 );
				using ( var result = target.Borrow( length ) )
				{
					Assert.NotNull( result );
					Assert.AreEqual( length, result.Sum( item => item.Count ) );
					for ( int j = 0; j < length; j++ )
					{
						var value = ( byte )random.Next( 1, Byte.MaxValue );
						written.Add( value );
					}

					result.Fill( written );
					CollectionAssert.AreEqual( written, result.ReadAll() );
				}
			}
		}

		[Test]
		public void TestBorrowAndReturn_Parallel()
		{
			var target = new ConcurrentBufferPool();
			List<Task> pendingTasks = new List<Task>();

			for ( int i = 0; i < Environment.ProcessorCount * 2; i++ )
			{
				pendingTasks.Add(
					Task.Factory.StartNew( state => TestBorrowAndRetrunCore( state as ConcurrentBufferPool ), target )
				);
			}

			Task.WaitAll( pendingTasks.ToArray(), TimeSpan.FromSeconds( 30 ) );
		}

		[Test]
		[Timeout( 15000 )]
		public void TestReallocate_Sequential()
		{
			var target = new ConcurrentBufferPool( 4 );
			TestReallocate_Sequential_Core( target );
		}

		private static void TestReallocate_Sequential_Core( ConcurrentBufferPool target )
		{
			Random random = new Random();
			var written = new List<byte>();

			for ( int i = 0; i < 100; i++ )
			{
				written.Clear();
				int length = random.Next( 1, 16 );
				using ( var result = target.Borrow( length ) )
				{
					Assert.NotNull( result );
					Assert.AreEqual( length, result.Sum( item => item.Count ) );
					int newLength = ( int )( length * ( random.NextDouble() + 1.0 ) );
					using ( var reallocated = result.Reallocate( newLength ) )
					{
						Assert.AreSame( result, reallocated );
						Assert.GreaterOrEqual( length + newLength, reallocated.Sum( item => item.Count ) );
					}

					for ( int j = 0; j < newLength; j++ )
					{
						written.Add( ( byte )random.Next( Byte.MaxValue ) );
					}

					result.Fill( written );
					CollectionAssertEx.StartsWith( written, result.ReadAll().ToList(), "Reallocate({0}) from {1}", newLength, length );
				}
			}
		}

		[Test]
		public void TestReallocate_Parallel()
		{
			bool success = false;
			var target = new ConcurrentBufferPool( 4 );
			try
			{
				List<Task> pendingTasks = new List<Task>();
				using ( var cts = new CancellationTokenSource() )
				{
					for ( int i = 0; i < Environment.ProcessorCount * 2; i++ )
					{
						pendingTasks.Add(
							Task.Factory.StartNew(
								_ => TestReallocate_Parallel_Core( target, cts ),
								null,
								cts.Token
							)
						);
					}

					Task.WaitAll( pendingTasks.ToArray(), TimeSpan.FromSeconds( 30 ) );
					success = true;
				}
			}
			catch ( AggregateException ex )
			{
				var buffer = new StringBuilder();
				buffer.Append( ex ).AppendLine();
				foreach ( var inner in ex.InnerExceptions )
				{
					buffer.Append( inner ).AppendLine();
				}

				buffer.AppendLine( "------------------------------" );

				Assert.Fail( buffer.ToString() );
			}
			finally
			{
				if ( !success )
				{
					//foreach ( var log in target.DebugGetLogRecords() )
					//{
					//    Console.WriteLine( log );
					//}
				}
			}
		}

		private static int _id;

		private static void TestReallocate_Parallel_Core( ConcurrentBufferPool target, CancellationTokenSource cts )
		{
			Random random = new Random();
			var written = new List<byte>();

			try
			{
				for ( int i = 0; i < 10000; i++ )
				{
					byte id = ( byte )( Interlocked.Increment( ref _id ) % 256 );
					written.Clear();
					int length = random.Next( 1, 40 );
					using ( var result = target.Borrow( length ) )
					{
						if ( cts.Token.IsCancellationRequested )
						{
							Console.WriteLine( "{0:d4}:Cancelled", Thread.CurrentThread.ManagedThreadId );
							target.TraceMarker( "--  Alloc  --" );
							return;
						}

						Assert.NotNull( result );
						Assert.AreEqual( length, result.Sum( item => item.Count ) );
						int newLength = ( int )( length * ( random.NextDouble() + 1.0 ) );
						using ( var reallocated = result.Reallocate( newLength ) )
						{
							if ( cts.Token.IsCancellationRequested )
							{
								Console.WriteLine( "{0:d4}:Cancelled", Thread.CurrentThread.ManagedThreadId );
								target.TraceMarker( "-- Realloc --" );
								return;
							}

							Assert.AreSame( result, reallocated );
							Assert.GreaterOrEqual( length + newLength, reallocated.Sum( item => item.Count ) );
						}

						written.AddRange( Enumerable.Repeat( id, newLength ) );

						result.Fill( written );
						if ( cts.Token.IsCancellationRequested )
						{
							Console.WriteLine( "{0:d4}:Cancelled", Thread.CurrentThread.ManagedThreadId );
							target.TraceMarker( "--  Write  --" );
							return;
						}

						var actual = result.ReadAll().ToList();
						CollectionAssertEx.StartsWith(
							written,
							actual,
							"ID:{1} Reallocate({2}) from {3}{0}{4}{0}{5}",
							Environment.NewLine,
							id,
							newLength,
							length,
							BitConverter.ToString( actual.Take( length + newLength ).ToArray() ),
							BitConverter.ToString( actual.Skip( length + newLength ).ToArray() )
						);
					}

					if ( cts.Token.IsCancellationRequested )
					{
						Console.WriteLine( "{0:d4}:Cancelled", Thread.CurrentThread.ManagedThreadId );
						target.TraceMarker( "--  Free   --" );
						return;
					}
				}
			}
			catch ( Exception )
			{
				if ( cts.IsCancellationRequested )
				{
					cts.Cancel();
					Console.WriteLine( "{0:d4}:Cancel", Thread.CurrentThread.ManagedThreadId );
				}
				throw;
			}
		}

	}
}
