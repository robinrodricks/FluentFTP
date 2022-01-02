using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Servers;
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for IBMzOSFTP servers
	/// </summary>
	public class IbmZosFtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.IBMzOSFTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect IBM z/OS server
			// Welcome message: "220-FTPD1 IBM FTP CS V2R3 at mysite.gov, 16:51:54 on 2019-12-12."
			if (message.Contains("IBM FTP CS")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public override FtpParser GetParser() {
			return FtpParser.IBMzOS;
		}

		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override void AfterConnected(FtpClient client) {
			FtpReply reply;
			if (!(reply = client.Execute("SITE DATASETMODE")).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = client.Execute("SITE QUOTESOVERRIDE")).Success) {
				throw new FtpCommandException(reply);
			}
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override async Task AfterConnectedAsync(FtpClient client, CancellationToken token) {
			FtpReply reply;
			if (!(reply = await client.ExecuteAsync("SITE DATASETMODE", token)).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = await client.ExecuteAsync("SITE QUOTESOVERRIDE", token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif
	}
}
