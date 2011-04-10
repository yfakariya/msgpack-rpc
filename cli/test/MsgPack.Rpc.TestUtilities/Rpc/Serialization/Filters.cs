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
using System.Text;

namespace MsgPack.Rpc.Serialization
{
	// This file generated from Filters.tt T4Template.
	// Do not modify this file. Edit Filters.tt instead.
	
	/// <summary>
	///		Filter provider for serializing request tracing.
	/// </summary>
	public sealed class SerializingRequestTracingFilterProvider : IFilterProvider< RequestMessageSerializationFilter >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public RequestMessageSerializationFilter GetFilter()
		{
			return new SerializingRequestTracingFilter( this._buffer );
		}
		
		private sealed class SerializingRequestTracingFilter : RequestMessageSerializationFilter
		{
			private readonly StringWriter _writer;

			public SerializingRequestTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override void ProcessCore( RequestMessageSerializationContext context )
				{
				if ( !context.SerializationError.IsSuccess )
				{
					this._writer.WriteLine( "SerializationError: {0}", context.SerializationError );
					var unpacker = new Unpacker( context.Buffer.ReadBytes() );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
				else
				{
					if( context.MessageId != null )
					{
						this._writer.WriteLine( 
							"Type:Request, ID:{0}, Method:'{1}', Argments:'{2}'", 
							context.MessageId, 
							context.MethodName, 
							context.Arguments.Aggregate(
								new StringBuilder( "[" ),
								( buffer, item ) => ( buffer.Length > 1 ? buffer.Append( ", " ) : buffer ).Append( item )
							).Append( "]" )
						);
					}
					else
					{
						this._writer.WriteLine( 
							"Type:Notification, Method:'{0}', Argments:'{1}'", 
							context.MethodName, 
							context.Arguments.Aggregate(
								new StringBuilder( "[" ),
								( buffer, item ) => ( buffer.Length > 1 ? buffer.Append( ", " ) : buffer ).Append( item )
							).Append( "]" )
						);
					}
				}
			}
		}
	}
	
	/// <summary>
	///		Filter provider for serialized request tracing.
	/// </summary>
	public sealed class SerializedRequestTracingFilterProvider : IFilterProvider< SerializedMessageFilter<MessageSerializationContext> >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public SerializedMessageFilter<MessageSerializationContext> GetFilter()
		{
			return new SerializedRequestTracingFilter( this._buffer );
		}
		
		private sealed class SerializedRequestTracingFilter : SerializedMessageFilter<MessageSerializationContext>
		{
			private readonly StringWriter _writer;

			public SerializedRequestTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override IEnumerable<byte> ProcessCore( IEnumerable<byte> binaryMessage, MessageSerializationContext context )
			{
				using ( var buffer = new MemoryStream() )
				{
					bool isHead = true;
					foreach ( var b in binaryMessage )
					{
						if ( isHead )
						{
							isHead = false;
						}
						else
						{
							this._writer.Write( ' ' );
						}

						this._writer.Write( b.ToString( "x2" ) );
						buffer.WriteByte( b );
						yield return b;
					}

					this._writer.WriteLine();

					buffer.Seek( 0L, SeekOrigin.Begin );
					var unpacker = new Unpacker( buffer );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
			} 
		}
	}
	
	/// <summary>
	///		Filter provider for deserializing request tracing.
	/// </summary>
	public sealed class DeserializingRequestTracingFilterProvider : IFilterProvider< SerializedMessageFilter<MessageDeserializationContext> >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public SerializedMessageFilter<MessageDeserializationContext> GetFilter()
		{
			return new DeserializingRequestTracingFilter( this._buffer );
		}
		
		private sealed class DeserializingRequestTracingFilter : SerializedMessageFilter<MessageDeserializationContext>
		{
			private readonly StringWriter _writer;

