using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Enums {
	/// <summary>
	/// Advanced options to configure GnuTLS with.
	/// Source : https://www.gnutls.org/manual/gnutls.html#tab_003aprio_002dspecial1
	/// </summary>
	public enum GnuAdvanced : int {
		/// <summary>
		/// will enable compatibility mode. It might mean that violations of the protocols are allowed as long as maximum compatibility with problematic clients and servers is achieved. More specifically this string will tolerate packets over the maximum allowed TLS record, and add a padding to TLS Client Hello packet to prevent it being in the 256-512 range which is known to be causing issues with a commonly used firewall (see the %DUMBFW option).
		/// </summary>
		CompatibilityMode,
		/// <summary>
		/// will add a private extension with bogus data that make the client hello exceed 512 bytes. This avoids a black hole behavior in some firewalls. This is the [RFC7685] client hello padding extension, also enabled with %COMPAT.
		/// </summary>
		DumbFirewall,
		/// <summary>
		/// will prevent the sending of any TLS extensions in client side. Note that TLS 1.2 requires extensions to be used, as well as safe renegotiation thus this option must be used with care. When this option is set no versions later than TLS1.2 can be negotiated.
		/// </summary>
		NoExtensions,
		/// <summary>
		/// will prevent sending of the TLS status_request extension in client side.
		/// </summary>
		NoStatusRequest,
		/// <summary>
		/// will prevent the advertizing of the TLS session ticket extension.
		/// </summary>
		NoTickets,
		/// <summary>
		/// will prevent the advertizing of the TLS session ticket extension in TLS 1.2. This is implied by the PFS keyword.
		/// </summary>
		NoTicketsTls12,
		/// <summary>
		/// will prevent the advertizing the TLS extended master secret (session hash) extension.
		/// </summary>
		NoSessionHash,
		/// <summary>
		/// The ciphersuite will be selected according to server priorities and not the client’s.
		/// </summary>
		ServerPrecedence,
		/// <summary>
		/// will use SSL3.0 record version in client hello. By default GnuTLS will set the minimum supported version as the client hello record version (do not confuse that version with the proposed handshake version at the client hello).
		/// </summary>
		Ssl3RecordVersion,
		/// <summary>
		/// will use the latest TLS version record version in client hello.
		/// </summary>
		LatestRecordVersion,
		/// <summary>
		/// will disable matching wildcards when comparing hostnames in certificates.
		/// </summary>
		DisableWildcards,
		/// <summary>
		/// will disable the encrypt-then-mac TLS extension (RFC7366). This is implied by the %COMPAT keyword.
		/// </summary>
		NoEncryptThenMac,
		/// <summary>
		/// negotiate CBC ciphersuites only when both sides of the connection support encrypt-then-mac TLS extension (RFC7366).
		/// </summary>
		ForceEncryptThenMac,
		/// <summary>
		/// will completely disable safe renegotiation completely. Do not use unless you know what you are doing.
		/// </summary>
		DisableSafeRenegotiation,
		/// <summary>
		/// will allow handshakes and re-handshakes without the safe renegotiation extension. Note that for clients this mode is insecure (you may be under attack), and for servers it will allow insecure clients to connect (which could be fooled by an attacker). Do not use unless you know what you are doing and want maximum compatibility.
		/// </summary>
		UnsafeRenegotiation,
		/// <summary>
		/// will allow initial handshakes to proceed, but not re-handshakes. This leaves the client vulnerable to attack, and servers will be compatible with non-upgraded clients for initial handshakes. This is currently the default for clients and servers, for compatibility reasons.
		/// </summary>
		PartialRenegotiation,
		/// <summary>
		/// will enforce safe renegotiation. Clients and servers will refuse to talk to an insecure peer. Currently this causes interoperability problems, but is required for full protection.
		/// </summary>
		SafeRenegotiation,
		/// <summary>
		/// will enable the use of the fallback signaling cipher suite value in the client hello. Note that this should be set only by applications that try to reconnect with a downgraded protocol version. See RFC7507 for details.
		/// </summary>
		FallbackScsv,
		/// <summary>
		/// will disable TLS 1.3 middlebox compatibility mode (RFC8446, Appendix D.4) for non-compliant middleboxes.
		/// </summary>
		DisableTls13CompatMode,
		/// <summary>
		/// will allow signatures with known to be broken algorithms (such as MD5 or SHA1) in certificate chains.
		/// </summary>
		Verify_AllowBroken,
		/// <summary>
		/// will allow RSA-MD5 signatures in certificate chains.
		/// </summary>
		Verify_AllowSignRsaMd5,
		/// <summary>
		/// will allow signatures with SHA1 hash algorithm in certificate chains.
		/// </summary>
		Verify_AllowSignWithSha1,
		/// <summary>
		/// will disable CRL or OCSP checks in the verification of the certificate chain.
		/// </summary>
		Verify_DisableCrlChecks,
		/// <summary>
		/// will allow V1 CAs in chains.
		/// </summary>
		Verify_AllowX509V1CaCrt
	}
}
