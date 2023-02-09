using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Enums {

	/// <summary>
	/// Security profile to use for GnuTLS protocol handler.
	/// Source : https://www.gnutls.org/manual/gnutls.html#tab_003akey_002dsizes
	/// </summary>
	public enum GnuProfile : int {

		/// <summary>
		/// No profile specified, so configuration is based on the `SecuritySuite` setting.
		/// </summary>
		None,
		/// <summary>
		/// Very short term protection against agencies (corresponds to ENISA legacy level)
		/// </summary>
		Low,
		/// <summary>
		/// Legacy standard level
		/// </summary>
		Legacy,
		/// <summary>
		/// Medium-term protection
		/// </summary>
		Medium,
		/// <summary>
		/// Long term protection (corresponds to ENISA future level)
		/// </summary>
		High,
		/// <summary>
		/// Even longer term protection
		/// </summary>
		Ultra,
		/// <summary>
		/// Foreseeable future
		/// </summary>
		Future,
		NsaSuiteB128,
		NsaSuiteB192,

	}
}
