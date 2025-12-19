using System.Net.Security;
using FluentFTP;

namespace FluentFTP.Client.BaseClient {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public partial class BaseFtpClient {

		/// <summary>
		/// Fires the SSL client authentication options configuration event
		/// </summary>
		/// <param name="options">The SSL client authentication options to be customized</param>
		internal void OnConfigureAuthentication(SslClientAuthenticationOptions options) {
			var evt = m_ConfigureAuthentication;

			if (evt != null) {
				var e = new FtpSslAuthenticationEventArgs(options);
				evt(this, e);
			}
		}

	}
#endif
}

