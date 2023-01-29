using System;
using System.Runtime.InteropServices;

namespace FluentFTP.GnuTLS.Core {

	[StructLayout(LayoutKind.Sequential)]
	public class DatumT {
		public IntPtr ptr = IntPtr.Zero;
		public uint size = 0;
	}

	//	[StructLayout(LayoutKind.Explicit)]
	//	unsafe struct headerUnion                  // 2048 bytes in header
	//  {
	//		[FieldOffset(0)]
	//		public fixed byte headerBytes[2048];
	//		[FieldOffset(0)]
	//		public long header;
	//	}

	/**
	* gnutls_init_flags_t:
	*
	* @GNUTLS_SERVER: Connection end is a server.
	* @GNUTLS_CLIENT: Connection end is a client.
	* @GNUTLS_DATAGRAM: Connection is datagram oriented (DTLS). Since 3.0.0.
	* @GNUTLS_NONBLOCK: Connection should not block. Since 3.0.0.
	* @GNUTLS_NO_SIGNAL: In systems where SIGPIPE is delivered on send, it will be disabled. That flag has effect in systems which support the MSG_NOSIGNAL sockets flag (since 3.4.2).
	* @GNUTLS_NO_EXTENSIONS: Do not enable any TLS extensions by default (since 3.1.2). As TLS 1.2 and later require extensions this option is considered obsolete and should not be used.
	* @GNUTLS_NO_REPLAY_PROTECTION: Disable any replay protection in DTLS. This must only be used if  replay protection is achieved using other means. Since 3.2.2.
	* @GNUTLS_ALLOW_ID_CHANGE: Allow the peer to replace its certificate, or change its ID during a rehandshake. This change is often used in attacks and thus prohibited by default. Since 3.5.0.
	* @GNUTLS_ENABLE_FALSE_START: Enable the TLS false start on client side if the negotiated ciphersuites allow it. This will enable sending data prior to the handshake being complete, and may introduce a risk of crypto failure when combined with certain key exchanged; for that GnuTLS may not enable that option in ciphersuites that are known to be not safe for false start. Since 3.5.0.
	* @GNUTLS_ENABLE_EARLY_START: Under TLS1.3 allow the server to return earlier than the full handshake
	*   finish; similarly to false start the handshake will be completed once data are received by the
	*   client, while the server is able to transmit sooner. This is not enabled by default as it could
	*   break certain existing server assumptions and use-cases. Since 3.6.4.
	* @GNUTLS_ENABLE_EARLY_DATA: Under TLS1.3 allow the server to receive early data sent as part of the initial ClientHello (0-RTT).
	*   This can also be used to explicitly indicate that the client will send early data.
	*   This is not enabled by default as early data has weaker security properties than other data. Since 3.6.5.
	* @GNUTLS_FORCE_CLIENT_CERT: When in client side and only a single cert is specified, send that certificate irrespective of the issuers expected by the server. Since 3.5.0.
	* @GNUTLS_NO_TICKETS: Flag to indicate that the session should not use resumption with session tickets.
	* @GNUTLS_NO_TICKETS_TLS12: Flag to indicate that the session should not use resumption with session tickets. This flag only has effect if TLS 1.2 is used.
	* @GNUTLS_KEY_SHARE_TOP3: Generate key shares for the top-3 different groups which are enabled.
	*   That is, as each group is associated with a key type (EC, finite field, x25519), generate
	*   three keys using %GNUTLS_PK_DH, %GNUTLS_PK_EC, %GNUTLS_PK_ECDH_X25519 if all of them are enabled.
	* @GNUTLS_KEY_SHARE_TOP2: Generate key shares for the top-2 different groups which are enabled.
	*   For example (ECDH + x25519). This is the default.
	* @GNUTLS_KEY_SHARE_TOP: Generate key share for the first group which is enabled.
	*   For example x25519. This option is the most performant for client (less CPU spent
	*   generating keys), but if the server doesn't support the advertized option it may
	*   result to more roundtrips needed to discover the server's choice.
	* @GNUTLS_NO_AUTO_REKEY: Disable auto-rekeying under TLS1.3. If this option is not specified
	*   gnutls will force a rekey after 2^24 records have been sent.
	* @GNUTLS_POST_HANDSHAKE_AUTH: Enable post handshake authentication for server and client. When set and
	*   a server requests authentication after handshake %GNUTLS_E_REAUTH_REQUEST will be returned
	*   by gnutls_record_recv(). A client should then call gnutls_reauth() to re-authenticate.
	* @GNUTLS_SAFE_PADDING_CHECK: Flag to indicate that the TLS 1.3 padding check will be done in a
	*   safe way which doesn't leak the pad size based on GnuTLS processing time. This is of use to
	*   applications which hide the length of transferred data via the TLS1.3 padding mechanism and
	*   are already taking steps to hide the data processing time. This comes at a performance
	*   penalty.
	* @GNUTLS_AUTO_REAUTH: Enable transparent re-authentication in client side when the server
	*    requests to. That is, reauthentication is handled within gnutls_record_recv(), and
	*    the %GNUTLS_E_REHANDSHAKE or %GNUTLS_E_REAUTH_REQUEST are not returned. This must be
	*    enabled with %GNUTLS_POST_HANDSHAKE_AUTH for TLS1.3. Enabling this flag requires to restore
	*    interrupted calls to gnutls_record_recv() based on the output of gnutls_record_get_direction(),
	*    since gnutls_record_recv() could be interrupted when sending when this flag is enabled.
	*    Note this flag may not be used if you are using the same session for sending and receiving
	*    in different threads.
	* @GNUTLS_ENABLE_RAWPK: Allows raw public-keys to be negotiated during the handshake. Since 3.6.6.
	* @GNUTLS_NO_AUTO_SEND_TICKET: Under TLS1.3 disable auto-sending of
	*    session tickets during the handshake.
	* @GNUTLS_NO_END_OF_EARLY_DATA: Under TLS1.3 suppress sending EndOfEarlyData message. Since 3.7.2.
	*
	* Enumeration of different flags for gnutls_init() function. All the flags
	* can be combined except @GNUTLS_SERVER and @GNUTLS_CLIENT which are mutually
	* exclusive.
	*
	* The key share options relate to the TLS 1.3 key share extension
	* which is a speculative key generation expecting that the server
	* would support the generated key.
*/
	[Flags]
	public enum InitFlagsT : uint {
		GNUTLS_SERVER = 1,
		GNUTLS_CLIENT = 1 << 1,
		GNUTLS_DATAGRAM = 1 << 2,
		GNUTLS_NONBLOCK = 1 << 3,
		GNUTLS_NO_EXTENSIONS = 1 << 4,
		GNUTLS_NO_REPLAY_PROTECTION = 1 << 5,
		GNUTLS_NO_SIGNAL = 1 << 6,
		GNUTLS_ALLOW_ID_CHANGE = 1 << 7,
		GNUTLS_ENABLE_FALSE_START = 1 << 8,
		GNUTLS_FORCE_CLIENT_CERT = 1 << 9,
		GNUTLS_NO_TICKETS = 1 << 10,
		GNUTLS_KEY_SHARE_TOP = 1 << 11,
		GNUTLS_KEY_SHARE_TOP2 = 1 << 12,
		GNUTLS_KEY_SHARE_TOP3 = 1 << 13,
		GNUTLS_POST_HANDSHAKE_AUTH = 1 << 14,
		GNUTLS_NO_AUTO_REKEY = 1 << 15,
		GNUTLS_SAFE_PADDING_CHECK = 1 << 16,
		GNUTLS_ENABLE_EARLY_START = 1 << 17,
		GNUTLS_ENABLE_RAWPK = 1 << 18,
		GNUTLS_AUTO_REAUTH = 1 << 19,
		GNUTLS_ENABLE_EARLY_DATA = 1 << 20,
		GNUTLS_NO_AUTO_SEND_TICKET = 1 << 21,
		GNUTLS_NO_END_OF_EARLY_DATA = 1 << 22,
		GNUTLS_NO_TICKETS_TLS12 = 1 << 23
	}

