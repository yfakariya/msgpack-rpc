﻿#region -- License Terms --
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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace MsgPack.Rpc
{
	partial class RpcException
	{

		/// <summary>
		///		Returns string representation of this exception for debugging.
		/// </summary>
		/// <returns>
		///		String representation of this exception for debugging.
		///		Note that this value contains debug information, so you SHOULD NOT transfer to remote site.
		/// </returns>
		/// <remarks>
		///		This method is equivelant to call <see cref="ToString(Boolean)"/> with true.
		/// </remarks>
		public override string ToString()
		{
			// UnSafe to-string.
			return this.ToString( true );
		}

		/// <summary>
		///		Returns string representation of this exception.
		///		specofying incluses debugging information.
		/// </summary>
		/// <param name="includesDebugInformation">
		///		If you want to include debugging information then true.
		/// </param>
		/// <returns>
		///		String representation of this exception.
		/// </returns>
		public string ToString( bool includesDebugInformation )
		{
			if ( !includesDebugInformation || this._remoteExceptions == null )
			{
				return base.ToString();
			}
			else
			{
				Contract.Assert( this._remoteExceptions != null );

				// <Type>: <Message> ---> <InnerType1>: <InnerMessage1> ---> <InnerType2>: <InnerMessage2> ---> ...
				// <ServerInnerStackTraceN>
				//    --- End of inner exception stack trace ---
				// <ServerInnerStackTrace1>
				//    --- End of inner exception stack trace ---
				// 
				// Server statck trace:
				// <ServerStackTrace>
				// 
				// Exception rethrown at[N]:
				// <ClientInnerStackTraceN>
				//    --- End of inner exception stack trace ---
				// <ClientInnerStackTrace1>
				//    --- End of inner exception stack trace ---
				// <StackTrace>
				StringBuilder stringBuilder = new StringBuilder();
				// Build <Type>: <Message> chain
				this.BuildExceptionMessage( stringBuilder );
				// Build stacktrace chain.
				this.BuildExceptionStackTrace( stringBuilder );

				return stringBuilder.ToString();
			}
		}

		/// <summary>
		///		Build exception message to specified buffer.
		/// </summary>
		/// <param name="stringBuilder">Buffer.</param>
		private void BuildExceptionMessage( StringBuilder stringBuilder )
		{
			stringBuilder.Append( this.GetType().FullName ).Append( ": " ).Append( this.Message );

			if ( this.InnerException != null )
			{
				Contract.Assert( this._remoteExceptions == null );

				for ( var inner = this.InnerException; inner != null; inner = inner.InnerException )
				{
					var asRpcException = inner as RpcException;
					if ( asRpcException != null )
					{
						asRpcException.BuildExceptionMessage( stringBuilder );
					}
					else
					{
						stringBuilder.Append( " ---> " ).Append( inner.GetType().FullName ).Append( ": " ).Append( inner.Message );
					}
				}
			}
			else if ( this._remoteExceptions != null )
			{
				foreach ( var remoteException in this._remoteExceptions )
				{
					stringBuilder.Append( " ---> " ).Append( remoteException.TypeName ).Append( ": " ).Append( remoteException.Message );
				}
			}
		}

		/// <summary>
		///		Build stack trace string to specified buffer.
		/// </summary>
		/// <param name="stringBuilder">Buffer.</param>
		private void BuildExceptionStackTrace( StringBuilder stringBuilder )
		{
			if ( this.InnerException != null )
			{
				Contract.Assert( this._remoteExceptions == null );

				for ( var inner = this.InnerException; inner != null; inner = inner.InnerException )
				{
					var asRpcException = inner as RpcException;
					if ( asRpcException != null )
					{
						asRpcException.BuildExceptionStackTrace( stringBuilder );
					}
					else
					{
						BuildGeneralStackTrace( inner, stringBuilder );
					}

					stringBuilder.Append( "   --- End of inner exception stack trace ---" );
				}
			}
			else if ( this._remoteExceptions != null && this._remoteExceptions.Length > 0 )
			{
				for ( int i = 0; i < this._remoteExceptions.Length; i++ )
				{
					if ( i > 0
						&& this._remoteExceptions[ i ].Hop != this._remoteExceptions[ i - 1 ].Hop
						&& this._remoteExceptions[ i ].TypeName == this._remoteExceptions[ i - 1 ].TypeName
					)
					{
						// Serialized -> Deserialized case
						stringBuilder.AppendFormat( "Exception transferred at[{0}]:", this._remoteExceptions[ i - 1 ].Hop );
					}
					else
					{
						// Inner exception case
						stringBuilder.Append( "   --- End of inner exception stack trace ---" );
					}

					foreach ( var frame in this._remoteExceptions[ i ].StackTrace )
					{
						WriteStackFrame( frame, stringBuilder );
					}
				}

				stringBuilder.AppendFormat( "Exception transferred at[{0}]:", this._remoteExceptions[ this._remoteExceptions.Length - 1 ].Hop ).AppendLine();
			}

			BuildGeneralStackTrace( this, stringBuilder );
		}

		/// <summary>
		///		Build general statck trace string of specified exception to buffer.
		/// </summary>
		/// <param name="target">Exception which is source of stack trace.</param>
		/// <param name="stringBuilder">Buffer.</param>
		private static void BuildGeneralStackTrace( Exception target, StringBuilder stringBuilder )
		{
			var stackTrace = new StackTrace( target, true );
			for ( int i = 0; i < stackTrace.FrameCount; i++ )
			{
				WriteStackFrame( stackTrace.GetFrame( stackTrace.FrameCount - ( i + 1 ) ), stringBuilder );
			}
		}

		/// <summary>
		///		Write local stack framew string to specified buffer.
		/// </summary>
		/// <param name="frame">Stack frame to write.</param>
		/// <param name="stringBuilder">Buffer.</param>
		private static void WriteStackFrame( StackFrame frame, StringBuilder stringBuilder )
		{
			const string at = "at";
			stringBuilder.AppendFormat( "   " + at + "{0}", frame.GetMethod() );
			if ( frame.GetFileName() != null )
			{
				stringBuilder.AppendFormat( " " + at + " {0}", frame.GetFileName() );

				if ( frame.GetFileLineNumber() > 0 )
				{
					stringBuilder.AppendFormat( ":line {0}", frame.GetFileLineNumber() );
				}
			}
		}

		/// <summary>
		///		Write remote stack framew string to specified buffer.
		/// </summary>
		/// <param name="frame">Stack frame to write.</param>
		/// <param name="stringBuilder">Buffer.</param>
		private static void WriteStackFrame( RemoteStackFrame frame, StringBuilder stringBuilder )
		{
			const string at = "at";
			stringBuilder.AppendFormat( "   " + at + "{0}", frame.MethodSignature );
			if ( frame.FileName != null )
			{
				stringBuilder.AppendFormat( " " + at + " {0}", frame.FileName );

				if ( frame.FileLineNumber > 0 )
				{
					stringBuilder.AppendFormat( ":line {0}", frame.FileLineNumber );
				}
			}
		}
	}
}
