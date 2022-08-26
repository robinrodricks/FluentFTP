using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for XLight FTP servers
	/// </summary>
	internal class XLightServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.XLight;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect XLight server
			// Welcome message: "220 Xlight FTP Server 3.9 ready"
			if (message.Contains("Xlight FTP Server")) {
				return true;
			}

			return false;
		}


	}
}
