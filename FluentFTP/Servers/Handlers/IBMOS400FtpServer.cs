using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for IBMOS400FTP servers
	/// </summary>
	internal class IBMOS400FtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.IBMOS400FTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect IBM OS/400 server
			// Welcome message: "220-QTCP at xxxxxx.xxxxx.xxx.com."
			if (message.Contains("QTCP at")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public override FtpParser GetParser() {
			return FtpParser.IBMOS400;
		}

		/// <summary>
		/// Return true if your server requires custom handling to handle listing analysis.
		/// </summary>
		public override bool IsCustomCalculateFullFtpPath() {
			return true;
		}

		/// <summary>
		/// Get the full path of a given FTP Listing entry
		/// Return null indicates custom code decided not to handle this
		/// </summary>
		public override bool? CalculateFullFtpPath(BaseFtpClient client, string path, FtpListItem item) {

			// If item.name is in the library/filename format, check if the current
			// working directory ends with that library name and then do not
			// duplicate that library name in the fullname.

			var parts = item.Name.Split('/');
			if (parts.Length < 2 || !path.EndsWith(parts[0])) {
				return null;
			}

			item.Name = parts[1];
			item.FullName = path + '/' + parts[1];

			return true;
		}

	}
}
