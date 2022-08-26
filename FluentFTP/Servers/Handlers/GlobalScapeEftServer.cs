using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for GlobalScapeEFT FTP servers
	/// </summary>
	internal class GlobalScapeEftServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.GlobalScapeEFT;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect GlobalScape EFT server
			// Welcome message: "EFT Server Enterprise 7.4.5.6"
			if (message.Contains("EFT Server")) {
				return true;
			}

			return false;
		}

	}
}
