using System;

namespace FluentFTP.Exceptions {
	public class FtpProxyException : FtpException {
		public FtpProxyException() : base("Exception with a FTP proxy server.") {
		}

		public FtpProxyException(string message)
			: base(message) {
		}

		public FtpProxyException(string message, Exception inner)
			: base(message, inner) {
		}
	}
}