using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for Rumpus FTP servers
	/// </summary>
	internal class RumpusServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.Rumpus;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect Rumpus server
			// Welcome message: "Response: 220-Welcome To Rumpus!"
			//					"Response: 220 Service ready for new user"
			if (message.Contains("Welcome To Rumpus")) {
				return true;
			}

			return false;
		}

	}
}