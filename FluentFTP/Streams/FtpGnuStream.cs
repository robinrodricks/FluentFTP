using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Authentication;

using GnuTlsWrap;

namespace FtpGnuLib {

	/// <summary>
	/// TODO list:
	///
	/// 1. Get Free(ptr) to work
	/// 2. Find out why there is a PULL failure after a (long long) while
	/// 3. Setup session resume for TLS1.3
	/// 
	/// 4. Disconnect FtpGnuLib from FluentFTP and build separate package
	///
	/// a. FEAT: CLNT, then issue CLNT command on logon
	///
	/// </summary>
	/// 
	public class FtpGnuStream : Stream, IDisposable {

		public static string ProtocolName { get; private set; } = "Unknown";
		public static string CipherSuite { get; private set; } = "None";
		public static string? AlpnProtocol { get; private set; } = null;
		public static SslProtocols SslProtocol { get; private set; } = SslProtocols.Tls12;
		public static int MaxRecordSize { get; private set; } = 8192;

		public bool IsResumed { get { return Static.SessionIsResumed(sess) == 1; } }
		public bool IsSessionOk { get; private set; } = false;

		// Logging call back to our user
		public delegate void GnuStreamLogCBFunc(string message);

		// GnuTLS Handshake Hook function
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GnuTlsHandshakeHookFunc(IntPtr session, uint htype, uint post, uint incoming);
		public GnuTlsHandshakeHookFunc handshakeHookFunc = HandshakeHook;

		//

		internal Logging logging;

		private static CertificateCredentials cred = new();

		private static DatumT resumeDataTLS12 = new();

		private ClientSession sess;

		private Socket sock;

		//

		private static int ctorCount = 0;

		//

		public FtpGnuStream(
			Socket socket,                  // The Berkeley socket associated with this stream
			string? alpn,                   // APLN protocol name or empty
			FtpGnuStream streamToResume,    // Stream to session resume or null
			string ciphers,                 // Ciphers supported to offer on handshake
			int handshaketimeout,           // Handshake timeout in ms
			GnuStreamLogCBFunc elog,        // Log callback or null
			int logMaxLevel,                // Max verbosity level of normal log messages
			int logQueueMaxSize) {          // Max size of debugging log buffer

			if (ctorCount < 1) { // == 0 !
				logging = new(elog, logMaxLevel, logQueueMaxSize);

				Logging.Log("GnuTLS " + Static.CheckVersion(null));

				Static.GlobalInit();

				cred = new();
			}

			ctorCount++;

			sock = socket;

			sess = new(InitFlagsT.GNUTLS_NO_TICKETS_TLS12);

			//Static.SessionSetPtr(sess);

			Static.DbSetCacheExpiration(sess, 100000000);

			if (ciphers == string.Empty) {
				Static.SetDefaultPriority(sess);
			}
			else if (ciphers.StartsWith("+") || ciphers.StartsWith("-")) {
				Static.SetDefaultPriority(sess);
				Static.SetDefaultPriorityAppend(sess, ciphers);
			}
			else {
				Static.PrioritySetDirect(sess, ciphers);
			}

			Static.DhSetPrimeBits(sess, 1024);

			Static.CredentialsSet(cred, sess);

			Static.HandshakeSetTimeout(sess, (uint)handshaketimeout);

			// Setup transport functions
			//gnutls_transport_set_push_function(session_, c_push_function);
			//gnutls_transport_set_pull_function(session_, c_pull_function);
			//gnutls_transport_set_ptr(session_, (gnutls_transport_ptr_t)this);
			// or:
			Static.TransportSetInt(sess, (int)sock.Handle);

			// Application Layer Protocol Negotiation (ALPN)
			if (!string.IsNullOrEmpty(alpn)) {
				Static.AlpnSetProtocols(sess, alpn);
			}

			IsSessionOk = true;

			// Session Resume
			if (streamToResume != null) {
				Static.SessionGetData2(streamToResume.sess, ref resumeDataTLS12);

				Logging.LogGnuFunc("Setting up session resume from control connection");
				Static.SessionSetData(sess, resumeDataTLS12);
				Static.Free(resumeDataTLS12.ptr);
			}

			// Disable the Nagle Algorithm
			sock.NoDelay = true;

			// Handshake logging hook
			Static.HandshakeSetHookFunction(sess, (int)HandshakeDescriptionT.GNUTLS_HANDSHAKE_ANY, (int)HandshakeHookT.GNUTLS_HOOK_BOTH, handshakeHookFunc);

			Static.HandShake(sess);

			// Reenable the Nagle Algorithm
			sock.NoDelay = false;

			// TLS1.2, TLS1.3 or what?
			ProtocolName = Static.ProtocolGetName(Static.ProtocolGetVersion(sess));

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
			CipherSuite = Static.SessionGetDesc(sess);

			// ftp ftp-data
			AlpnProtocol = Static.AlpnGetSelectedProtocol(sess);

			// Maximum record size
			MaxRecordSize = Static.RecordGetMaxSize(sess);
			Logging.LogGnuFunc("Maximum record size: " + MaxRecordSize);

			if (IsResumed) {
				Logging.LogGnuFunc("Session resumed from control connection");
			}
		}

		// Destructor

		~FtpGnuStream() {
		}

		public void Dispose() {
			if (sess != null) {
				if (IsSessionOk) {
					int count = Static.RecordCheckPending(sess);
					if (count > 0) {
						byte[] buf = new byte[count];
						int result = this.Read(buf, 0, count);
					}
					Static.Bye(sess, CloseRequestT.GNUTLS_SHUT_RDWR);
				}
				sess.Dispose();
			}

			if (ctorCount <= 1) {
				cred.Dispose();
				Static.GlobalDeInit();
			}

			ctorCount--;
		}

		// Methods overriding base ( = Stream )

		public override int Read(byte[] buffer, int offset, int maxCount) {
			if (maxCount <= 0) {
				throw new ArgumentException("maxCount must be greater than zero");
			}
			if (offset + maxCount > buffer.Length) {
				throw new ArgumentException("offset + maxCount go beyond buffer length");
			}

			maxCount = Math.Min(maxCount, MaxRecordSize);

			int result = Static.gnutls_record_recv(sess.ptr, buffer, maxCount);

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
				result = Static.gnutls_record_send(sess.ptr, buf, Math.Min(buf.Length, MaxRecordSize));
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

		}
	}
}