	/**
	 * gnutls_close_request_t:
	 * @GNUTLS_SHUT_RDWR: Disallow further receives/sends.
	 * @GNUTLS_SHUT_WR: Disallow further sends.
	 *
	 * Enumeration of how TLS session should be terminated.  See gnutls_bye().
*/
	public enum CloseRequestT : uint {
		GNUTLS_SHUT_RDWR = 0,
		GNUTLS_SHUT_WR = 1
	}

	/**
	 * gnutls_server_name_type_t:
	 * @GNUTLS_NAME_DNS: Domain Name System name type.
	 *
	 * Enumeration of different server name types.
	*/
	public enum ServerNameTypeT : uint {
		GNUTLS_NAME_DNS = 1
	}

	/**
	 * gnutls_credentials_type_t:
	 * @GNUTLS_CRD_CERTIFICATE: Certificate credential.
	 * @GNUTLS_CRD_ANON: Anonymous credential.
	 * @GNUTLS_CRD_SRP: SRP credential.
	 * @GNUTLS_CRD_PSK: PSK credential.
	 * @GNUTLS_CRD_IA: IA credential.
	 *
	 * Enumeration of different credential types.
	*/
	public enum CredentialsTypeT : uint {
		GNUTLS_CRD_CERTIFICATE = 1,
		GNUTLS_CRD_ANON = 2,
		GNUTLS_CRD_SRP = 3,
		GNUTLS_CRD_PSK = 4,
		GNUTLS_CRD_IA = 5
	}

