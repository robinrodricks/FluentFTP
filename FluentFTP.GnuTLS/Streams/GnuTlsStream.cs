using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using FluentFTP.GnuTLS.Core;

namespace FluentFTP.GnuTLS {

	/// <summary>
	/// Adds support for GnuTLS TLS1.2 and TLS1.3 (with session resume capability)
	/// for FluentFTP by using a .NET c# wrapper for GnuTLS.
	/// </summary>
	internal class GnuTlsStream : Stream, IDisposable {

		public static string ProtocolName { get; private set; } = "Unknown";
		public static string CipherSuite { get; private set; } = "None";
		public static string? AlpnProtocol { get; private set; } = null;
		public static SslProtocols SslProtocol { get; private set; } = SslProtocols.Tls12;
		public static int MaxRecordSize { get; private set; } = 8192;

		public bool IsResumed { get { return Native.SessionIsResumed(sess) == 1; } }
		public bool IsSessionOk { get; private set; } = false;

		// Logging call back to our user
		public delegate void GnuStreamLogCBFunc(string message);

		// GnuTLS Handshake Hook function
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GnuTlsHandshakeHookFunc(IntPtr session, uint htype, uint post, uint incoming);
		public GnuTlsHandshakeHookFunc handshakeHookFunc = HandshakeHook;

		//

		internal Logging logging;

		private static CertificateCredentials cred;

		private static DatumT resumeDataTLS12 = new();

		private ClientSession sess;

		private Socket sock;

		//

		private static int ctorCount = 0;

		//

