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

namespace MsgPack.Rpc
{
	// This file generated from RpcClientOptions.tt T4Template.
	// Do not modify this file. Edit RpcClientOptions.tt instead.

	/// <summary>
	///		Represents various configuration information of MessagePack-RPC client.
	/// </summary>
	public sealed class RpcClientOptions
	{
		private bool _isFrozen;

		/// <summary>
		///		Get the value which indicates this instance is frozen or not.
		/// </summary>
		/// <value>
		///		If this instance is frozen then true.
		/// </value>
		public bool IsFrozen
		{
			get{ return this._isFrozen; }
		}

		/// <summary>
		///		Freeze this instance.
		/// </summary>
		/// <remarks>
		///		Frozen instance will be immutable.
		/// </remarks>
		public void Freeze()
		{
			this._isFrozen = true;
		}

		private int? _ChunkSize;

		/// <summary>
		///		Get buffer chunk size of buffer in bytes.
		/// </summary>
		/// <value>
		///		Buffer chunk size of buffer in bytes.
		/// </value>
		public int? ChunkSize
		{
			get
			{
				return this._ChunkSize;
			}
		}
		
		/// <summary>
		///		Set buffer chunk size of buffer in bytes.
		/// </summary>
		/// <param name="value">
		///		Buffer chunk size of buffer in bytes.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetChunkSize( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ChunkSize = value;
		}

		private int? _ConnectingConcurrency;

		/// <summary>
		///		Get concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		public int? ConnectingConcurrency
		{
			get
			{
				return this._ConnectingConcurrency;
			}
		}
		
		/// <summary>
		///		Set concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <param name="value">
		///		Concurrency of 'Connect' operation in <see cref="PollingClientEventLoop"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetConnectingConcurrency( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ConnectingConcurrency = value;
		}

		private int? _SendingConcurrency;

		/// <summary>
		///		Get concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		public int? SendingConcurrency
		{
			get
			{
				return this._SendingConcurrency;
			}
		}
		
		/// <summary>
		///		Set concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <param name="value">
		///		Concurrency of 'Send' operation in <see cref="PollingClientEventLoop"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetSendingConcurrency( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._SendingConcurrency = value;
		}

		private int? _ReceivingConcurrency;

		/// <summary>
		///		Get concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </value>
		public int? ReceivingConcurrency
		{
			get
			{
				return this._ReceivingConcurrency;
			}
		}
		
		/// <summary>
		///		Set concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <param name="value">
		///		Concurrency of 'Receive' operation in <see cref="PollingClientEventLoop"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetReceivingConcurrency( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ReceivingConcurrency = value;
		}

		private int? _ConnectingQueueLength;

		/// <summary>
		///		Get number of connection to pool.
		/// </summary>
		/// <value>
		///		Number of connection to pool
		/// </value>
		public int? ConnectingQueueLength
		{
			get
			{
				return this._ConnectingQueueLength;
			}
		}
		
		/// <summary>
		///		Set number of connection to pool.
		/// </summary>
		/// <param name="value">
		///		Number of connection to pool
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetConnectingQueueLength( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ConnectingQueueLength = value;
		}

		private int? _SendingQueueLength;

		/// <summary>
		///		Get limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </value>
		public int? SendingQueueLength
		{
			get
			{
				return this._SendingQueueLength;
			}
		}
		
		/// <summary>
		///		Set limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <param name="value">
		///		Limit of queue of sending message in <see cref="PollingClientEventLoop"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetSendingQueueLength( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._SendingQueueLength = value;
		}

		private int? _ReceivingQueueLength;

		/// <summary>
		///		Get limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <value>
		///		Limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </value>
		public int? ReceivingQueueLength
		{
			get
			{
				return this._ReceivingQueueLength;
			}
		}
		
		/// <summary>
		///		Set limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </summary>
		/// <param name="value">
		///		Limit of queue of receiving message in <see cref="PollingClientEventLoop"/>.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetReceivingQueueLength( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ReceivingQueueLength = value;
		}

		private bool? _UseConnectionPooling;

		/// <summary>
		///		Get whether connection pooling is used.
		/// </summary>
		/// <value>
		///		If connection pooling is used then true.
		/// </value>
		public bool? UseConnectionPooling
		{
			get
			{
				return this._UseConnectionPooling;
			}
		}
		
		/// <summary>
		///		Set whether connection pooling is used.
		/// </summary>
		/// <param name="value">
		///		If connection pooling is used then true.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetUseConnectionPooling( bool? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._UseConnectionPooling = value;
		}

		private bool? _ForceIPv4;

		/// <summary>
		///		Get whether force using IP v4 even if IP v6 is available.
		/// </summary>
		/// <value>
		///		If IP v4 is forced then true.
		/// </value>
		public bool? ForceIPv4
		{
			get
			{
				return this._ForceIPv4;
			}
		}
		
		/// <summary>
		///		Set whether force using IP v4 even if IP v6 is available.
		/// </summary>
		/// <param name="value">
		///		If IP v4 is forced then true.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetForceIPv4( bool? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ForceIPv4 = value;
		}

		private int? _MaximumRequestQuota;

		/// <summary>
		///		Get maximum request length in bytes.
		/// </summary>
		/// <value>
		///		Maximum request length in bytes.
		/// </value>
		public int? MaximumRequestQuota
		{
			get
			{
				return this._MaximumRequestQuota;
			}
		}
		
		/// <summary>
		///		Set maximum request length in bytes.
		/// </summary>
		/// <param name="value">
		///		Maximum request length in bytes.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetMaximumRequestQuota( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._MaximumRequestQuota = value;
		}

		private int? _MaximumResponseQuota;

		/// <summary>
		///		Get maximum response length in bytes.
		/// </summary>
		/// <value>
		///		Maximum response length in bytes.
		/// </value>
		public int? MaximumResponseQuota
		{
			get
			{
				return this._MaximumResponseQuota;
			}
		}
		
		/// <summary>
		///		Set maximum response length in bytes.
		/// </summary>
		/// <param name="value">
		///		Maximum response length in bytes.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetMaximumResponseQuota( int? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._MaximumResponseQuota = value;
		}

		private TimeSpan? _ConnectTimeout;

		/// <summary>
		///		Get socket connect timeout.
		/// </summary>
		/// <value>
		///		Socket connect timeout.
		/// </value>
		public TimeSpan? ConnectTimeout
		{
			get
			{
				return this._ConnectTimeout;
			}
		}
		
		/// <summary>
		///		Set socket connect timeout.
		/// </summary>
		/// <param name="value">
		///		Socket connect timeout.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetConnectTimeout( TimeSpan? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._ConnectTimeout = value;
		}

		private TimeSpan? _DrainTimeout;

		/// <summary>
		///		Get socket drain timeout.
		/// </summary>
		/// <value>
		///		Socket drain timeout.
		/// </value>
		public TimeSpan? DrainTimeout
		{
			get
			{
				return this._DrainTimeout;
			}
		}
		
		/// <summary>
		///		Set socket drain timeout.
		/// </summary>
		/// <param name="value">
		///		Socket drain timeout.
		/// </param>
		/// <exception cref="InvalidOperationException">You attempt to set the value when this instance is frozen.</exception>
		public void SetDrainTimeout( TimeSpan? value )
		{
			if( this._isFrozen )
			{
				throw new InvalidOperationException( "This instance is frozen." );
			}

			Contract.EndContractBlock();

			this._DrainTimeout = value;
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		public RpcClientOptions()
		{
			// nop.
		}
	}
}