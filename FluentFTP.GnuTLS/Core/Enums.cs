using System;
using System.Runtime.InteropServices;

namespace FluentFTP.GnuTLS.Core {

	[StructLayout(LayoutKind.Sequential)]
	public struct DatumT {
		public IntPtr ptr;
		public ulong size;
	}

	//
	// Enums/Types gleaned from GnuTLS V 3.7.7 to help interop
	// 

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
		GNUTLS_ALPN_SERVER_PRECEDENCE = 1 << 1
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

	/**
	 * gnutls_session_flags_t:
	 * @GNUTLS_SFLAGS_SAFE_RENEGOTIATION: Safe renegotiation (RFC5746) was used
	 * @GNUTLS_SFLAGS_EXT_MASTER_SECRET: The extended master secret (RFC7627) extension was used
	 * @GNUTLS_SFLAGS_ETM: The encrypt then MAC (RFC7366) extension was used
	 * @GNUTLS_SFLAGS_RFC7919: The RFC7919 Diffie-Hellman parameters were negotiated
	 * @GNUTLS_SFLAGS_HB_LOCAL_SEND: The heartbeat negotiation allows the local side to send heartbeat messages
	 * @GNUTLS_SFLAGS_HB_PEER_SEND: The heartbeat negotiation allows the peer to send heartbeat messages
	 * @GNUTLS_SFLAGS_FALSE_START: False start was used in this client session.
	 * @GNUTLS_SFLAGS_SESSION_TICKET: A session ticket has been received by the server.
	 * @GNUTLS_SFLAGS_POST_HANDSHAKE_AUTH: Indicates client capability for post-handshake auth; set only on server side.
	 * @GNUTLS_SFLAGS_EARLY_START: The TLS1.3 server session returned early.
	 * @GNUTLS_SFLAGS_EARLY_DATA: The TLS1.3 early data has been received by the server.
	 * @GNUTLS_SFLAGS_CLI_REQUESTED_OCSP: Set when the client has requested OCSP staple during handshake.
	 * @GNUTLS_SFLAGS_SERV_REQUESTED_OCSP: Set when the server has requested OCSP staple during handshake.
	 *
	 * Enumeration of different session parameters.
	*/
	[Flags]
	enum SessionFlagsT : uint {
		GNUTLS_SFLAGS_SAFE_RENEGOTIATION = 1,
		GNUTLS_SFLAGS_EXT_MASTER_SECRET = 1 << 1,
		GNUTLS_SFLAGS_ETM = 1 << 2,
		GNUTLS_SFLAGS_HB_LOCAL_SEND = 1 << 3,
		GNUTLS_SFLAGS_HB_PEER_SEND = 1 << 4,
		GNUTLS_SFLAGS_FALSE_START = 1 << 5,
		GNUTLS_SFLAGS_RFC7919 = 1 << 6,
		GNUTLS_SFLAGS_SESSION_TICKET = 1 << 7,
		GNUTLS_SFLAGS_POST_HANDSHAKE_AUTH = 1 << 8,
		GNUTLS_SFLAGS_EARLY_START = 1 << 9,
		GNUTLS_SFLAGS_EARLY_DATA = 1 << 10,
		GNUTLS_SFLAGS_CLI_REQUESTED_OCSP = 1 << 11,
		GNUTLS_SFLAGS_SERV_REQUESTED_OCSP = 1 << 12
	}

	public class TimeoutV {
		// Very special values:
		uint GNUTLS_DEFAULT_HANDSHAKE_TIMEOUT = unchecked((uint)-1);
		uint GNUTLS_INDEFINITE_TIMEOUT = unchecked((uint)-2);
	}

