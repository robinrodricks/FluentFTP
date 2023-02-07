using FluentFTP.GnuTLS.Core;

using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FluentFTP.GnuTLS {

	/// <summary>
	/// Adds support for GnuTLS TLS1.2 and TLS1.3 (with session resume capability)
	/// for FluentFTP by using a .NET c# wrapper for GnuTLS.
	/// </summary>
	internal class GnuTlsStream : Stream, IDisposable {

		//
		// For our creator
		//

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

		public GnuTlsStream(
			string targetHostString,
			Socket socketDescriptor,
			CustomRemoteCertificateValidationCallback customRemoteCertificateValidation,
			string? alpnString,
			GnuTlsStream streamToResume,
			string priorityString,
			int handshakeTimeout,
			GnuStreamLogCBFunc elog,
			int logMaxLevel,
			LogDebugInformationMessagesT logDebugInformationMessages, 
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

				string versionNeeded = "3.7.7";
				string version = Native.CheckVersion(null);

				Logging.Log("GnuTLS " + version);

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
				Native.GlobalInit();

				// Setup/Allocate certificate credentials for this first session
				cred = new();

			}

			// Further code runs on first and all subsequent instantiations of
			// GnuTlsStream - for FTP, typically there is one control connections
			// as the first instance, and one further instance that is born and then dies
			// multiple times as a data connection.

			ctorCount++;

			sess = new(/*InitFlagsT.GNUTLS_NO_TICKETS_TLS12*/);

			SetupHandshake();

			// Setup handshake hook
			Native.HandshakeSetHookFunction(sess, (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_ANY, (int)HandshakeHookT.GNUTLS_HOOK_BOTH, handshakeHookFunc);

			IsSessionOk = true;

			// Setup Session Resume
			if (streamToResume != null) {
				Native.SessionGetData2(streamToResume.sess, ref resumeDataTLS);

				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Setting up session resume from control connection");
				Native.SessionSetData(sess, resumeDataTLS);
				//Native.GnuFree(resumeDataTLS.ptr);
			}

			DisableNagle();

			Native.HandShake(sess);

			ReEnableNagle();

			PopulateHandshakeInfo();

			ReportClientCertificateUsed();

			ValidateServerCertificates(customRemoteCertificateValidation);

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
				throw new ArgumentException("FtpGnuStream.Read: offset + maxCount go beyond buffer length");
			}

			maxCount = Math.Min(maxCount, MaxRecordSize);

			int result;

			do {
				result = Native.gnutls_record_recv(sess.ptr, buffer, maxCount);

				if (result >= (int)EC.en.GNUTLS_E_SUCCESS) {
					break;
				}
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Read, "FtpGnuStream.Read repeat due to " + Enum.GetName(typeof(EC.en), result));
				switch (result) {
					case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
						Logging.LogGnuFunc(LogDebugInformationMessagesT.Alert, "Warning alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
						break;
					case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
						Logging.LogGnuFunc(LogDebugInformationMessagesT.Alert, "Fatal alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
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
					result = Native.gnutls_record_send(sess.ptr, buf, Math.Min(buf.Length, MaxRecordSize));
					if (result >= (int)EC.en.GNUTLS_E_SUCCESS) {
						break;
					}
					Logging.LogGnuFunc(LogDebugInformationMessagesT.Write, "FtpGnuStream.Write repeat due to " + Enum.GetName(typeof(EC.en), result));
					switch (result) {
						case (int)EC.en.GNUTLS_E_WARNING_ALERT_RECEIVED:
							Logging.LogGnuFunc(LogDebugInformationMessagesT.Alert, "Warning alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
							break;
						case (int)EC.en.GNUTLS_E_FATAL_ALERT_RECEIVED:
							Logging.LogGnuFunc(LogDebugInformationMessagesT.Alert, "Fatal alert received: " + Native.AlertGetName(Native.AlertGet(sess)));
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
			// Do we need to do anything here? This is actually invoked.
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotImplementedException();
		}

		public override void SetLength(long value) {
			throw new NotImplementedException();
		}

		// 

		internal static void HandshakeHook(IntPtr session, uint description, uint post, uint incoming) {

			if (session == null) {
				return;
			}

			string action;

			// incoming  post
			// ==============
			//    1       0    received
			//    1       1    processed
			//
			//    0       0    about to send
			//    0       1    sent
			//

			if (incoming == 0) {
				// send
				action = post == 0 ? "about to send" : "sent";
			}
			else {
				// receive
				action = post == 0 ? "received" : "processed";
			}

			Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Handshake " + action + " " + Enum.GetName(typeof(HandshakeDescriptionT), description));

			// Check for certain action/description combinations

			if (incoming != 0 && post != 0) { // receive processed") 

				//
				// TLS1.2 : If the session ticket extension is active, a session ticke may appear
				//          ProFTPd server will do this, for example
				//          One can forbid this by setting GNUTLS_NO_TICKETS_TLS12 on the init flags
				//          or by using %NO_TICKETS_TLS12 in the priority string in config
				// TLS1.3 : A session ticket appeared
				//
				if (description == (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_NEW_SESSION_TICKET) {
					SessionFlagsT flags = Native.SessionGetFlags(session);
					if (flags.HasFlag(SessionFlagsT.GNUTLS_SFLAGS_SESSION_TICKET)) {
						Native.SessionGetData2(session, ref resumeDataTLS);
						Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Retrieved session data with new session ticket");

						Native.SessionSetData(session, resumeDataTLS);
						//Native.GnuFree(resumeDataTLS.ptr);
					}
				}

			}

		}

		private void DisableNagle() {
			socket.NoDelay = true;
		}

		private void ReEnableNagle() {
			socket.NoDelay = false;
		}

		private void SetupHandshake() {

			// Stangely, one reads that this also somehow influences maximum TLS session time
			Native.DbSetCacheExpiration(sess, 100000000);

			// Handle the different ways Config could pass a priority string to here
			if (priority == string.Empty) {
				// None given, so use GnuTLS default
				Native.SetDefaultPriority(sess);
			}
			else if (priority.StartsWith("+") || priority.StartsWith("-")) {
				// Add or subtract from default
				Native.SetDefaultPriorityAppend(sess, priority);
			}
			else {
				// Use verbatim
				Native.PrioritySetDirect(sess, priority);
			}

			// Bits for Diffie-Hellman prime
			Native.DhSetPrimeBits(sess, 1024);

			// Allocate and link credential object
			Native.CredentialsSet(cred, sess);

			// Application Layer Protocol Negotiation (ALPN)
			// (alway AFTER credential allocation and setup
			if (!string.IsNullOrEmpty(alpn)) {
				Native.AlpnSetProtocols(sess, alpn);
			}

			// Tell GnuTLS how to send and receive: Use already open socket
			Native.TransportSetInt(sess, (int)socket.Handle);

			// Set the timeout for the handshake process
			Native.HandshakeSetTimeout(sess, (uint)timeout);

			// Any client certificate for presentation to server?
			SetupClientCertificates();

		}

		private void PopulateHandshakeInfo() {

			// This will be the GnuTLS format of the protocol name
			// TLS1.2, TLS1.3 or other
			ProtocolName = Native.ProtocolGetName(Native.ProtocolGetVersion(sess));

			// Try to "back-translate" to SslStream / System.Net.Security 
			if (ProtocolName == "TLS1.2") {
				SslProtocol = SslProtocols.Tls12;
			}
			else if (ProtocolName == "TLS1.3") {
				// Cannot set TLS1.3 on all builds, even if GnuTLS can do it, .NET can not
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

			// ftp / ftp-data
			AlpnProtocol = Native.AlpnGetSelectedProtocol(sess);

			// Maximum record size
			MaxRecordSize = Native.RecordGetMaxSize(sess);
			Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Maximum record size: " + MaxRecordSize);

			// Is this session a resume one?
			IsResumed = Native.SessionIsResumed(sess) == 1;
			if (IsResumed) {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Session is resumed from control connection");
			}

		}

		private void SetupClientCertificates() {

			//
			// TODO: Setup (if any) client certificates for verification
			//       by the server, at this point.
			// ****
			//

			Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Setup client certificate - currently not implemented");

		}

		private void ReportClientCertificateUsed() {

			if (Native.CertificateClientGetRequestStatus(sess)) {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Server requested client certificate");
			}
			else {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Server did not request client certificate");
			}

		}

		private void ValidateServerCertificates(CustomRemoteCertificateValidationCallback customRemoteCertificateValidation) {

			CertificateStatusT serverCertificateStatus;

			// Set Certificate Verification Profile and Flags
			// If no profile is set (uppermost 8 bits), it is taken from the priority string, please
			// read the GnuTLS docs on "priority strings".
			// If no flags are set, the internal default is used.

			// You could set these flags programmatically here, to overide the priority mechanism,
			// if you uncomment this statement:
			// Native.CertificateSetVerifyFlags(cred, (CertificateVerifyFlagsT)0x00FFFFFF);

			//
			// Perform the GnuTls internal validation, it is part of the handshake process
			//
			Native.CertificateVerifyPeers3(sess, hostname, out serverCertificateStatus);

			string serverCertificateStatusText = serverCertificateStatus.ToString("G");
			if (serverCertificateStatusText == "0") {
				serverCertificateStatusText = string.Empty;
			}

			if (serverCertificateStatus != 0) {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Internal server certificate validation function reports:");
				Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, serverCertificateStatusText);
			}

			//
			// Setup the (possibly) user supplied external validation callback
			//

			//
			// Determine the type of the servers certificate(s)/key and get the data out
			// from them.
			//
			CertificateTypeT certificateType = Native.CertificateTypeGet2(sess, CtypeTargetT.GNUTLS_CTYPE_PEERS);

			string serverCertificate = string.Empty;

			switch (certificateType) {
				case CertificateTypeT.GNUTLS_CRT_X509:
					GetCertInfoX509(out serverCertificate);
					break;

				case CertificateTypeT.GNUTLS_CRT_RAWPK:
					GetCertInfoRAWPK();
					break;

				default:
					break;
			}

			//
			// TODO: **** DONE
			// Convert the servers certificate, which is available now a PEM format in
			// a string, to the .NET certificate type. This is then passed to the callback
			// in the same format as it is in the SslStream validation callback, for coding
			// compatibility.
			//

			X509Certificate valCert = null;

			if (!string.IsNullOrEmpty(serverCertificate)) {
				valCert = new X509Certificate2(Encoding.ASCII.GetBytes(serverCertificate));
			}

			//
			// TODO:
			// Convert the servers certificate chain to the .NET format.
			//

			X509Chain valChain = null;

			//
			// Invoke any external user supplied validation callback
			//
			if (!customRemoteCertificateValidation(this, valCert, valChain, serverCertificateStatusText)) {
				Logging.LogGnuFunc(LogDebugInformationMessagesT.ClientCertificateValidation, "Error set by external server certificate validation function");
				throw new AuthenticationException(serverCertificateStatusText);
			};

			// End of method here
			// Local context functions:

			//
			// Extract X509 certificate(s)
			//
			void GetCertInfoX509(out string pCertS) {

				pCertS= string.Empty;

				DatumT[] data;
				uint numData = 0;

				// Get the servers list of X.509 certificates, these will be in DER format
				data = Native.CertificateGetPeers(sess, ref numData);
				if (numData == 0) {
					Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, "No certificates found");
					return;
				}

				//Logging.LogGnuFunc("Certificate type: X.509, list contains " + numData);

				IntPtr cert = IntPtr.Zero;
				DatumT pinfo = new();
				DatumT cinfo = new();

				for (uint i = 0; i < numData; i++) {

					Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, "Certificate #" + (i + 1));

					int result;

					result = Native.X509CrtInit(ref cert);
					if (result < 0) {
						Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, "Error allocating Memory");
						return;
					}

					result = Native.X509CrtImport(cert, ref data[i], X509CrtFmtT.GNUTLS_X509_FMT_DER);
					if (result < 0) {
						Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, "Error decoding: " + Utils.GnuTlsErrorText(result));
						return;
					}

					result = Native.X509CrtExport2(cert, X509CrtFmtT.GNUTLS_X509_FMT_PEM, ref cinfo);
					if (result == 0) {
						string cOutput = Marshal.PtrToStringAnsi(cinfo.ptr);
						pCertS = cOutput;
						Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, cOutput);
						//Native.GnuFree(pinfo.ptr);
					}

					CertificatePrintFormatsT flag = CertificatePrintFormatsT.GNUTLS_CRT_PRINT_FULL;
					result = Native.X509CrtPrint(cert, flag, ref pinfo);
					if (result == 0) {
						string pOutput = Marshal.PtrToStringAnsi(pinfo.ptr);
						Logging.LogGnuFunc(LogDebugInformationMessagesT.X509, pOutput);
						//Native.GnuFree(cinfo.ptr);
					}

					Native.X509CrtDeinit(cert);

				}

				return;
			}

			//
			// Extract Raw Publick Key "certificate(s)"
			//
			void GetCertInfoRAWPK() {

				DatumT[] data;
				uint numData = 0;

				// Get the servers list of Raw Public Key certificates, these will be in DER format
				data = Native.CertificateGetPeers(sess, ref numData);
				if (numData == 0) {
					Logging.LogGnuFunc(LogDebugInformationMessagesT.RAWPK, "No certificates found");
					return;
				}

				//Logging.LogGnuFunc("Certificate type: Raw Public Key, list contains " + numData);

				IntPtr cert = IntPtr.Zero;
				PkAlgorithmT algo;
				DatumT cinfo = new();

				int result;

				result = Native.PcertImportRawpkRaw(cert, ref data[0], X509CrtFmtT.GNUTLS_X509_FMT_DER, 0, 0);
				if (result < 0) {
					Logging.LogGnuFunc(LogDebugInformationMessagesT.RAWPK, "Error decoding: " + Utils.GnuTlsErrorText(result));
					return;
				}

				//
				// TODO:
				//
				//pk_algo = gnutls_pubkey_get_pk_algorithm(pk_cert.pubkey, NULL);

				//log_msg(out, "- Raw pk info:\n");
				//log_msg(out, " - PK algo: %s\n", gnutls_pk_algorithm_get_name(pk_algo));

				//if (print_cert) {
				//	gnutls_datum_t pem;

				//	ret = gnutls_pubkey_export2(pk_cert.pubkey, GNUTLS_X509_FMT_PEM, &pem);
				//	if (ret < 0) {
				//		fprintf(stderr, "Encoding error: %s\n",
				//			gnutls_strerror(ret));
				//		return;
				//	}

				//	log_msg(out, "\n%s\n", (char*)pem.data);

				//	gnutls_free(pem.data);
				//}

				return;
			}

		}

	}
}
