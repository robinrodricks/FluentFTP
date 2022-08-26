using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for WS_FTP servers
	/// </summary>
	internal class WSFTPServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.WSFTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect FTP2S3 server
			// Welcome message: "220 ***.com X2 WS_FTP Server 8.5.0(24135676)"
			if (message.Contains("WS_FTP Server")) {
				return true;
			}
			return false;
		}

	}
}