	/**
	 * gnutls_alpn_flags_t:
	 * @GNUTLS_ALPN_MANDATORY: Require ALPN negotiation. The connection will be
	 *   aborted if no matching ALPN protocol is found.
	 * @GNUTLS_ALPN_SERVER_PRECEDENCE: The choices set by the server
	 *   will take precedence over the client's.
	 *
	 * Enumeration of different ALPN flags. These are used by gnutls_alpn_set_protocols().
	*/
	[Flags]
	public enum AlpnFlagsT {
		GNUTLS_ALPN_MANDATORY = 1,
		GNUTLS_ALPN_SERVER_PRECEDENCE = (1 << 1)
	}

	/**
	 * gnutls_protocol_t:
	 * @GNUTLS_SSL3: SSL version 3.0.
	 * @GNUTLS_TLS1_0: TLS version 1.0.
	 * @GNUTLS_TLS1: Same as %GNUTLS_TLS1_0.
	 * @GNUTLS_TLS1_1: TLS version 1.1.
	 * @GNUTLS_TLS1_2: TLS version 1.2.
	 * @GNUTLS_TLS1_3: TLS version 1.3.
	 * @GNUTLS_DTLS1_0: DTLS version 1.0.
	 * @GNUTLS_DTLS1_2: DTLS version 1.2.
	 * @GNUTLS_DTLS0_9: DTLS version 0.9 (Cisco AnyConnect / OpenSSL 0.9.8e).
	 * @GNUTLS_TLS_VERSION_MAX: Maps to the highest supported TLS version.
	 * @GNUTLS_DTLS_VERSION_MAX: Maps to the highest supported DTLS version.
	 * @GNUTLS_VERSION_UNKNOWN: Unknown SSL/TLS version.
	 *
	 * Enumeration of different SSL/TLS protocol versions.
	*/
	public enum ProtocolT : uint {
		GNUTLS_SSL3 = 1,
		GNUTLS_TLS1_0 = 2,
		GNUTLS_TLS1_1 = 3,
		GNUTLS_TLS1_2 = 4,
		GNUTLS_TLS1_3 = 5,

