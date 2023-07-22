using System.Collections.Generic;
using SysSslProtocols = System.Security.Authentication.SslProtocols;

namespace FluentFTP {

	public class FtpAutoDetectConfig {

		/// <summary>
		/// Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)
		/// </summary>
		public bool CloneConnection { get; set; } = true;

		/// <summary>
		/// Find all successful profiles (false) or stop after finding the first successful profile (true)
		/// </summary>
		public bool FirstOnly { get; set; } = true;

		/// <summary>
		/// Also try the very seldom used Implicit mode
		/// Default is try implicit, otherwise many would need a code change
		/// as per the previous versions of AutoDetect.
		/// Recommendation: Change this default to "false"
		/// </summary>
		public bool IncludeImplicit { get; set; } = true;

		/// <summary>
		/// Do not try the insecure unencrypted mode (even if it might work)
		/// Default is allow insecure, otherwise many would need a code change
		/// as per the previous versions of AutoDetect.
		/// </summary>
		public bool RequireEncryption { get; set; } = false;
		/// Recommendation: Change this default to "true"

		public List<SysSslProtocols> ProtocolPriority = new List<SysSslProtocols> {
			SysSslProtocols.Tls11 | SysSslProtocols.Tls12,
			// Do not EVER use "Default". It boils down to "SSL or TLS1.0" or worse.
			// Do not use "None" - it can connect to TLS13, but Session Resume won't work, so a successful AutoDetect will be a false truth.
		};


	}
}