using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

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
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public override bool DetectBySyst(string message) {

			// Detect IBM OS/400 server
			// SYST type: "OS/400 is the remote operating system..."
			if (message.Contains("OS/400")) {
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
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override void AfterConnected(FtpClient client) {
			FtpReply reply;
			if (!(reply = client.Execute("SITE LISTFMT 1")).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = client.Execute("SITE NAMEFMT 1")).Success) {
				throw new FtpCommandException(reply);
			}
		}

		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override async Task AfterConnectedAsync(AsyncFtpClient client, CancellationToken token) {
			FtpReply reply;
			if (!(reply = await client.Execute("SITE LISTFMT 1", token)).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = await client.Execute("SITE NAMEFMT 1", token)).Success) {
				throw new FtpCommandException(reply);
			}
		}

	}
}
