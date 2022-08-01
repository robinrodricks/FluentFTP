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
	/// Server-specific handling for glFTPd FTP servers
	/// </summary>
	internal class GlFtpdServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.glFTPd;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect glFTPd server
			// Welcome message: "220 W 00 T (glFTPd 2.01 Linux+TLS) ready."
			// Welcome message: "220 <hostname> (glFTPd 2.01 Linux+TLS) ready."
			if (message.Contains("glFTPd ")) {
				return true;
			}

			return false;
		}

	}
}