		public GnuTlsStream(Socket socket, string? alpn, GnuTlsStream streamToResume, string ciphers, int handshakeTimeout, GnuStreamLogCBFunc elog, int logMaxLevel, int logQueueMaxSize) {

			if (ctorCount < 1) { // == 0 !
				Logging.InitLogging(elog, logMaxLevel, logQueueMaxSize);

				string versionNeeded = "3.7.7";
				string version = Native.CheckVersion(null);

				Logging.Log("GnuTLS " + version);

				if (version != versionNeeded) {
					throw new GnuTlsException("GnuTLS library version must be " + versionNeeded);
				}

				Native.GlobalInit();

				cred = new();
			}

			ctorCount++;

			sock = socket;

			sess = new(InitFlagsT.GNUTLS_NO_TICKETS_TLS12);

			//Native.SessionSetPtr(sess, ????);

			Native.DbSetCacheExpiration(sess, 100000000);

			if (ciphers == string.Empty) {
				Native.SetDefaultPriority(sess);
			}
			else if (ciphers.StartsWith("+") || ciphers.StartsWith("-")) {
				Native.SetDefaultPriorityAppend(sess, ciphers);
			}
			else {
				Native.PrioritySetDirect(sess, ciphers);
			}

			Native.DhSetPrimeBits(sess, 1024);

			Native.CredentialsSet(cred, sess);

			Native.HandshakeSetTimeout(sess, (uint)handshakeTimeout);

			// Setup transport functions
			//gnutls_transport_set_push_function(session_, c_push_function);
			//gnutls_transport_set_pull_function(session_, c_pull_function);
			//gnutls_transport_set_ptr(session_, (gnutls_transport_ptr_t)this);
			// or:
			Native.TransportSetInt(sess, (int)sock.Handle);

			// Application Layer Protocol Negotiation (ALPN)
			if (!string.IsNullOrEmpty(alpn)) {
				Native.AlpnSetProtocols(sess, alpn);
			}

			// Setup handshake hook
			Native.HandshakeSetHookFunction(sess, (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_ANY, (int)HandshakeHookT.GNUTLS_HOOK_BOTH, handshakeHookFunc);

			IsSessionOk = true;

			// Setup Session Resume
			if (streamToResume != null) {
				Native.SessionGetData2(streamToResume.sess, ref resumeDataTLS12);

				Logging.LogGnuFunc("Setting up session resume from control connection");
				Native.SessionSetData(sess, resumeDataTLS12);
				Native.Free(resumeDataTLS12.ptr);
			}

			// Disable the Nagle Algorithm
			sock.NoDelay = true;

			Native.HandShake(sess);

			// Reenable the Nagle Algorithm
			sock.NoDelay = false;

			// TLS1.2, TLS1.3 or what?
			ProtocolName = Native.ProtocolGetName(Native.ProtocolGetVersion(sess));

			if (ProtocolName == "TLS1.2") {
				SslProtocol = SslProtocols.Tls12;
			}
			else if (ProtocolName == "TLS1.3") {
				// Cannot set TLS1.3 on all builds,
				// even if GnuTLS can do it
#if NET5_0_OR_GREATER
				SslProtocol = SslProtocols.Tls13;
#else
				SslProtocol = SslProtocols.None;
#endif
			}
			else {
				SslProtocol = SslProtocols.None;
			}

			// (TLS1.2)-(ECDHE-SECP384R1)-(ECDSA-SHA384)-(AES-256-GCM)
			// (TLS1.3)-(ECDHE-SECP256R1)-(ECDSA-SECP256R1-SHA256)-(AES-256-GCM)
			CipherSuite = Native.SessionGetDesc(sess);

			// ftp ftp-data
			AlpnProtocol = Native.AlpnGetSelectedProtocol(sess);

			// Maximum record size
			MaxRecordSize = Native.RecordGetMaxSize(sess);
			Logging.LogGnuFunc("Maximum record size: " + MaxRecordSize);

			if (IsResumed) {
				Logging.LogGnuFunc("Session resumed from control connection");
			}
		}

		// Destructor

		~GnuTlsStream() {
		}

		public void Dispose() {
			if (sess != null) {
				if (IsSessionOk) {
					int count = Native.RecordCheckPending(sess);
					if (count > 0) {
						byte[] buf = new byte[count];
						this.Read(buf, 0, count);
					}
					Native.Bye(sess, CloseRequestT.GNUTLS_SHUT_RDWR);
				}
				sess.Dispose();
			}

			if (ctorCount <= 1) {
				cred.Dispose();
				Native.GlobalDeInit();
			}

			ctorCount--;
		}

		// Methods overriding base ( = Stream )

		public override int Read(byte[] buffer, int offset, int maxCount) {
			if (maxCount <= 0) {
				throw new ArgumentException("FtpGnuStream.Read: maxCount must be greater than zero");
			}
			if (offset + maxCount > buffer.Length) {
				throw new ArgumentException("FtpGnuStream.Write: offset + maxCount go beyond buffer length");
			}

			maxCount = Math.Min(maxCount, MaxRecordSize);

			int result;
			SessionFlagsT flags;

			do {

					result = Native.gnutls_record_recv(sess.ptr, buffer, maxCount);

				if (result >= (int)EC.en.GNUTLS_E_SUCCESS) { break; }
				Logging.LogGnuFunc("FtpGnuStream.Read repeat due to " + Enum.GetName(typeof(EC.en), result));
				switch (result) {
					case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
						Logging.LogGnuFunc("Warning alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
						break;
					case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
						Logging.LogGnuFunc("Fatal alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
						break;
					default:
						break;
				}
			} while (result == (int)EC.en.GNUTLS_E_AGAIN ||
					 result == (int)EC.en.GNUTLS_E_INTERRUPTED ||
					 result == (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED ||
					 result == (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED);

			Utils.Check("FtpGnuStream.Read", result);

			return result;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (count <= 0) {
				throw new ArgumentException("count must be greater than zero");
			}
			if (offset + count > buffer.Length) {
				throw new ArgumentException("offset + count go beyond buffer length");
			}

			byte[] buf = new byte[count];

			Array.Copy(buffer, offset, buf, 0, count);

			int result = int.MaxValue;

			while (result > 0) {
				do {
				result = Native.gnutls_record_send(sess.ptr, buf, Math.Min(buf.Length, MaxRecordSize));
					if (result >= (int)EC.en.GNUTLS_E_SUCCESS) { break; }
					Logging.LogGnuFunc("FtpGnuStream.Write repeat due to " + Enum.GetName(typeof(EC.en), result));
					switch (result) {
						case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
							Logging.LogGnuFunc("Warning alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
							break;
						case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
							Logging.LogGnuFunc("Fatal alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
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
				Utils.Check("FtpGnuStream.Write", result);
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
			// Do we need this?
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		// 

		public static void HandshakeHook(IntPtr session, uint htype, uint post, uint incoming) {

			if (session == null) {
				return;
			}

			string prefix;

			if (incoming != 0) {
				if (post != 0) {
					prefix = "processed";
				}
				else {
					prefix = "received";
				}
			}
			else {
				if (post != 0) {
					prefix = "sent";
				}
				else {
					prefix = "about to send";
				}
			}

			Logging.LogGnuFunc("Handshake " + prefix + " " + Enum.GetName(typeof(HandshakeDescriptionT), htype));

			if (prefix == "processed") { 
				if (htype == (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_NEW_SESSION_TICKET) {
					SessionFlagsT flags = Native.SessionGetFlags(session);
					if (flags.HasFlag(SessionFlagsT.GNUTLS_SFLAGS_SESSION_TICKET)) {
						Native.SessionGetData2(session, ref resumeDataTLS12);
						Logging.LogGnuFunc("Retrieving session data with session key");
						Native.SessionSetData(session, resumeDataTLS12);
						Native.Free(resumeDataTLS12.ptr);
					}

				}
			}

		}
	}
}
