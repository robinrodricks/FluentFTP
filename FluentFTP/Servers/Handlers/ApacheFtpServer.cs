using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for Apache (MINA) FTP servers
	/// </summary>
	internal class ApacheFtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.Apache;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public override bool DetectBySyst(string message) {

			// Detect Apache server
			// SYST type: "UNIX Type: Apache FtpServer"
			if (message.Contains("Apache FtpServer")) {
				return true;
			}

			return false;
		}

	}
}