			public DeserializingRequestTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override IEnumerable<byte> ProcessCore( IEnumerable<byte> binaryMessage, MessageDeserializationContext context )
			{
				using ( var buffer = new MemoryStream() )
				{
					bool isHead = true;
					foreach ( var b in binaryMessage )
					{
						if ( isHead )
						{
							isHead = false;
						}
						else
						{
							this._writer.Write( ' ' );
						}

						this._writer.Write( b.ToString( "x2" ) );
						buffer.WriteByte( b );
						yield return b;
					}

					this._writer.WriteLine();

					buffer.Seek( 0L, SeekOrigin.Begin );
					var unpacker = new Unpacker( buffer );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
			} 
		}
	}
	
	/// <summary>
	///		Filter provider for deserialized request tracing.
	/// </summary>
	public sealed class DeserializedRequestTracingFilterProvider : IFilterProvider< RequestMessageDeserializationFilter >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public RequestMessageDeserializationFilter GetFilter()
		{
			return new DeserializedRequestTracingFilter( this._buffer );
		}
		
		private sealed class DeserializedRequestTracingFilter : RequestMessageDeserializationFilter
		{
			private readonly StringWriter _writer;

			public DeserializedRequestTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override void ProcessCore( RequestMessageDeserializationContext context )
				{
				if ( !context.SerializationError.IsSuccess )
				{
					this._writer.WriteLine( "SerializationError: {0}", context.SerializationError );
					var unpacker = new Unpacker( context.ReadBytes() );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
				else
				{
					if( context.MessageId != null )
					{
						this._writer.WriteLine( 
							"Type:Request, ID:{0}, Method:'{1}', Argments:'{2}'", 
							context.MessageId, 
							context.MethodName, 
							context.Arguments.Aggregate(
								new StringBuilder( "[" ),
								( buffer, item ) => ( buffer.Length > 1 ? buffer.Append( ", " ) : buffer ).Append( item )
							).Append( "]" )
						);
					}
					else
					{
						this._writer.WriteLine( 
							"Type:Notification, Method:'{0}', Argments:'{1}'", 
							context.MethodName, 
							context.Arguments.Aggregate(
								new StringBuilder( "[" ),
								( buffer, item ) => ( buffer.Length > 1 ? buffer.Append( ", " ) : buffer ).Append( item )
							).Append( "]" )
						);
					}
				}
			}
		}
	}
	
	/// <summary>
	///		Filter provider for serializing response tracing.
	/// </summary>
	public sealed class SerializingResponseTracingFilterProvider : IFilterProvider< ResponseMessageSerializationFilter >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public ResponseMessageSerializationFilter GetFilter()
		{
			return new SerializingResponseTracingFilter( this._buffer );
		}
		
		private sealed class SerializingResponseTracingFilter : ResponseMessageSerializationFilter
		{
			private readonly StringWriter _writer;

			public SerializingResponseTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override void ProcessCore( ResponseMessageSerializationContext context )
				{
				if ( !context.SerializationError.IsSuccess )
				{
					this._writer.WriteLine( "SerializationError: {0}", context.SerializationError );
					var unpacker = new Unpacker( context.Buffer.ReadBytes() );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
				else
				{
					if( context.Exception == null )
					{
						if( context.IsVoid )
						{
							this._writer.WriteLine( "ID:{0}, (void)", context.MessageId );
						}
						else
						{
							this._writer.WriteLine( "ID:{0}, Result:{1}", context.MessageId, context.ReturnValue );
						}
					}
					else
					{
						this._writer.WriteLine( "ID:{0}, Error:{1}", context.MessageId, context.Exception );
					}
					}
			}
		}
	}
	
	/// <summary>
	///		Filter provider for serialized response tracing.
	/// </summary>
	public sealed class SerializedResponseTracingFilterProvider : IFilterProvider< SerializedMessageFilter<MessageSerializationContext> >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public SerializedMessageFilter<MessageSerializationContext> GetFilter()
		{
			return new SerializedResponseTracingFilter( this._buffer );
		}
		
		private sealed class SerializedResponseTracingFilter : SerializedMessageFilter<MessageSerializationContext>
		{
			private readonly StringWriter _writer;

