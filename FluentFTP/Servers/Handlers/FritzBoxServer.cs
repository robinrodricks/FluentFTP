using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for FritzBox FTP servers
	/// </summary>
	internal class FritzBoxServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.FritzBox;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect FTP2S3 server
			// Welcome message: "220 FRITZ!Box7490 FTP server ready"
			// Welcome message: "220 FRITZ!BoxFonWLAN7390 FTP server ready"
			if (message.Contains("FRITZ!Box")) {
				return true;
			}
			return false;
		}

	}
}