	/**
	 * gnutls_certificate_status_t:
	 * @GNUTLS_CERT_INVALID: The certificate is not signed by one of the
	 *   known authorities or the signature is invalid (deprecated by the flags 
	 *   %GNUTLS_CERT_SIGNATURE_FAILURE and %GNUTLS_CERT_SIGNER_NOT_FOUND).
	 * @GNUTLS_CERT_SIGNATURE_FAILURE: The signature verification failed.
	 * @GNUTLS_CERT_REVOKED: Certificate is revoked by its authority.  In X.509 this will be
	 *   set only if CRLs are checked.
	 * @GNUTLS_CERT_SIGNER_NOT_FOUND: The certificate's issuer is not known. 
	 *   This is the case if the issuer is not included in the trusted certificate list.
	 * @GNUTLS_CERT_SIGNER_NOT_CA: The certificate's signer was not a CA. This
	 *   may happen if this was a version 1 certificate, which is common with
	 *   some CAs, or a version 3 certificate without the basic constrains extension.
	 * @GNUTLS_CERT_SIGNER_CONSTRAINTS_FAILURE: The certificate's signer constraints were
	 *   violated.
	 * @GNUTLS_CERT_INSECURE_ALGORITHM:  The certificate was signed using an insecure
	 *   algorithm such as MD2 or MD5. These algorithms have been broken and
	 *   should not be trusted.
	 * @GNUTLS_CERT_NOT_ACTIVATED: The certificate is not yet activated.
	 * @GNUTLS_CERT_EXPIRED: The certificate has expired.
	 * @GNUTLS_CERT_REVOCATION_DATA_SUPERSEDED: The revocation data are old and have been superseded.
	 * @GNUTLS_CERT_REVOCATION_DATA_ISSUED_IN_FUTURE: The revocation data have a future issue date.
	 * @GNUTLS_CERT_UNEXPECTED_OWNER: The owner is not the expected one.
	 * @GNUTLS_CERT_MISMATCH: The certificate presented isn't the expected one (TOFU)
	 * @GNUTLS_CERT_PURPOSE_MISMATCH: The certificate or an intermediate does not match the intended purpose (extended key usage).
	 * @GNUTLS_CERT_MISSING_OCSP_STATUS: The certificate requires the server to send the certificate status, but no status was received.
	 * @GNUTLS_CERT_INVALID_OCSP_STATUS: The received OCSP status response is invalid.
	 * @GNUTLS_CERT_UNKNOWN_CRIT_EXTENSIONS: The certificate has extensions marked as critical which are not supported.
	 *
	 * Enumeration of certificate status codes.  Note that the status
	 * bits may have different meanings in OpenPGP keys and X.509
	 * certificate verification.
	*/
	[Flags]
	enum CertificateStatusT : uint {
		INVALID = 1 << 1,
		REVOKED = 1 << 5,
		SIGNER_NOT_FOUND = 1 << 6,
		SIGNER_NOT_CA = 1 << 7,
		INSECURE_ALGORITHM = 1 << 8,
		NOT_ACTIVATED = 1 << 9,
		EXPIRED = 1 << 10,
		SIGNATURE_FAILURE = 1 << 11,
		REVOCATION_DATA_SUPERSEDED = 1 << 12,
		UNEXPECTED_OWNER = 1 << 14,
		REVOCATION_DATA_ISSUED_IN_FUTURE = 1 << 15,
		SIGNER_CONSTRAINTS_FAILURE = 1 << 16,
		MISMATCH = 1 << 17,
		PURPOSE_MISMATCH = 1 << 18,
		MISSING_OCSP_STATUS = 1 << 19,
		INVALID_OCSP_STATUS = 1 << 20,
		UNKNOWN_CRIT_EXTENSIONS = 1 << 21
	}

	/**
	 * gnutls_certificate_request_t:
	 * @GNUTLS_CERT_IGNORE: Ignore certificate.
	 * @GNUTLS_CERT_REQUEST: Request certificate.
	 * @GNUTLS_CERT_REQUIRE: Require certificate.
	 *
	 * Enumeration of certificate request types.
	*/
	enum CertificateRequestT : uint {
		GNUTLS_CERT_IGNORE = 0,
		GNUTLS_CERT_REQUEST = 1,
		GNUTLS_CERT_REQUIRE = 2
	}

