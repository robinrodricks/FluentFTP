using System;
using System.Net.Security;
using FluentFTP.Client.BaseClient;

namespace FluentFTP {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// Event is fired when SSL client authentication options need to be configured before the TLS handshake.
	/// This allows customization of SSL options such as CipherSuitesPolicy for legacy FTPS servers.
	/// </summary>
	/// <param name="control">The control connection that triggered the event</param>
	/// <param name="e">Event args containing the SslClientAuthenticationOptions to customize</param>
	public delegate void FtpSslAuthentication(BaseFtpClient control, FtpSslAuthenticationEventArgs e);

	/// <summary>
	/// Event args for the FtpSslClientAuthenticationOptions delegate
	/// </summary>
	public class FtpSslAuthenticationEventArgs : EventArgs {
		private SslClientAuthenticationOptions m_options = null;

		/// <summary>
		/// The SSL client authentication options to be customized.
		/// Modify this object to configure SSL settings such as CipherSuitesPolicy.
		/// </summary>
		public SslClientAuthenticationOptions Options {
			get => m_options;
			set => m_options = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Creates a new instance of FtpSslClientAuthenticationOptionsEventArgs
		/// </summary>
		/// <param name="options">The SSL client authentication options</param>
		public FtpSslAuthenticationEventArgs(SslClientAuthenticationOptions options) {
			m_options = options ?? throw new ArgumentNullException(nameof(options));
		}
	}
#endif
}