using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for NonStop/Tandem FTP servers
	/// </summary>
	internal class NonStopTandemServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.NonStopTandem;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect Tandem/NonStop server
			// Welcome message: "220 mysite.com FTP SERVER T9552H02 (Version H02 TANDEM 11SEP2008) ready."
			// Welcome message: "220 FTP SERVER T9552G08 (Version G08 TANDEM 15JAN2008) ready."
			if (message.Contains("FTP SERVER ") && message.Contains(" TANDEM ")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public override FtpParser GetParser() {
			return FtpParser.NonStop;
		}

	}
}
