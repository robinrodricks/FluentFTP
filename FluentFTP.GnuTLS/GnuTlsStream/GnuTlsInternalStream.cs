using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using FluentFTP.GnuTLS.Core;
using FluentFTP.GnuTLS.Enums;

namespace FluentFTP.GnuTLS {

	/// <summary>
	/// Adds support for GnuTLS TLS1.2 and TLS1.3 (with session resume capability)
	/// for FluentFTP by using a .NET c# wrapper for GnuTLS.
	/// </summary>
	internal partial class GnuTlsInternalStream : Stream, IDisposable {

		// After a successful handshake, the following will be available:
		public static string ProtocolName { get; private set; } = "Unknown";
		public static string CipherSuite { get; private set; } = "None";
		public static string? AlpnProtocol { get; private set; } = null;
		public static SslProtocols SslProtocol { get; private set; } = SslProtocols.None;
		public static int MaxRecordSize { get; private set; } = 8192;

		public bool IsResumed { get; private set; } = false;
		public bool IsSessionOk { get; private set; } = false;

		// Logging call back to our user.
		public delegate void GnuStreamLogCBFunc(string message);

		//
		// These are brought in by the .ctor
		//

		// The underlying socket of the connection
		private Socket socket;

		// The desired ALPN string to be used in the handshake
		private string alpn;

		// The desired Priority string to be used in the handshake
		private string priority;

		// The expected Host name for certificate verification
		private string hostname;

		// The Handshake Timeout to be honored on handshake
		private int timeout;

		//
		// For our own inside use
		//

		internal Logging logging;

		// GnuTLS Handshake Hook function
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void GnuTlsHandshakeHookFunc(IntPtr session, uint htype, uint post, uint incoming);
		internal GnuTlsHandshakeHookFunc handshakeHookFunc = HandshakeHook;

		// Keep track: Is this the first instance or a subsequent one?
		// We need to do a "GLobal Init" and a "Global DeInit" when the first
		// instance is born or dies.
		private static int ctorCount = 0;

		//

		// The TLS session associated with this GnuTlsStream
		private ClientSession sess;

		// The Certificate Credentials associated with this
		// GnuTlsStream and ALL streams resumed from it
		// One for all of these, therefore static
		private static CertificateCredentials cred;

		// Storage for resume data:
		// * retrieved from the "session-to-be-resumed"
		// * used for a session that is "to-be-resumed"
		// * re-used, therefore static
		private static DatumT resumeDataTLS = new();

		//
		// Constructor
		//

