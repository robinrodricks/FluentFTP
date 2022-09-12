using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for CrushFTP servers
	/// </summary>
	internal class CrushFtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.CrushFTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect CrushFTP server
			// Welcome message: "220 CrushFTP Server Ready!"
			if (message.Contains("CrushFTP Server")) {
				return true;
			}

			return false;
		}

	}
}
