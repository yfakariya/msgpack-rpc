using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MsgPack.Rpc.Protocols;
using MsgPack.Rpc.Protocols.Connection;
using MsgPack.Rpc.Services;
using MsgPack.Collections;
using System.Net.Sockets;

namespace MsgPack.Rpc
{
	public static class ClientServices
	{
		internal static readonly bool CanUseIOCompletionPort = DetermineCanUseIOCompletionPortOnCli();

		private static bool DetermineCanUseIOCompletionPortOnCli()
		{
			if ( Environment.OSVersion.Platform != PlatformID.Win32NT )
			{
				return false;
			}

			// TODO: silverlight/moonlight path...
			string windir = Environment.GetEnvironmentVariable( "windir" );
			if ( String.IsNullOrEmpty( windir ) )
			{
				return false;
			}

			string clrMSCorLibPath =
				Path.Combine(
					windir,
					"Microsoft.NET",
					"Framework" + ( IntPtr.Size == 8 ? "64" : String.Empty ),
					"v" + Environment.Version.Major + Environment.Version.Minor + Environment.Version.Build,
					"mscorlib.dll"
				);

			return String.Equals( typeof( object ).Assembly.Location, clrMSCorLibPath, StringComparison.OrdinalIgnoreCase );
		}

		public static ClientEventLoopFactory EventLoopFactory
		{
			get { return CanUseIOCompletionPort ? ( ClientEventLoopFactory )new IOCompletionPortClientEventLoopFactory() : new PollingClientEventLoopFactory(); }
		}

		private static RequestSerializerFactory _requestSerializerFactory = RequestSerializerFactory.Default;

		public static RequestSerializerFactory RequestSerializerFactory
		{
			get { return ClientServices._requestSerializerFactory; }
		}

		private static ResponseDeserializerFactory _responseDeserializerFactory = ResponseDeserializerFactory.Default;

		public static ResponseDeserializerFactory ResponseDeserializerFactory
		{
			get { return ClientServices._responseDeserializerFactory; }
		}

		private static RpcBufferFactory _rpcBufferFactory = new DefaultRpcBufferFactory();

		public static RpcBufferFactory RpcBufferFactory
		{
			get { return ClientServices._rpcBufferFactory; }
		}

		private static Func<Socket, RpcSocket> _socketFactory = socket => new SimpleRpcSocket( socket );

		public static Func<Socket, RpcSocket> SocketFactory
		{
			get { return ClientServices._socketFactory; }
		}

		private static bool _isFrozen;

		public static bool IsFrozen
		{
			get { return ClientServices._isFrozen; }
		}

		public static void Freeze()
		{

		}
	}



	
	


}
