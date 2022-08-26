using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for FTP2S3Gateway FTP servers
	/// </summary>
	internal class Ftp2S3GatewayServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.FTP2S3Gateway;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect FTP2S3 server
			// Welcome message: "220 Aruba FTP2S3 gateway 1.0.1 ready"
			if (message.Contains("FTP2S3 gateway")) {
				return true;
			}
			return false;
		}

	}
}
