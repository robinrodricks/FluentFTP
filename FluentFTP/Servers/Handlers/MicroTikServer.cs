using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for MikroTik RouterOS FTP servers
	/// </summary>
	internal class MicroTikServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.MikroTik;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect MikroTik server
			// Welcome message: "MikroTik FTP server (MikroTik 2.9.27) ready"
			// Welcome message: "MikroTik FTP server (MikroTik 6.0rc2) ready"
			if (message.Contains("MikroTik FTP")) {
				return true;
			}

			return false;
		}

	}
}