		public GnuTlsInternalStream(
			string targetHostString,
			Socket socketDescriptor,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			string? alpnString,
			GnuTlsInternalStream streamToResume,
			string priorityString,
			int handshakeTimeout,
			GnuStreamLogCBFunc elog,
			int logMaxLevel,
			GnuMessage logDebugInformationMessages, 
			int logQueueMaxSize) {

			socket = socketDescriptor;
			alpn = alpnString;
			priority = priorityString;
			hostname = targetHostString;
			timeout = handshakeTimeout;

			if (ctorCount < 1) {

				// On the first instance of GnuTlsStream, setup:
				// 1. Logging
				// 2. Make sure GnuTls version corresponds to our Native. and Enums.
				// 3. GnuTls Gobal Init
				// 4. One single credentials set

				Logging.InitLogging(elog, logMaxLevel, logDebugInformationMessages, logQueueMaxSize);

				int bitsNeeded = 64;
				int bits = IntPtr.Size * 8;
				string versionNeeded = "3.7.7";
				string version = Core.GnuTls.CheckVersion(null);

				Logging.Log("GnuTLS " + version + " (x" + bits + ")");

				if (bits != bitsNeeded) {
					throw new GnuTlsException("GnuTlsStream needs to run as 64bit process");
				}
				if (version != versionNeeded) {
					throw new GnuTlsException("GnuTLS library version must be " + versionNeeded);
				}

				// GnuTlsStreams are organized as
				// TLS 1.2:
				// First one: Creates/Initializes the GnuTls infrastructure, cannot resume
				// Subsequent ones: Re-use part of the GnuTls infrastructure, can resume from first
				// one or previous ones
				// TLS 1.3:
				// Additionally, Session Tickets to store session data may appear at any time
				if (streamToResume != null) {
					throw new GnuTlsException("Cannot resume from anything if fresh stream");
				}

				// Setup the GnuTLS infrastructure
				Core.GnuTls.GlobalInit();

				// Setup/Allocate certificate credentials for this first session
				cred = new();

			}

			// Further code runs on first and all subsequent instantiations of
			// GnuTlsStream - for FTP, typically there is one control connection
			// as the first instance, and one further instance that is born and then dies
			// multiple times as a data connection.

			ctorCount++;

			sess = new(/*InitFlagsT.GNUTLS_NO_TICKETS_TLS12*/);

			SetupHandshake();

			// Setup handshake hook
			Core.GnuTls.HandshakeSetHookFunction(sess, (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_ANY, (int)HandshakeHookT.GNUTLS_HOOK_BOTH, handshakeHookFunc);

			IsSessionOk = true;

			// Setup Session Resume
			if (streamToResume != null) {
				GCHandle gcHandle = GCHandle.Alloc(resumeDataTLS, GCHandleType.Pinned);

				Core.GnuTls.SessionGetData2(streamToResume.sess, out resumeDataTLS);

				Logging.LogGnuFunc(GnuMessage.Handshake, "Setting up session resume from control connection");
				Core.GnuTls.SessionSetData(sess, resumeDataTLS);
				//GnuTls.Free(resumeDataTLS.ptr);

				gcHandle.Free();
			}

			DisableNagle();

			Core.GnuTls.HandShake(sess);

			ReEnableNagle();

			PopulateHandshakeInfo();

			ReportClientCertificateUsed();

			ValidateServerCertificates(customRemoteCertificateValidation);

		}

		// Destructor

		~GnuTlsInternalStream() {
		}

		// Dispose

		public void Dispose() {
			if (sess != null) {
				if (IsSessionOk) {
					int count = Core.GnuTls.RecordCheckPending(sess);
					if (count > 0) {
						byte[] buf = new byte[count];
						this.Read(buf, 0, count);
					}
					Core.GnuTls.Bye(sess, CloseRequestT.GNUTLS_SHUT_RDWR);
				}
				sess.Dispose();
			}

			if (ctorCount <= 1) {
				cred.Dispose();
				Core.GnuTls.GlobalDeInit();
			}

			ctorCount--;
		}

		// Methods overriding base ( = Stream )

		public override int Read(byte[] buffer, int offset, int maxCount) {
			if (maxCount <= 0) {
				throw new ArgumentException("FtpGnuStream.Read: maxCount must be greater than zero");
			}
			if (offset + maxCount > buffer.Length) {
				throw new ArgumentException("FtpGnuStream.Read: offset + maxCount go beyond buffer length");
			}

			maxCount = Math.Min(maxCount, MaxRecordSize);

			int result;

			do {
				result = Core.GnuTls.gnutls_record_recv(sess.ptr, buffer, maxCount);

				if (result >= (int)EC.en.GNUTLS_E_SUCCESS) {
					break;
				}
				Logging.LogGnuFunc(GnuMessage.Read, "FtpGnuStream.Read repeat due to " + Enum.GetName(typeof(EC.en), result));
				switch (result) {
					case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
						Logging.LogGnuFunc(GnuMessage.Alert, "Warning alert received: " + Core.GnuTls.AlertGetName(Core.GnuTls.AlertGet(sess)));
						break;
					case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
						Logging.LogGnuFunc(GnuMessage.Alert, "Fatal alert received: " + Core.GnuTls.AlertGetName(Core.GnuTls.AlertGet(sess)));
						break;
					default:
						break;
				}
			} while (result == (int)EC.en.GNUTLS_E_AGAIN ||
					 result == (int)EC.en.GNUTLS_E_INTERRUPTED ||
					 result == (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED ||
					 result == (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED);

			GnuUtils.Check("FtpGnuStream.Read", result);

			return result;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (count <= 0) {
				throw new ArgumentException("FtpGnuStream.Write: count must be greater than zero");
			}
			if (offset + count > buffer.Length) {
				throw new ArgumentException("FtpGnuStream.Write: offset + count go beyond buffer length");
			}

			byte[] buf = new byte[count];

			Array.Copy(buffer, offset, buf, 0, count);

			int result = int.MaxValue;

			while (result > 0) {
				do {
					result = Core.GnuTls.gnutls_record_send(sess.ptr, buf, Math.Min(buf.Length, MaxRecordSize));
					if (result >= (int)EC.en.GNUTLS_E_SUCCESS) {
						break;
					}
					Logging.LogGnuFunc(GnuMessage.Write, "FtpGnuStream.Write repeat due to " + Enum.GetName(typeof(EC.en), result));
					switch (result) {
						case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
							Logging.LogGnuFunc(GnuMessage.Alert, "Warning alert received: " + Core.GnuTls.AlertGetName(Core.GnuTls.AlertGet(sess)));
							break;
						case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
							Logging.LogGnuFunc(GnuMessage.Alert, "Fatal alert received: " + Core.GnuTls.AlertGetName(Core.GnuTls.AlertGet(sess)));
							break;
						default:
							break;
					}
				} while (result == (int)EC.en.GNUTLS_E_AGAIN ||
						 result == (int)EC.en.GNUTLS_E_INTERRUPTED ||
						 result == (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED ||
						 result == (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED);

				int newLength = buf.Length - result;
				if (newLength <= 0) {
					break;
				}
				Array.Copy(buf, result, buf, 0, newLength);
				Array.Resize(ref buf, buf.Length - result);
			}

			if (result < 0) {
				GnuUtils.Check("FtpGnuStream.Write", result);
			}
		}

		public override bool CanRead {
			get {
				return IsSessionOk;
			}
		}

		public override bool CanWrite {
			get {
				return IsSessionOk;
			}
		}

		public override bool CanSeek { get { return false; } }

		public override long Length => throw new NotImplementedException();
		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void Flush() {
			// Do we need to do anything here? This is actually invoked.
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

	}
}