			public SerializedResponseTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override IEnumerable<byte> ProcessCore( IEnumerable<byte> binaryMessage, MessageSerializationContext context )
			{
				using ( var buffer = new MemoryStream() )
				{
					bool isHead = true;
					foreach ( var b in binaryMessage )
					{
						if ( isHead )
						{
							isHead = false;
						}
						else
						{
							this._writer.Write( ' ' );
						}

						this._writer.Write( b.ToString( "x2" ) );
						buffer.WriteByte( b );
						yield return b;
					}

					this._writer.WriteLine();

					buffer.Seek( 0L, SeekOrigin.Begin );
					var unpacker = new Unpacker( buffer );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
			} 
		}
	}
	
	/// <summary>
	///		Filter provider for deserializing response tracing.
	/// </summary>
	public sealed class DeserializingResponseTracingFilterProvider : IFilterProvider< SerializedMessageFilter<MessageDeserializationContext> >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public SerializedMessageFilter<MessageDeserializationContext> GetFilter()
		{
			return new DeserializingResponseTracingFilter( this._buffer );
		}
		
		private sealed class DeserializingResponseTracingFilter : SerializedMessageFilter<MessageDeserializationContext>
		{
			private readonly StringWriter _writer;

			public DeserializingResponseTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override IEnumerable<byte> ProcessCore( IEnumerable<byte> binaryMessage, MessageDeserializationContext context )
			{
				using ( var buffer = new MemoryStream() )
				{
					bool isHead = true;
					foreach ( var b in binaryMessage )
					{
						if ( isHead )
						{
							isHead = false;
						}
						else
						{
							this._writer.Write( ' ' );
						}

						this._writer.Write( b.ToString( "x2" ) );
						buffer.WriteByte( b );
						yield return b;
					}

					this._writer.WriteLine();

					buffer.Seek( 0L, SeekOrigin.Begin );
					var unpacker = new Unpacker( buffer );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
			} 
		}
	}
	
	/// <summary>
	///		Filter provider for deserialized response tracing.
	/// </summary>
	public sealed class DeserializedResponseTracingFilterProvider : IFilterProvider< ResponseMessageDeserializationFilter >
	{
		private readonly StringBuilder _buffer = new StringBuilder();

		/// <summary>
		/// 	Get priority of this filter provider.
		/// </summary>
		/// <value>Always 0.</value>
		public int Priority
		{
			get { return 0; }
		}

		/// <summary>
		/// 	Return captured trace message.
		/// </summary>
		/// <returns>Captured trace message.</returns>
		public string GetTrace()
		{
			return this._buffer.ToString();
		}

		/// <summary>
		/// 	Return new filter.
		/// </summary>
		/// <returns>New filter for tracing.</returns>
		public ResponseMessageDeserializationFilter GetFilter()
		{
			return new DeserializedResponseTracingFilter( this._buffer );
		}
		
		private sealed class DeserializedResponseTracingFilter : ResponseMessageDeserializationFilter
		{
			private readonly StringWriter _writer;

			public DeserializedResponseTracingFilter( StringBuilder buffer )
			{
				this._writer = new StringWriter( buffer );
			}

			protected override void ProcessCore( ResponseMessageDeserializationContext context )
				{
				if ( !context.SerializationError.IsSuccess )
				{
					this._writer.WriteLine( "SerializationError: {0}", context.SerializationError );
					var unpacker = new Unpacker( context.ReadBytes() );
					try
					{
						this._writer.WriteLine( "Message:" );
						foreach ( var message in unpacker )
						{
							this._writer.WriteLine( "\t'{0}'", message );
						}
					}
					catch ( Exception ex )
					{
						this._writer.WriteLine( "Exception:'{0}'", ex );
					}
				}
				else
				{
					if( context.Error.IsNil )
					{
						this._writer.WriteLine( "ID:{0}, Result:{1}", context.MessageId, context.DeserializedResult );
					}
					else
					{
						this._writer.WriteLine( "ID:{0}, Error:{1}, ErrorDetail:{2}", context.MessageId, context.Error, context.DeserializedResult );
					}
				}
			}
		}
	}
}
