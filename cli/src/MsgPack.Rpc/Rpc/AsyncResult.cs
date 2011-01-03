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
using System.Diagnostics.Contracts;
using System.Threading;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Minimal implementation of <see cref="IAsyncResult"/>.
	/// </summary>
	internal class AsyncResult : IAsyncResult
	{
		// State flags
		private const int _initialized = 0;
		private const int _completed = 0x100;
		private const int _completedSynchronously = 0x101;
		private const int _finished = 0x2;

		private readonly object _owner;

		internal object Owner
		{
			get { return _owner; }
		}

		private readonly AsyncCallback _asyncCallback;

		public AsyncCallback AsyncCallback
		{
			get { return this._asyncCallback; }
		}

		private readonly object _asyncState;

		public object AsyncState
		{
			get { return this._asyncState; }
		}

		private ManualResetEvent _asyncWaitHandle;

		public WaitHandle AsyncWaitHandle
		{
			get { return LazyInitializer.EnsureInitialized( ref this._asyncWaitHandle, () => new ManualResetEvent( false ) ); }
		}

		// manipulated via Interlocked methods.
		private int _state;

		bool IAsyncResult.CompletedSynchronously
		{
			get { return ( this._state & _completedSynchronously ) != 0; }
		}

		public bool IsCompleted
		{
			get { return ( this._state & _completed ) != 0; }
		}

		public bool IsFinished
		{
			get { return ( this._state & _finished ) != 0; }
		}

		protected AsyncResult( object owner, AsyncCallback asyncCallback, object asyncState )
		{
			this._owner = owner;
			this._asyncCallback = asyncCallback;
			this._asyncState = asyncState;
		}

		internal void Complete( bool completedSynchronously )
		{
			int state = _completed | ( completedSynchronously ? _completedSynchronously : 0 );
			if ( Interlocked.CompareExchange( ref this._state, state, _initialized ) == _initialized )
			{
				var waitHandle = this._asyncWaitHandle;
				if ( waitHandle != null )
				{
					waitHandle.Set();
				}
			}
		}

		public void Finish()
		{
			Contract.Assert( this._state != _initialized );
			int oldValue = this._state;
			int newValue = this._state | _finished;
			while ( Interlocked.CompareExchange( ref this._state, newValue, oldValue ) != oldValue )
			{
				oldValue = this._state;
				newValue = oldValue | _finished;
			}
		}

		internal static TAsyncResult Verify<TAsyncResult>( IAsyncResult asyncResult, object owner )
			where TAsyncResult : AsyncResult
		{
			Contract.Assert( owner != null );
			if ( asyncResult == null )
			{
				throw new ArgumentNullException( "asyncResult" );
			}

			TAsyncResult result = asyncResult as TAsyncResult;
			if ( result == null )
			{
				throw new ArgumentException( "Unknown asyncResult.", "asyncResult" );
			}

			if ( !Object.ReferenceEquals( result.Owner, owner ) )
			{
				throw new InvalidOperationException( "Async operation was not started on this instance." );
			}

			if ( result.IsFinished )
			{
				throw new InvalidOperationException( "Async operation has already been finished." );
			}

			return result;
		}


	}
}
