using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for WuFTPd FTP servers
	/// </summary>
	internal class WuFtpdServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.WuFTPd;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect WuFTPd server
			// Welcome message: "FTP server (Revision 9.0 Version wuftpd-2.6.1 Mon Jun 30 09:28:28 GMT 2014) ready"
			// Welcome message: "220 DH FTP server (Version wu-2.6.2-5) ready"
			if (message.Contains("Version wuftpd") || message.Contains("Version wu-")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public override bool RecursiveList() {

			// No support, per: http://wu-ftpd.therockgarden.ca/man/ftpd.html
			return false;
		}

		/// <summary>
		/// Return your FTP server's default capabilities.
		/// Used if your server does not broadcast its capabilities using the FEAT command.
		/// </summary>
		public override string[] DefaultCapabilities() {

			// HP-UX version of wu-ftpd 2.6.1
			// http://nixdoc.net/man-pages/HP-UX/ftpd.1m.html

			// assume the basic features supported
			return new[] { "ABOR", "ACCT", "ALLO", "APPE", "CDUP", "CWD", "DELE", "EPSV", "EPRT", "HELP", "LIST", "LPRT", "LPSV", "MKD", "MDTM", "MODE", "NLST", "NOOP", "PASS", "PASV", "PORT", "PWD", "QUIT", "REST", "RETR", "RMD", "RNFR", "RNTO", "SITE", "SIZE", "STAT", "STOR", "STOU", "STRU", "SYST", "TYPE" };

		}

	}
}