		GNUTLS_DTLS0_9 = 200,
		GNUTLS_DTLS1_0 = 201,
		GNUTLS_DTLS1_2 = 202,
	}

	/**
	 * gnutls_handshake_description_t:
	 * @GNUTLS_HANDSHAKE_HELLO_REQUEST: Hello request.
	 * @GNUTLS_HANDSHAKE_HELLO_VERIFY_REQUEST: DTLS Hello verify request.
	 * @GNUTLS_HANDSHAKE_CLIENT_HELLO: Client hello.
	 * @GNUTLS_HANDSHAKE_SERVER_HELLO: Server hello.
	 * @GNUTLS_HANDSHAKE_END_OF_EARLY_DATA: End of early data.
	 * @GNUTLS_HANDSHAKE_HELLO_RETRY_REQUEST: Hello retry request.
	 * @GNUTLS_HANDSHAKE_NEW_SESSION_TICKET: New session ticket.
	 * @GNUTLS_HANDSHAKE_CERTIFICATE_PKT: Certificate packet.
	 * @GNUTLS_HANDSHAKE_SERVER_KEY_EXCHANGE: Server key exchange.
	 * @GNUTLS_HANDSHAKE_CERTIFICATE_REQUEST: Certificate request.
	 * @GNUTLS_HANDSHAKE_SERVER_HELLO_DONE: Server hello done.
	 * @GNUTLS_HANDSHAKE_CERTIFICATE_VERIFY: Certificate verify.
	 * @GNUTLS_HANDSHAKE_CLIENT_KEY_EXCHANGE: Client key exchange.
	 * @GNUTLS_HANDSHAKE_FINISHED: Finished.
	 * @GNUTLS_HANDSHAKE_CERTIFICATE_STATUS: Certificate status (OCSP).
	 * @GNUTLS_HANDSHAKE_KEY_UPDATE: TLS1.3 key update message.
	 * @GNUTLS_HANDSHAKE_COMPRESSED_CERTIFICATE_PKT: Compressed certificate packet.
	 * @GNUTLS_HANDSHAKE_SUPPLEMENTAL: Supplemental.
	 * @GNUTLS_HANDSHAKE_CHANGE_CIPHER_SPEC: Change Cipher Spec.
	 * @GNUTLS_HANDSHAKE_CLIENT_HELLO_V2: SSLv2 Client Hello.
	 * @GNUTLS_HANDSHAKE_ENCRYPTED_EXTENSIONS: Encrypted extensions message.
	 *
	 * Enumeration of different TLS handshake packets.
	*/
	public enum HandshakeDescriptionT : uint {
		GNUTLS_HANDSHAKE_HELLO_REQUEST = 0,
		GNUTLS_HANDSHAKE_CLIENT_HELLO = 1,
		GNUTLS_HANDSHAKE_SERVER_HELLO = 2,
		GNUTLS_HANDSHAKE_HELLO_VERIFY_REQUEST = 3,
		GNUTLS_HANDSHAKE_NEW_SESSION_TICKET = 4,
		GNUTLS_HANDSHAKE_END_OF_EARLY_DATA = 5,
		GNUTLS_HANDSHAKE_ENCRYPTED_EXTENSIONS = 8,
		GNUTLS_HANDSHAKE_CERTIFICATE_PKT = 11,
		GNUTLS_HANDSHAKE_SERVER_KEY_EXCHANGE = 12,
		GNUTLS_HANDSHAKE_CERTIFICATE_REQUEST = 13,
		GNUTLS_HANDSHAKE_SERVER_HELLO_DONE = 14,
		GNUTLS_HANDSHAKE_CERTIFICATE_VERIFY = 15,
		GNUTLS_HANDSHAKE_CLIENT_KEY_EXCHANGE = 16,
		GNUTLS_HANDSHAKE_FINISHED = 20,
		GNUTLS_HANDSHAKE_CERTIFICATE_STATUS = 22,
		GNUTLS_HANDSHAKE_SUPPLEMENTAL = 23,
		GNUTLS_HANDSHAKE_KEY_UPDATE = 24,
		GNUTLS_HANDSHAKE_COMPRESSED_CERTIFICATE_PKT = 25,
		GNUTLS_HANDSHAKE_CHANGE_CIPHER_SPEC = 254,
		GNUTLS_HANDSHAKE_CLIENT_HELLO_V2 = 1024,
		GNUTLS_HANDSHAKE_HELLO_RETRY_REQUEST = 1025,
		GNUTLS_HANDSHAKE_ANY = unchecked((uint)-1),
	}