	/**
	 * gnutls_certificate_verify_flags:
	 * @GNUTLS_VERIFY_DISABLE_CA_SIGN: If set a signer does not have to be
	 *   a certificate authority. This flag should normally be disabled,
	 *   unless you know what this means.
	 * @GNUTLS_VERIFY_DISABLE_TRUSTED_TIME_CHECKS: If set a signer in the trusted
	 *   list is never checked for expiration or activation.
	 * @GNUTLS_VERIFY_DO_NOT_ALLOW_X509_V1_CA_CRT: Do not allow trusted CA
	 *   certificates that have version 1.  This option is to be used
	 *   to deprecate all certificates of version 1.
	 * @GNUTLS_VERIFY_DO_NOT_ALLOW_SAME: If a certificate is not signed by
	 *   anyone trusted but exists in the trusted CA list do not treat it
	 *   as trusted.
	 * @GNUTLS_VERIFY_ALLOW_UNSORTED_CHAIN: A certificate chain is tolerated
	 *   if unsorted (the case with many TLS servers out there). This is the
	 *   default since GnuTLS 3.1.4.
	 * @GNUTLS_VERIFY_DO_NOT_ALLOW_UNSORTED_CHAIN: Do not tolerate an unsorted
	 *   certificate chain.
	 * @GNUTLS_VERIFY_ALLOW_ANY_X509_V1_CA_CRT: Allow CA certificates that
	 *   have version 1 (both root and intermediate). This might be
	 *   dangerous since those haven't the basicConstraints
	 *   extension. 
	 * @GNUTLS_VERIFY_ALLOW_SIGN_RSA_MD2: Allow certificates to be signed
	 *   using the broken MD2 algorithm.
	 * @GNUTLS_VERIFY_ALLOW_SIGN_RSA_MD5: Allow certificates to be signed
	 *   using the broken MD5 algorithm.
	 * @GNUTLS_VERIFY_ALLOW_SIGN_WITH_SHA1: Allow certificates to be signed
	 *   using the broken SHA1 hash algorithm.
	 * @GNUTLS_VERIFY_ALLOW_BROKEN: Allow certificates to be signed
	 *   using any broken algorithm.
	 * @GNUTLS_VERIFY_DISABLE_TIME_CHECKS: Disable checking of activation
	 *   and expiration validity periods of certificate chains. Don't set
	 *   this unless you understand the security implications.
	 * @GNUTLS_VERIFY_DISABLE_CRL_CHECKS: Disable checking for validity
	 *   using certificate revocation lists or the available OCSP data.
	 * @GNUTLS_VERIFY_DO_NOT_ALLOW_WILDCARDS: When including a hostname
	 *   check in the verification, do not consider any wildcards.
	 * @GNUTLS_VERIFY_DO_NOT_ALLOW_IP_MATCHES: When verifying a hostname
	 *   prevent textual IP addresses from matching IP addresses in the
	 *   certificate. Treat the input only as a DNS name.
	 * @GNUTLS_VERIFY_USE_TLS1_RSA: This indicates that a (raw) RSA signature is provided
	 *   as in the TLS 1.0 protocol. Not all functions accept this flag.
	 * @GNUTLS_VERIFY_IGNORE_UNKNOWN_CRIT_EXTENSIONS: This signals the verification
	 *   process, not to fail on unknown critical extensions.
	 * @GNUTLS_VERIFY_RSA_PSS_FIXED_SALT_LENGTH: Disallow RSA-PSS signatures made
	 *   with mismatching salt length with digest length, as mandated in RFC 8446
	 *   4.2.3.
	 *
	 * Enumeration of different certificate verify flags. Additional
	 * verification profiles can be set using GNUTLS_PROFILE_TO_VFLAGS()
	 * and %gnutls_certificate_verification_profiles_t.
	*/
	[Flags]
	enum CertificateVerifyFlagsT : uint {
		GNUTLS_VERIFY_DISABLE_CA_SIGN = 1 << 0,
		GNUTLS_VERIFY_DO_NOT_ALLOW_IP_MATCHES = 1 << 1,
		GNUTLS_VERIFY_DO_NOT_ALLOW_SAME = 1 << 2,
		GNUTLS_VERIFY_ALLOW_ANY_X509_V1_CA_CRT = 1 << 3,
		GNUTLS_VERIFY_ALLOW_SIGN_RSA_MD2 = 1 << 4,
		GNUTLS_VERIFY_ALLOW_SIGN_RSA_MD5 = 1 << 5,
		GNUTLS_VERIFY_DISABLE_TIME_CHECKS = 1 << 6,
		GNUTLS_VERIFY_DISABLE_TRUSTED_TIME_CHECKS = 1 << 7,
		GNUTLS_VERIFY_DO_NOT_ALLOW_X509_V1_CA_CRT = 1 << 8,
		GNUTLS_VERIFY_DISABLE_CRL_CHECKS = 1 << 9,
		GNUTLS_VERIFY_ALLOW_UNSORTED_CHAIN = 1 << 10,
		GNUTLS_VERIFY_DO_NOT_ALLOW_UNSORTED_CHAIN = 1 << 11,
		GNUTLS_VERIFY_DO_NOT_ALLOW_WILDCARDS = 1 << 12,
		GNUTLS_VERIFY_USE_TLS1_RSA = 1 << 13,
		GNUTLS_VERIFY_IGNORE_UNKNOWN_CRIT_EXTENSIONS = 1 << 14,
		GNUTLS_VERIFY_ALLOW_SIGN_WITH_SHA1 = 1 << 15,
		GNUTLS_VERIFY_RSA_PSS_FIXED_SALT_LENGTH = 1 << 16
		/* cannot exceed 2^24 due to GNUTLS_PROFILE_TO_VFLAGS() */
	}

