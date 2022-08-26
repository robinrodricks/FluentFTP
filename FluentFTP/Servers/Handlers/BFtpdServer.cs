using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for BFTPd FTP servers
	/// </summary>
	internal class BFtpdServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.BFTPd;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect BFTPd server
			// Welcome message: "220 bftpd 2.2.1 at 192.168.1.1 ready"
			if (message.Contains("bftpd ")) {
				return true;
			}

			return false;
		}

	}
}
