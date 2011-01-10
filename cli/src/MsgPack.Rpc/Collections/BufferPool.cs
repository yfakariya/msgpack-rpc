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

namespace MsgPack.Collections
{
	/// <summary>
	///		Provide basic features and basic interface of buffer pool.
	/// </summary>
	public abstract class BufferPool
	{
		/// <summary>
		///		Get <see cref="ChunkBuffer"/> which has specified size.
		/// </summary>
		/// <param name="length">Size of buffer.</param>
		/// <returns><see cref="ChunkBuffer"/> which has spciefied size.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
		public ChunkBuffer Borrow( int length )
		{
			if ( length <= 0 )
			{
				throw new ArgumentOutOfRangeException( "length", "'length' must be positive." );
			}

			Contract.EndContractBlock();

			return this.BorrowCore( length );
		}

		/// <summary>
		///		Get <see cref="ChunkBuffer"/> which has specified size.
		/// </summary>
		/// <param name="length">Size of buffer.</param>
		/// <returns><see cref="ChunkBuffer"/> which has spciefied size.</returns>
		protected abstract ChunkBuffer BorrowCore( int length );

		/// <summary>
		///		Return spcified buffer to this pool.
		/// </summary>
		/// <param name="buffer">Buffer to return.</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
		/// <exception cref="ArgumentException">Specified buffer was not created by this buffer. Or, specified buffer already has been returned.</exception>
		public void Return( ChunkBuffer buffer )
		{
			if ( buffer == null )
			{
				throw new ArgumentNullException( "buffer" );
			}

			Contract.EndContractBlock();

			this.ReturnCore( buffer );
		}

		/// <summary>
		///		Return spcified buffer to this pool.
		/// </summary>
		/// <param name="buffer">Buffer to return.</param>
		/// <exception cref="ArgumentException">Specified buffer was not created by this buffer. Or, specified buffer already has been returned.</exception>
		protected abstract void ReturnCore( ChunkBuffer buffer );
	}
}