	/**
	 * gnutls_handshake_hook_func:
	 * @session: the current session
	 * @htype: the type of the handshake message (%gnutls_handshake_description_t)
	 * @when: non zero if this is a post-process/generation call and zero otherwise
	 * @incoming: non zero if this is an incoming message and zero if this is an outgoing message
	 * @msg: the (const) data of the handshake message without the handshake headers.
	 *
	 * Function prototype for handshake hooks. It is set using
	 * gnutls_handshake_set_hook_function().
	 *
	 * Returns: Non zero on error.
	*/
	public enum HandshakeHookT : int {
		GNUTLS_HOOK_PRE = 0,
		GNUTLS_HOOK_POST = 1, 
		GNUTLS_HOOK_BOTH = -1,
	}

	/**
	 * gnutls_alert_level_t:
	 * @GNUTLS_AL_WARNING: Alert of warning severity.
	 * @GNUTLS_AL_FATAL: Alert of fatal severity.
	 *
	 * Enumeration of different TLS alert severities.
	 */
	public enum AlertLevelT : uint {
		GNUTLS_AL_WARNING = 1,
		GNUTLS_AL_FATAL
	}

	/**
	 * gnutls_alert_description_t:
	 * @GNUTLS_A_CLOSE_NOTIFY: Close notify.
	 * @GNUTLS_A_UNEXPECTED_MESSAGE: Unexpected message.
	 * @GNUTLS_A_BAD_RECORD_MAC: Bad record MAC.
	 * @GNUTLS_A_DECRYPTION_FAILED: Decryption failed.
	 * @GNUTLS_A_RECORD_OVERFLOW: Record overflow.
	 * @GNUTLS_A_DECOMPRESSION_FAILURE: Decompression failed.
	 * @GNUTLS_A_HANDSHAKE_FAILURE: Handshake failed.
	 * @GNUTLS_A_SSL3_NO_CERTIFICATE: No certificate.
	 * @GNUTLS_A_BAD_CERTIFICATE: Certificate is bad.
	 * @GNUTLS_A_UNSUPPORTED_CERTIFICATE: Certificate is not supported.
	 * @GNUTLS_A_CERTIFICATE_REVOKED: Certificate was revoked.
	 * @GNUTLS_A_CERTIFICATE_EXPIRED: Certificate is expired.
	 * @GNUTLS_A_CERTIFICATE_UNKNOWN: Unknown certificate.
	 * @GNUTLS_A_ILLEGAL_PARAMETER: Illegal parameter.
	 * @GNUTLS_A_UNKNOWN_CA: CA is unknown.
	 * @GNUTLS_A_ACCESS_DENIED: Access was denied.
	 * @GNUTLS_A_DECODE_ERROR: Decode error.
	 * @GNUTLS_A_DECRYPT_ERROR: Decrypt error.
	 * @GNUTLS_A_EXPORT_RESTRICTION: Export restriction.
	 * @GNUTLS_A_PROTOCOL_VERSION: Error in protocol version.
	 * @GNUTLS_A_INSUFFICIENT_SECURITY: Insufficient security.
	 * @GNUTLS_A_INTERNAL_ERROR: Internal error.
	 * @GNUTLS_A_INAPPROPRIATE_FALLBACK: Inappropriate fallback,
	 * @GNUTLS_A_USER_CANCELED: User canceled.
	 * @GNUTLS_A_NO_RENEGOTIATION: No renegotiation is allowed.
	 * @GNUTLS_A_MISSING_EXTENSION: An extension was expected but was not seen
	 * @GNUTLS_A_UNSUPPORTED_EXTENSION: An unsupported extension was
	 *   sent.
	 * @GNUTLS_A_CERTIFICATE_UNOBTAINABLE: Could not retrieve the
	 *   specified certificate.
	 * @GNUTLS_A_UNRECOGNIZED_NAME: The server name sent was not
	 *   recognized.
	 * @GNUTLS_A_UNKNOWN_PSK_IDENTITY: The SRP/PSK username is missing
	 *   or not known.
	 * @GNUTLS_A_CERTIFICATE_REQUIRED: Certificate is required.
	 * @GNUTLS_A_NO_APPLICATION_PROTOCOL: The ALPN protocol requested is
	 *   not supported by the peer.
	 *
	 * Enumeration of different TLS alerts.
	*/
	public enum AlertDescriptionT : uint {
		GNUTLS_A_CLOSE_NOTIFY,
		GNUTLS_A_UNEXPECTED_MESSAGE = 10,
		GNUTLS_A_BAD_RECORD_MAC = 20,
		GNUTLS_A_DECRYPTION_FAILED,
		GNUTLS_A_RECORD_OVERFLOW,
		GNUTLS_A_DECOMPRESSION_FAILURE = 30,
		GNUTLS_A_HANDSHAKE_FAILURE = 40,
		GNUTLS_A_SSL3_NO_CERTIFICATE = 41,
		GNUTLS_A_BAD_CERTIFICATE = 42,
		GNUTLS_A_UNSUPPORTED_CERTIFICATE,
		GNUTLS_A_CERTIFICATE_REVOKED,
		GNUTLS_A_CERTIFICATE_EXPIRED,
		GNUTLS_A_CERTIFICATE_UNKNOWN,
		GNUTLS_A_ILLEGAL_PARAMETER,
		GNUTLS_A_UNKNOWN_CA,
		GNUTLS_A_ACCESS_DENIED,
		GNUTLS_A_DECODE_ERROR = 50,
		GNUTLS_A_DECRYPT_ERROR,
		GNUTLS_A_EXPORT_RESTRICTION = 60,
		GNUTLS_A_PROTOCOL_VERSION = 70,
		GNUTLS_A_INSUFFICIENT_SECURITY,
		GNUTLS_A_INTERNAL_ERROR = 80,
		GNUTLS_A_INAPPROPRIATE_FALLBACK = 86,
		GNUTLS_A_USER_CANCELED = 90,
		GNUTLS_A_NO_RENEGOTIATION = 100,
		GNUTLS_A_MISSING_EXTENSION = 109,
		GNUTLS_A_UNSUPPORTED_EXTENSION = 110,
		GNUTLS_A_CERTIFICATE_UNOBTAINABLE = 111,
		GNUTLS_A_UNRECOGNIZED_NAME = 112,
		GNUTLS_A_UNKNOWN_PSK_IDENTITY = 115,
		GNUTLS_A_CERTIFICATE_REQUIRED = 116,
		GNUTLS_A_NO_APPLICATION_PROTOCOL = 120,
		GNUTLS_A_MAX = GNUTLS_A_NO_APPLICATION_PROTOCOL
	}

	public class TimeoutV {
		// Very special values:
		uint GNUTLS_DEFAULT_HANDSHAKE_TIMEOUT = unchecked((uint)-1);
		uint GNUTLS_INDEFINITE_TIMEOUT = unchecked((uint)-2);
	}
}
