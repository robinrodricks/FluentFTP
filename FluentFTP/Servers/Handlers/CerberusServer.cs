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
	/// Server-specific handling for Cerberus FTP servers
	/// </summary>
	public class CerberusServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public virtual FtpServer ToEnum() {
			return FtpServer.Cerberus;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectedByWelcome(string message) {

			// Detect Cerberus server
			// Welcome message: "220-Cerberus FTP Server Personal Edition"
			if (message.Contains("Cerberus FTP")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public override bool DetectedBySyst(string message) {
			return false;
		}

	}
}
