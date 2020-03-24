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
	/// Server-specific handling for OpenVMS FTP servers
	/// </summary>
	public class OpenVmsServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public virtual FtpServer ToEnum() {
			return FtpServer.OpenVMS;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectedByWelcome(string message) {

			// Detect OpenVMS server
			// Welcome message: "220 ftp.bedrock.net FTP-OpenVMS FTPD V5.5-3 (c) 2001 Process Software"
			if (message.Contains("OpenVMS FTPD")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public override bool DetectedBySyst(string message) {

			// Detect OpenVMS server
			// SYST type: "VMS OpenVMS V8.4"
			if (message.Contains("OpenVMS")) {
				return true;
			}

			return false;
		}

	}
}
