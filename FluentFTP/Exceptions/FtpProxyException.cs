using System;

namespace FluentFTP.Exceptions {

	/// <summary>
	/// FtpProxyException
	/// </summary>
	public class FtpProxyException : FtpException {

		/// <summary>
		/// FtpProxyException
		/// </summary>
		public FtpProxyException() : base("Exception with a FTP proxy server.") {
		}

		/// <summary>
		/// FtpProxyException
		/// </summary>
		public FtpProxyException(string message)
			: base(message) {
		}

		/// <summary>
		/// FtpProxyException
		/// </summary>
		public FtpProxyException(string message, Exception inner)
			: base(message, inner) {
		}
	}
}