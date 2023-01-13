using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FtpSslLib {
	/// <summary>
	/// FtpSslStream is an SslStream that properly sends a close_notify message when closing
	/// the connection. This is required per RFC 5246 to avoid truncation attacks.
	/// For more information, see https://tools.ietf.org/html/rfc5246#section-7.2.1
	///
	/// Inspired by: https://stackoverflow.com/questions/237807/net-sslstream-doesnt-close-tls-connection-properly/22626756#22626756
	///
	/// See: https://learn.microsoft.com/en-us/windows/win32/secauthn/shutting-down-an-schannel-connection
	/// See: https://learn.microsoft.com/en-us/windows/win32/secauthn/using-sspi-with-a-windows-sockets-client?source=recommendations
	///
	/// Note:
	/// Microsoft says we should not override close():
	/// "Place all cleanup logic for your stream object in Dispose(Boolean). Do not override Close()."
	/// See: https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.dispose?view=net-7.0
	/// But: We recently changed the below logic due to issue #1107, which solved the problem in part
	/// 
	/// </summary>
	public class FtpSslStream : SslStream {

		/// <summary>
		/// Create an SslStream object
		/// </summary>
		public FtpSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback)
			: base(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback) {
		}

#if NETFRAMEWORK
		private bool _Closed = false;
#endif

		/// <summary>
		/// Close
		/// </summary>
		public override void Close() {

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			base.ShutdownAsync()
				.ConfigureAwait(false)
				.GetAwaiter()
				.GetResult();
#elif NETFRAMEWORK
			if (!_Closed) {
				_Closed = true;
				SslDirectCall.CloseNotify(this);
			}
#else
			;
#endif
		}

		/// <summary>
		/// For representing this SslStream in the log
		/// </summary>
		public override string ToString() {
#if NET5_0_OR_GREATER
			return $"{SslProtocol} ({CipherAlgorithm}, {NegotiatedCipherSuite}, {KeyExchangeAlgorithm}, {KeyExchangeStrength})";
#elif NETFRAMEWORK
			return $"{SslProtocol} ({CipherAlgorithm}, {KeyExchangeAlgorithm}, {KeyExchangeStrength})";
#else
			return string.Empty;
#endif
		}
	}

	internal unsafe static class SslDirectCall {
		public static void CloseNotify(SslStream sslStream) {
			if (!sslStream.IsAuthenticated) {
				return;
			}

			byte[] result;
			int resultSize;

			byte[] sChannelShutdown = new byte[4] { 0x01, 0x00, 0x00, 0x00 };

			NativeApi.SSPIHandle securityContextHandle = default(NativeApi.SSPIHandle);
			NativeApi.SSPIHandle credentialsHandleHandle = default(NativeApi.SSPIHandle);

			//If ever needed, here are the necessary changes to work for .NET 5.0+
			//var context = ReflectUtil.GetField(sslStream, "_context");

			//var securityContext = ReflectUtil.GetField(context, "_securityContext");
			//var securityContextHandleOriginal = ReflectUtil.GetField(securityContext, "_handle");

			//securityContextHandle.HandleHi = (IntPtr)ReflectUtil.GetField(securityContextHandleOriginal, "dwLower");
			//securityContextHandle.HandleLo = (IntPtr)ReflectUtil.GetField(securityContextHandleOriginal, "dwUpper");

			//var credentialsHandle = ReflectUtil.GetField(context, "_credentialsHandle");
			//var credentialsHandleHandleOriginal = ReflectUtil.GetField(credentialsHandle, "_handle");

			//credentialsHandleHandle.HandleHi = (IntPtr)ReflectUtil.GetField(credentialsHandleHandleOriginal, "dwLower");
			//credentialsHandleHandle.HandleLo = (IntPtr)ReflectUtil.GetField(credentialsHandleHandleOriginal, "dwUpper");

			// The following is for .NET Framework
			var sslstate = ReflectUtil.GetField(sslStream, "_SslState");
			var context = ReflectUtil.GetProperty(sslstate, "Context");

			var securityContext = ReflectUtil.GetField(context, "m_SecurityContext");
			var securityContextHandleOriginal = ReflectUtil.GetField(securityContext, "_handle");

			securityContextHandle.HandleHi = (IntPtr)ReflectUtil.GetField(securityContextHandleOriginal, "HandleHi");
			securityContextHandle.HandleLo = (IntPtr)ReflectUtil.GetField(securityContextHandleOriginal, "HandleLo");

			var credentialsHandle = ReflectUtil.GetField(context, "m_CredentialsHandle");
			var credentialsHandleHandleOriginal = ReflectUtil.GetField(credentialsHandle, "_handle");

			credentialsHandleHandle.HandleHi = (IntPtr)ReflectUtil.GetField(credentialsHandleHandleOriginal, "HandleHi");
			credentialsHandleHandle.HandleLo = (IntPtr)ReflectUtil.GetField(credentialsHandleHandleOriginal, "HandleLo");

			NativeApi.SecurityBufferDescriptor securityBufferDescriptor = new NativeApi.SecurityBufferDescriptor();
			NativeApi.SecurityBufferStruct[] unmanagedBuffer = new NativeApi.SecurityBufferStruct[1];

			fixed (NativeApi.SecurityBufferStruct* ptr = unmanagedBuffer)

			fixed (void* workArrayPtr = sChannelShutdown) {
				securityBufferDescriptor.UnmanagedPointer = (void*)ptr;

				unmanagedBuffer[0].token = (IntPtr)workArrayPtr;
				unmanagedBuffer[0].count = 4;
				unmanagedBuffer[0].type = 2;

				int status;

				status = NativeApi.ApplyControlToken(
					ref securityContextHandle,
					securityBufferDescriptor);

				if (status != 0) {
					throw new InvalidOperationException(string.Format("ApplyControlToken returned [{0}] during CloseNotify.", status));
				}

				unmanagedBuffer[0].token = IntPtr.Zero;
				unmanagedBuffer[0].count = 0;
				unmanagedBuffer[0].type = 2;

				NativeApi.SSPIHandle contextHandleOut = default(NativeApi.SSPIHandle);

				int inflags = 0x811c;
				int outflags = 0;

				status = NativeApi.InitializeSecurityContextW(
					ref credentialsHandleHandle,
					ref securityContextHandle,
					null,
					inflags,
					0,
					16,
					null,
					0,
					ref contextHandleOut,
					securityBufferDescriptor,
					ref outflags,
					out _);

				if (status != 0) {
					throw new InvalidOperationException(string.Format("InitializeSecurityContextW returned [{0}] during CloseNotify.", status));
				}

				byte[] resultArr = new byte[unmanagedBuffer[0].count];
				Marshal.Copy(unmanagedBuffer[0].token, resultArr, 0, resultArr.Length);
				Marshal.FreeCoTaskMem(unmanagedBuffer[0].token);
				result = resultArr;
				resultSize = resultArr.Length;
			}

			//If ever needed, here are the necessary changes to work for .NET 5.0+
			//var innerStream = (Stream)ReflectUtil.GetProperty(sslStream, "InnerStream");

			// The following is for .NET Framework
			var innerStream = (Stream)ReflectUtil.GetProperty(sslstate, "InnerStream");

			innerStream.Write(result, 0, resultSize);
		}
	}

	internal unsafe static class NativeApi {

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct SSPIHandle {
			public IntPtr HandleHi;
			public IntPtr HandleLo;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal class SecurityBufferDescriptor {
			public readonly int Version;
			public readonly int Count;
			public unsafe void* UnmanagedPointer;

			public SecurityBufferDescriptor() {
				Version = 0;
				Count = 1;
				UnmanagedPointer = null;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SecurityBufferStruct {
			public int count;
			public int type;
			public IntPtr token;
			public static readonly int Size = sizeof(SecurityBufferStruct);
		}

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int ApplyControlToken(ref SSPIHandle contextHandle, [In][Out] SecurityBufferDescriptor outputBuffer);

		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int InitializeSecurityContextW(ref SSPIHandle credentialHandle, ref SSPIHandle contextHandle, [In] byte* targetName, [In] int inFlags, [In] int reservedI, [In] int endianness, [In] SecurityBufferDescriptor inputBuffer, [In] int reservedII, ref SSPIHandle outContextPtr, [In][Out] SecurityBufferDescriptor outputBuffer, [In][Out] ref int attributes, out long timeStamp);
	}

	internal static class ReflectUtil {

		private static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		public static object GetField(object obj, string fieldName) {
			var tp = obj.GetType();
			var info = GetAllFields(tp).Where(f => f.Name == fieldName).Single();
			return info.GetValue(obj);
		}

		public static object GetProperty(object obj, string propertyName) {
			var tp = obj.GetType();
			var info = GetAllProperties(tp).Where(f => f.Name == propertyName).Single();
			return info.GetValue(obj, null);
		}

		private static IEnumerable<FieldInfo> GetAllFields(Type t) {
			if (t == null) {
				return Enumerable.Empty<FieldInfo>();
			}
			return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
		}

		private static IEnumerable<PropertyInfo> GetAllProperties(Type t) {
			if (t == null) {
				return Enumerable.Empty<PropertyInfo>();
			}
			return t.GetProperties(flags).Concat(GetAllProperties(t.BaseType));
		}

	}

}
