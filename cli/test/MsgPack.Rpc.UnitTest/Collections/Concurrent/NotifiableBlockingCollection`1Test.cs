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
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace MsgPack.Collections.Concurrent
{
	[TestFixture]
	public sealed class NotifiableBlockingCollection_1Test
	{
		/// <summary>
		///		Test that ConsumerWaitHandle is usable for ThreadPool.RegisterWaitForSingleObject.
		/// </summary>
		[Test]
		[Timeout( 3000 )]
		public void ThreadPoolTest()
		{
			ConcurrentQueue<int> underlying = new ConcurrentQueue<int>();
			bool? isFired = null;
			int? taken = null;
			using ( var target = new NotifiableBlockingCollection<int>( underlying, 1 ) )
			using ( var done = new ManualResetEventSlim() )
			{
				var registeredWaitHandle =
					ThreadPool.RegisterWaitForSingleObject(
						target.ConsumerWaitHandle,
						( arg0, arg1 ) =>
						{
							int val;
							( arg0 as NotifiableBlockingCollection<int> ).Take( CancellationToken.None, out val );
							isFired = arg1;
							taken = val;
							done.Set();
						},
						target,
						-1,
						true
					);
				try
				{
					Assert.False( done.Wait( TimeSpan.FromSeconds( 1 ) ) );
					int value = Environment.TickCount;
					bool isAdded;
					target.Add( value, CancellationToken.None, out isAdded );
					Assert.True( isAdded );
					Assert.True( done.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Assert.False( isFired.Value );
					Assert.AreEqual( value, taken.Value );
				}
				finally
				{
					registeredWaitHandle.Unregister( target.ConsumerWaitHandle );
				}
			}
		}

		/// <summary>
		///		Test that when collection is empty, Take is blocking.
		/// </summary>
		[Test]
		[Timeout( 3000 )]
		public void EmptyTest()
		{
			ConcurrentQueue<int> underlying = new ConcurrentQueue<int>();
			using ( var target = new NotifiableBlockingCollection<int>( underlying, 1 ) )
			using ( var ready = new ManualResetEventSlim() )
			{
				var thread =
					new Thread(
						item =>
						{
							int dummy;
							try
							{
								ready.Set();
								( item as NotifiableBlockingCollection<int> ).Take( CancellationToken.None, out dummy );
								Assert.Fail( "Failed to block." );
							}
							catch ( ThreadInterruptedException ) { }
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					thread.Interrupt();
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}

				ready.Reset();
				bool? isOk = null;
				int expected = Environment.TickCount;
				thread =
					new Thread(
						item =>
						{
							int value;
							ready.Set();
							( item as NotifiableBlockingCollection<int> ).Take( CancellationToken.None, out value );
							isOk = ( expected == value );
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Thread.Yield();
					bool added;
					target.Add( expected, CancellationToken.None, out added );
					Assert.True( added );
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
					Assert.True( isOk.Value );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}
			}
		}

		/// <summary>
		///		Test that when collection reaches to limit, Add attempt is blocked.
		/// </summary>
		[Test]
		[Timeout( 3000 )]
		public void LimitationTest()
		{
			ConcurrentQueue<int> underlying = new ConcurrentQueue<int>();
			Random random = new Random();
			int expected1 = random.Next();
			int expected2 = random.Next();
			using ( var target = new NotifiableBlockingCollection<int>( underlying, 1 ) )
			using ( var ready = new ManualResetEventSlim() )
			{
				var thread =
					new Thread(
						item =>
						{
							bool added;
							( item as NotifiableBlockingCollection<int> ).Add( expected1, CancellationToken.None, out added );
							Assert.True( added );
							added = false;
							try
							{
								ready.Set();
								( item as NotifiableBlockingCollection<int> ).Add( expected2, CancellationToken.None, out added );
								Assert.Fail( "Failed to block." );
							}
							catch ( ThreadInterruptedException ) { }
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					thread.Interrupt();
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}

				ready.Reset();
				thread =
					new Thread(
						item =>
						{
							ready.Set();
							bool added;
							( item as NotifiableBlockingCollection<int> ).Add( expected2, CancellationToken.None, out added );
							Assert.True( added );
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Thread.Yield();
					int value;
					target.Take( CancellationToken.None, out value );
					Assert.AreEqual( expected1, value );
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
					target.Take( CancellationToken.None, out value );
					Assert.AreEqual( expected2, value );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}
			}
		}

		/// <summary>
		///		Test cancellation is work.
		/// </summary>
		[Test]
		[Timeout( 3000 )]
		public void CancellationTest()
		{
			ConcurrentQueue<int> underlying = new ConcurrentQueue<int>();
			using ( var tokenSource = new CancellationTokenSource() )
			using ( var target = new NotifiableBlockingCollection<int>( underlying, 1 ) )
			using ( var ready = new ManualResetEventSlim() )
			{
				var thread =
					new Thread(
						item =>
						{
							ready.Set();
							try
							{
								int dummy;
								target.Take( tokenSource.Token, out dummy );
								Assert.Fail( "Not blocked" );
							}
							catch ( OperationCanceledException ) { }
							catch ( ThreadInterruptedException )
							{
								Assert.Fail( "Interuptted" );
							}
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Thread.Yield();
					tokenSource.Cancel( true );
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}

				ready.Reset();
				bool added;
				target.Add( Environment.TickCount, CancellationToken.None, out added );
				Assert.True( added );
				thread =
					new Thread(
						item =>
						{
							ready.Set();
							bool dummy;
							try
							{
								( item as NotifiableBlockingCollection<int> ).Add( Environment.TickCount, tokenSource.Token, out dummy );
								Assert.Fail( "Not blocked." );
							}
							catch ( OperationCanceledException ) { }
							catch ( ThreadInterruptedException )
							{
								Assert.Fail( "Interuppted." );
							}
						}
					);

				thread.Start( target );
				try
				{
					Assert.True( ready.Wait( TimeSpan.FromSeconds( 1 ) ) );
					Thread.Yield();
					tokenSource.Cancel( true );
					Assert.True( thread.Join( TimeSpan.FromSeconds( 1 ) ) );
				}
				finally
				{
					if ( thread.IsAlive )
					{
						thread.Abort();
					}
				}
			}
		}
	}
}
