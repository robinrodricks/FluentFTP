using System;
using System.Collections.Generic;
using System.IO;
#if !CORE
using System.Linq;
using System.Net.Security;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

#endif

namespace FluentFTP {
#if !CORE
	/// <summary>
	/// .NET SslStream doesn't close TLS connection properly.
	/// It does not send the close_notify alert before closing the connection.
	/// FtpSslStream uses unsafe code to do that.
	/// This is required when we want to downgrade the connection to plaintext using CCC command.
	/// Thanks to Neco @ https://stackoverflow.com/questions/237807/net-sslstream-doesnt-close-tls-connection-properly/22626756#22626756
	/// </summary>
	internal class FtpSslStream : SslStream {
		private bool sentCloseNotify = false;

		public FtpSslStream(Stream innerStream)
			: base(innerStream) {
		}

		public FtpSslStream(Stream innerStream, bool leaveInnerStreamOpen)
			: base(innerStream, leaveInnerStreamOpen) {
		}

		public FtpSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback)
			: base(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback) {
		}

		public FtpSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback)
			: base(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback) {
		}

#if !NET20 && !NET35
		public FtpSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback, EncryptionPolicy encryptionPolicy)
			: base(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback, encryptionPolicy) {
		}

#endif
		public override void Close() {
			try {
				if (!sentCloseNotify) {
					SslDirectCall.CloseNotify(this);
					sentCloseNotify = true;
				}
			}
			finally {
				base.Close();
			}
		}
	}

	internal static unsafe class SslDirectCall {
		/// <summary>
		/// Send an SSL close_notify alert.
		/// </summary>
		/// <param name="sslStream"></param>
		public static void CloseNotify(SslStream sslStream) {
			if (sslStream.IsAuthenticated && sslStream.CanWrite) {
				var isServer = sslStream.IsServer;

				byte[] result;
				int resultSz;
				var asmbSystem = typeof(System.Net.Authorization).Assembly;

				var SCHANNEL_SHUTDOWN = 1;
				var workArray = BitConverter.GetBytes(SCHANNEL_SHUTDOWN);

				var sslstate = FtpReflection.GetField(sslStream, "_SslState");
				var context = FtpReflection.GetProperty(sslstate, "Context");

				var securityContext = FtpReflection.GetField(context, "m_SecurityContext");
				var securityContextHandleOriginal = FtpReflection.GetField(securityContext, "_handle");
				var securityContextHandle = default(SslNativeApi.SSPIHandle);
				securityContextHandle.HandleHi = (IntPtr) FtpReflection.GetField(securityContextHandleOriginal, "HandleHi");
				securityContextHandle.HandleLo = (IntPtr) FtpReflection.GetField(securityContextHandleOriginal, "HandleLo");

				var credentialsHandle = FtpReflection.GetField(context, "m_CredentialsHandle");
				var credentialsHandleHandleOriginal = FtpReflection.GetField(credentialsHandle, "_handle");
				var credentialsHandleHandle = default(SslNativeApi.SSPIHandle);
				credentialsHandleHandle.HandleHi = (IntPtr) FtpReflection.GetField(credentialsHandleHandleOriginal, "HandleHi");
				credentialsHandleHandle.HandleLo = (IntPtr) FtpReflection.GetField(credentialsHandleHandleOriginal, "HandleLo");

				var bufferSize = 1;
				var securityBufferDescriptor = new SslNativeApi.SecurityBufferDescriptor(bufferSize);
				var unmanagedBuffer = new SslNativeApi.SecurityBufferStruct[bufferSize];

				fixed (SslNativeApi.SecurityBufferStruct* ptr = unmanagedBuffer)
				fixed (void* workArrayPtr = workArray) {
					securityBufferDescriptor.UnmanagedPointer = (void*) ptr;

					unmanagedBuffer[0].token = (IntPtr) workArrayPtr;
					unmanagedBuffer[0].count = workArray.Length;
					unmanagedBuffer[0].type = SslNativeApi.BufferType.Token;

					SslNativeApi.SecurityStatus status;
					status = (SslNativeApi.SecurityStatus) SslNativeApi.ApplyControlToken(ref securityContextHandle, securityBufferDescriptor);
					if (status == SslNativeApi.SecurityStatus.OK) {
						unmanagedBuffer[0].token = IntPtr.Zero;
						unmanagedBuffer[0].count = 0;
						unmanagedBuffer[0].type = SslNativeApi.BufferType.Token;

						var contextHandleOut = default(SslNativeApi.SSPIHandle);
						var outflags = SslNativeApi.ContextFlags.Zero;
						long ts = 0;

						var inflags = SslNativeApi.ContextFlags.SequenceDetect |
						              SslNativeApi.ContextFlags.ReplayDetect |
						              SslNativeApi.ContextFlags.Confidentiality |
						              SslNativeApi.ContextFlags.AcceptExtendedError |
						              SslNativeApi.ContextFlags.AllocateMemory |
						              SslNativeApi.ContextFlags.InitStream;

						if (isServer) {
							status = (SslNativeApi.SecurityStatus) SslNativeApi.AcceptSecurityContext(ref credentialsHandleHandle, ref securityContextHandle, null,
								inflags, SslNativeApi.Endianness.Native, ref contextHandleOut, securityBufferDescriptor, ref outflags, out ts);
						}
						else {
							status = (SslNativeApi.SecurityStatus) SslNativeApi.InitializeSecurityContextW(ref credentialsHandleHandle, ref securityContextHandle, null,
								inflags, 0, SslNativeApi.Endianness.Native, null, 0, ref contextHandleOut, securityBufferDescriptor, ref outflags, out ts);
						}

						if (status == SslNativeApi.SecurityStatus.OK) {
							var resultArr = new byte[unmanagedBuffer[0].count];
							Marshal.Copy(unmanagedBuffer[0].token, resultArr, 0, resultArr.Length);
							Marshal.FreeCoTaskMem(unmanagedBuffer[0].token);
							result = resultArr;
							resultSz = resultArr.Length;
						}
						else {
							throw new InvalidOperationException(string.Format("AcceptSecurityContext/InitializeSecurityContextW returned [{0}] during CloseNotify.", status));
						}
					}
					else {
						throw new InvalidOperationException(string.Format("ApplyControlToken returned [{0}] during CloseNotify.", status));
					}
				}

				var innerStream = (Stream)FtpReflection.GetProperty(sslstate, "InnerStream");
				innerStream.Write(result, 0, resultSz);
			}
		}
	}

	internal static unsafe class SslNativeApi {
		internal enum BufferType {
			Empty,
			Data,
			Token,
			Parameters,
			Missing,
			Extra,
			Trailer,
			Header,
			Padding = 9,
			Stream,
			ChannelBindings = 14,
			TargetHost = 16,
			ReadOnlyFlag = -2147483648,
			ReadOnlyWithChecksum = 268435456
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct SSPIHandle {
			public IntPtr HandleHi;
			public IntPtr HandleLo;
			public bool IsZero => HandleHi == IntPtr.Zero && HandleLo == IntPtr.Zero;

			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			internal void SetToInvalid() {
				HandleHi = IntPtr.Zero;
				HandleLo = IntPtr.Zero;
			}

			public override string ToString() {
				return HandleHi.ToString("x") + ":" + HandleLo.ToString("x");
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal class SecurityBufferDescriptor {
			public readonly int Version;
			public readonly int Count;
			public unsafe void* UnmanagedPointer;

			public SecurityBufferDescriptor(int count) {
				Version = 0;
				Count = count;
				UnmanagedPointer = null;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SecurityBufferStruct {
			public int count;
			public BufferType type;
			public IntPtr token;
			public static readonly int Size = sizeof(SecurityBufferStruct);
		}

		internal enum SecurityStatus {
			OK,
			ContinueNeeded = 590610,
			CompleteNeeded,
			CompAndContinue,
			ContextExpired = 590615,
			CredentialsNeeded = 590624,
			Renegotiate,
			OutOfMemory = -2146893056,
			InvalidHandle,
			Unsupported,
			TargetUnknown,
			InternalError,
			PackageNotFound,
			NotOwner,
			CannotInstall,
			InvalidToken,
			CannotPack,
			QopNotSupported,
			NoImpersonation,
			LogonDenied,
			UnknownCredentials,
			NoCredentials,
			MessageAltered,
			OutOfSequence,
			NoAuthenticatingAuthority,
			IncompleteMessage = -2146893032,
			IncompleteCredentials = -2146893024,
			BufferNotEnough,
			WrongPrincipal,
			TimeSkew = -2146893020,
			UntrustedRoot,
			IllegalMessage,
			CertUnknown,
			CertExpired,
			AlgorithmMismatch = -2146893007,
			SecurityQosFailed,
			SmartcardLogonRequired = -2146892994,
			UnsupportedPreauth = -2146892989,
			BadBinding = -2146892986
		}

		[Flags]
		internal enum ContextFlags {
			Zero = 0,
			Delegate = 1,
			MutualAuth = 2,
			ReplayDetect = 4,
			SequenceDetect = 8,
			Confidentiality = 16,
			UseSessionKey = 32,
			AllocateMemory = 256,
			Connection = 2048,
			InitExtendedError = 16384,
			AcceptExtendedError = 32768,
			InitStream = 32768,
			AcceptStream = 65536,
			InitIntegrity = 65536,
			AcceptIntegrity = 131072,
			InitManualCredValidation = 524288,
			InitUseSuppliedCreds = 128,
			InitIdentify = 131072,
			AcceptIdentify = 524288,
			ProxyBindings = 67108864,
			AllowMissingBindings = 268435456,
			UnverifiedTargetName = 536870912
		}

		internal enum Endianness {
			Network,
			Native = 16
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int ApplyControlToken(ref SSPIHandle contextHandle, [In] [Out] SecurityBufferDescriptor outputBuffer);

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern unsafe int AcceptSecurityContext(ref SSPIHandle credentialHandle, ref SSPIHandle contextHandle, [In] SecurityBufferDescriptor inputBuffer, [In] ContextFlags inFlags, [In] Endianness endianness, ref SSPIHandle outContextPtr, [In] [Out] SecurityBufferDescriptor outputBuffer, [In] [Out] ref ContextFlags attributes, out long timeStamp);

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[DllImport("secur32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern unsafe int InitializeSecurityContextW(ref SSPIHandle credentialHandle, ref SSPIHandle contextHandle, [In] byte* targetName, [In] ContextFlags inFlags, [In] int reservedI, [In] Endianness endianness, [In] SecurityBufferDescriptor inputBuffer, [In] int reservedII, ref SSPIHandle outContextPtr, [In] [Out] SecurityBufferDescriptor outputBuffer, [In] [Out] ref ContextFlags attributes, out long timeStamp);
	}

#endif
}