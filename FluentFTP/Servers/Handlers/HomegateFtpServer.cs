using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for HomegateFTP servers
	/// </summary>
	internal class HomegateFtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.HomegateFTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect Homegate FTP server
			// Welcome message: "220 Homegate FTP Server ready"
			if (message.Contains("Homegate FTP Server")) {
				return true;
			}

			return false;
		}

	}
}
