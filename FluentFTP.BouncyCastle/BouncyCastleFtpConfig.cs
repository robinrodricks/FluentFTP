using System;
using FluentFTP.Streams;

namespace FluentFTP.BouncyCastle {
	/// <summary>Configures the Bouncy Castle TLS stream used by FluentFTP.</summary>
	public sealed class BouncyCastleFtpConfig : IFtpStreamConfig {
		/// <summary>
		/// Gets or sets a value indicating whether a data connection must fail when the server does not
		/// resume the control connection's TLS session.
		/// </summary>
		public bool RequireSessionResumption { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether TLS 1.2 sessions without RFC 7627 Extended Master
		/// Secret may be resumed. Keep disabled unless a known legacy server requires it.
		/// </summary>
		public bool AllowLegacyResumption { get; set; }

		/// <summary>Gets or sets an optional callback for non-sensitive connection diagnostics.</summary>
		public Action<string>? Diagnostic { get; set; }
	}
}