	/**
	 * gnutls_certificate_verification_profiles_t:
	 * @GNUTLS_PROFILE_UNKNOWN: An invalid/unknown profile.
	 * @GNUTLS_PROFILE_VERY_WEAK: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_VERY_WEAK (64 bits)
	 * @GNUTLS_PROFILE_LOW: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_LOW (80 bits)
	 * @GNUTLS_PROFILE_LEGACY: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_LEGACY (96 bits)
	 * @GNUTLS_PROFILE_MEDIUM: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_MEDIUM (112 bits)
	 * @GNUTLS_PROFILE_HIGH: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_HIGH (128 bits)
	 * @GNUTLS_PROFILE_ULTRA: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_ULTRA (192 bits)
	 * @GNUTLS_PROFILE_FUTURE: A verification profile that
	 *  corresponds to @GNUTLS_SEC_PARAM_FUTURE (256 bits)
	 * @GNUTLS_PROFILE_SUITEB128: A verification profile that
	 *  applies the SUITEB128 rules
	 * @GNUTLS_PROFILE_SUITEB192: A verification profile that
	 *  applies the SUITEB192 rules
	 *
	 * Enumeration of different certificate verification profiles.
	*/
	enum CertificateVerificationProfilesT : uint {
		GNUTLS_PROFILE_UNKNOWN = 0,
		GNUTLS_PROFILE_VERY_WEAK = 1,
		GNUTLS_PROFILE_LOW = 2,
		GNUTLS_PROFILE_LEGACY = 4,
		GNUTLS_PROFILE_MEDIUM = 5,
		GNUTLS_PROFILE_HIGH = 6,
		GNUTLS_PROFILE_ULTRA = 7,
		GNUTLS_PROFILE_FUTURE = 8,
		GNUTLS_PROFILE_SUITEB128 = 32,
		GNUTLS_PROFILE_SUITEB192 = 33
		/*GNUTLS_PROFILE_MAX=255*/
	}

	//#define GNUTLS_PROFILE_TO_VFLAGS(x) \
	//	(((unsigned) x)<<24)

	//#define GNUTLS_VFLAGS_PROFILE_MASK (0xff000000)

	//#define GNUTLS_VFLAGS_TO_PROFILE(x) \
	//	((((unsigned) x)>>24)&0xff)


	/**
	 * gnutls_certificate_type_t:
	 * @GNUTLS_CRT_UNKNOWN: Unknown certificate type.
	 * @GNUTLS_CRT_X509: X.509 Certificate.
	 * @GNUTLS_CRT_OPENPGP: OpenPGP certificate.
	 * @GNUTLS_CRT_RAWPK: Raw public-key (SubjectPublicKeyInfo)
	 *
	 * Enumeration of different certificate types.
	*/
	enum CertificateTypeT : uint {
		GNUTLS_CRT_UNKNOWN = 0,
		GNUTLS_CRT_X509 = 1,
		GNUTLS_CRT_OPENPGP = 2,
		GNUTLS_CRT_RAWPK = 3,
		GNUTLS_CRT_MAX = GNUTLS_CRT_RAWPK
	}

	/**
	 * gnutls_x509_crt_fmt_t:
	 * @GNUTLS_X509_FMT_DER: X.509 certificate in DER format (binary).
	 * @GNUTLS_X509_FMT_PEM: X.509 certificate in PEM format (text).
	 *
	 * Enumeration of different certificate encoding formats.
	*/
	enum X509CrtFmtT : uint {
		GNUTLS_X509_FMT_DER = 0,
		GNUTLS_X509_FMT_PEM = 1
	}

