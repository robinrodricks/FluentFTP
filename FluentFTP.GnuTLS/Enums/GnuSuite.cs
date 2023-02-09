using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Enums {
	/// <summary>
	/// Security suite to use for GnuTLS protocol handler.
	/// Source : https://www.gnutls.org/manual/gnutls.html#tab_003aprio_002dkeywords
	/// </summary>
	public enum GnuSuite : int {
		/// <summary>
		/// Enable all secure ciphersuites, limited to 128 bit ciphers and sorted by terms of speed performance. The message authenticity security level is of 64 bits or more, and the certificate verification profile is set to GNUTLS_PROFILE_LOW (80-bits).
		/// </summary>
		Performance,
		/// <summary>
		/// Enable all secure ciphersuites. The ciphers are sorted by security margin, although the 256-bit ciphers are included as a fallback only. The message authenticity security level is of 64 bits or more, and the certificate verification profile is set to GNUTLS_PROFILE_LOW (80-bits).
		/// This priority string implicitly enables ECDHE and DHE. The ECDHE ciphersuites are placed first in the priority order, but due to compatibility issues with the DHE ciphersuites they are placed last in the priority order, after the plain RSA ciphersuites. 
		/// </summary>
		Normal,
		/// <summary>
		/// This sets the NORMAL settings that were used for GnuTLS 3.2.x or earlier. There is no verification profile set, and the allowed DH primes are considered weak today (but are often used by misconfigured servers).
		/// </summary>
		Legacy,
		/// <summary>
		/// Enable all secure ciphersuites that support perfect forward secrecy (ECDHE and DHE). The ciphers are sorted by security margin, although the 256-bit ciphers are included as a fallback only. The message authenticity security level is of 80 bits or more, and the certificate verification profile is set to GNUTLS_PROFILE_LOW (80-bits). This option is available since 3.2.4 or later.
		/// </summary>
		PerfectForwardSecrecy,
		/// <summary>
		/// Enable all known to be secure ciphersuites that offer a security level 128-bit or more. The message authenticity security level is of 80 bits or more, and the certificate verification profile is set to GNUTLS_PROFILE_LOW (80-bits).
		/// </summary>
		Secure128,
		/// <summary>
		/// Enable all secure ciphersuites that offer a security level 192-bit or more. The message authenticity security level is of 128 bits or more, and the certificate verification profile is set to GNUTLS_PROFILE_HIGH (128-bits).
		/// </summary>
		Secure192,
		/// <summary>
		/// Currently alias for SECURE192. This option, will enable ciphers which use a 256-bit key but, due to limitations of the TLS protocol, the overall security level will be 192-bits (the security level depends on more factors than cipher key size).
		/// </summary>
		Secure256,
		/// <summary>
		/// Enable all the NSA Suite B cryptography (RFC5430) ciphersuites with an 128 bit security level, as well as the enabling of the corresponding verification profile.
		/// </summary>
		NsaSuiteB128,
		/// <summary>
		/// Enable all the NSA Suite B cryptography (RFC5430) ciphersuites with an 192 bit security level, as well as the enabling of the corresponding verification profile.
		/// </summary>
		NsaSuiteB192,
		/// <summary>
		/// Nothing is enabled. You will have to manually add `SecurityOptions` and specify the security features that you want enabled. 
		/// </summary>
		None,
	}
}
