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
	/// Server-specific handling for Huawei FTP servers
	/// </summary>
	internal class HuaweiServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.Huawei;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect Huawei server
			// Welcome message: "220 HG510a FTP version 1.0 ready"
			// Welcome message: "220 HG520b FTP version 1.0 ready"
			// Welcome message: "220 HG530 FTP version 1.0 ready"
			if (message.Contains("FTP version 1.0")) {
				if (message.Contains("HG51") || message.Contains("HG52")
					|| message.Contains("HG53") || message.Contains("HG54")) {
					return true;
				}
			}

			return false;
		}

	}
}