	/**
	 * gnutls_certificate_print_formats_t:
	 * @GNUTLS_CRT_PRINT_FULL: Full information about certificate.
	 * @GNUTLS_CRT_PRINT_FULL_NUMBERS: Full information about certificate and include easy to parse public key parameters.
	 * @GNUTLS_CRT_PRINT_COMPACT: Information about certificate name in one line, plus identification of the public key.
	 * @GNUTLS_CRT_PRINT_ONELINE: Information about certificate in one line.
	 * @GNUTLS_CRT_PRINT_UNSIGNED_FULL: All info for an unsigned certificate.
	 *
	 * Enumeration of different certificate printing variants.
	 */
	enum CertificatePrintFormatsT : int {
		GNUTLS_CRT_PRINT_FULL = 0,
		GNUTLS_CRT_PRINT_ONELINE = 1,
		GNUTLS_CRT_PRINT_UNSIGNED_FULL = 2,
		GNUTLS_CRT_PRINT_COMPACT = 3,
		GNUTLS_CRT_PRINT_FULL_NUMBERS = 4
	}

	/**
	 * gnutls_ctype_target_t:
	 * @GNUTLS_CTYPE_CLIENT: for requesting client certificate type values.
	 * @GNUTLS_CTYPE_SERVER: for requesting server certificate type values.
	 * @GNUTLS_CTYPE_OURS: for requesting our certificate type values.
	 * @GNUTLS_CTYPE_PEERS: for requesting the peers' certificate type values.
	 *
	 * Enumeration of certificate type targets with respect to asymmetric
	 * certificate types as specified in RFC7250 and P2P connection set up
	 * as specified in draft-vanrein-tls-symmetry-02.
	 */
	enum CtypeTargetT : uint {
		GNUTLS_CTYPE_CLIENT,
		GNUTLS_CTYPE_SERVER,
		GNUTLS_CTYPE_OURS,
		GNUTLS_CTYPE_PEERS
	}

	/**
	 * gnutls_pk_algorithm_t:
	 * @GNUTLS_PK_UNKNOWN: Unknown public-key algorithm.
	 * @GNUTLS_PK_RSA: RSA public-key algorithm.
	 * @GNUTLS_PK_RSA_PSS: RSA public-key algorithm, with PSS padding.
	 * @GNUTLS_PK_DSA: DSA public-key algorithm.
	 * @GNUTLS_PK_DH: Diffie-Hellman algorithm. Used to generate parameters.
	 * @GNUTLS_PK_ECDSA: Elliptic curve algorithm. These parameters are compatible with the ECDSA and ECDH algorithm.
	 * @GNUTLS_PK_ECDH_X25519: Elliptic curve algorithm, restricted to ECDH as per rfc7748.
	 * @GNUTLS_PK_EDDSA_ED25519: Edwards curve Digital signature algorithm. Used with SHA512 on signatures.
	 * @GNUTLS_PK_GOST_01: GOST R 34.10-2001 algorithm per rfc5832.
	 * @GNUTLS_PK_GOST_12_256: GOST R 34.10-2012 algorithm, 256-bit key per rfc7091.
	 * @GNUTLS_PK_GOST_12_512: GOST R 34.10-2012 algorithm, 512-bit key per rfc7091.
	 * @GNUTLS_PK_ECDH_X448: Elliptic curve algorithm, restricted to ECDH as per rfc7748.
	 * @GNUTLS_PK_EDDSA_ED448: Edwards curve Digital signature algorithm. Used with SHAKE256 on signatures.
	 *
	 * Enumeration of different public-key algorithms.
	*/
	enum PkAlgorithmT : uint {
		GNUTLS_PK_UNKNOWN = 0,
		GNUTLS_PK_RSA = 1,
		GNUTLS_PK_DSA = 2,
		GNUTLS_PK_DH = 3,
		GNUTLS_PK_ECDSA = 4,
		GNUTLS_PK_ECDH_X25519 = 5,
		GNUTLS_PK_RSA_PSS = 6,
		GNUTLS_PK_EDDSA_ED25519 = 7,
		GNUTLS_PK_GOST_01 = 8,
		GNUTLS_PK_GOST_12_256 = 9,
		GNUTLS_PK_GOST_12_512 = 10,
		GNUTLS_PK_ECDH_X448 = 11,
		GNUTLS_PK_EDDSA_ED448 = 12,
		GNUTLS_PK_MAX = GNUTLS_PK_EDDSA_ED448
	}
}
