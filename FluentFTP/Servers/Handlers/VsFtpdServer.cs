using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for VsFTPd FTP servers
	/// </summary>
	internal class VsFtpdServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.VsFTPd;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect vsFTPd server
			// Welcome message: "(vsFTPd 3.0.3)"
			if (message.Contains("(vsFTPd")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public override bool RecursiveList() {

			// Has support, but OFF by default, per: https://linux.die.net/man/5/vsftpd.conf
			return false; // impossible to detect on a server-by-server basis
		}

	}
}
