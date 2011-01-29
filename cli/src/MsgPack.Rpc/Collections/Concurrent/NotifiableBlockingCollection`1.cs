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

namespace MsgPack.Collections.Concurrent
{
	/// <summary>
	///		Custom BlockCollection implentation which can be used with <see cref="ThreadPool.RegisterWaitForSingleObject(WaitHandle,WaitOrTimerCallback,Object,TimeSpan,Boolean)"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal sealed class NotifiableBlockingCollection<T> : IDisposable
	{
		private readonly IProducerConsumerCollection<T> _underlying;
		private readonly SemaphoreSlim _producerBlockingSemaphore;
		private readonly SemaphoreSlim _consumerBlockingSemaphore;
		private readonly int _limit;

		/// <summary>
		///		Get <see cref="WaitHandle"/> to wait for consumer thread available.
		/// </summary>
		public WaitHandle ConsumerWaitHandle
		{
			get { return this._consumerBlockingSemaphore.AvailableWaitHandle; }
		}

		/// <summary>
		///		Initialize new instance which wraps specified <see cref="IProducerConsumerCollection&lt;T&gt;"/>.
		/// </summary>
		/// <param name="underlying"><see cref="IProducerConsumerCollection&lt;T&gt;"/> to be wrapped.</param>
		/// <param name="limit">Maximum count of collection to be added.</param>
		public NotifiableBlockingCollection( IProducerConsumerCollection<T> underlying, int? limit )
		{
			this._underlying = underlying;
			this._limit = limit ?? Int32.MaxValue;
			this._producerBlockingSemaphore = new SemaphoreSlim( limit ?? Int32.MaxValue, limit ?? Int32.MaxValue );
			this._consumerBlockingSemaphore = new SemaphoreSlim( 0, limit ?? Int32.MaxValue );
		}

		public void Dispose()
		{
			this._consumerBlockingSemaphore.Dispose();
			this._producerBlockingSemaphore.Dispose();
		}

		// out variable semantics is more reliable than return value for asynchronous exception.
		/// <summary>
		///		Add specified item to this collection.
		/// </summary>
		/// <param name="item">Item to be added.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel waiting until collection will be not full.</param>
		/// <param name="added">If success to add <paramref name="item"/> then true.</param>
		public void Add( T item, CancellationToken cancellationToken, out bool added )
		{
			this._producerBlockingSemaphore.Wait( cancellationToken );

			try { }
			finally
			{
				added = this._underlying.TryAdd( item );
				this._consumerBlockingSemaphore.Release();
			}
		}

		// out variable semantics is more reliable than return value for asynchronous exception.
		/// <summary>
		///		Try to take value and remove it from this collection.
		/// </summary>
		/// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel waiting until collection will not be empty.</param>
		/// <param name="value">Taken value.</param>
		public void Take( CancellationToken cancellationToken, out T value )
		{
			bool taken = false;
			try
			{
				do
				{
					this._consumerBlockingSemaphore.Wait( cancellationToken );
					try { }
					finally
					{
						taken = this._underlying.TryTake( out value );
					}
				} while ( !taken );
			}
			finally
			{
				if ( taken )
				{
					this._producerBlockingSemaphore.Release();
				}
			}
		}
	}

}
