using System.Collections.Generic;
using SysSslProtocols = System.Security.Authentication.SslProtocols;

namespace FluentFTP.Model.Functions {

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
		/// If true, then try the very rarely used Implicit FTP mode.
		/// </summary>
		public bool IncludeImplicit { get; set; } = true;

		/// <summary>
		/// If true, timeouts will lead to an exception, otherwise we will try the next profile.
		/// </summary>
		public bool AbortOnTimeout { get; set; } = true;

		/// <summary>
		/// If true, then we will not try the insecure FTP unencrypted mode, and only try FTPS.
		/// If false, then both FTP and FTPS will be tried.
		/// </summary>
		public bool RequireEncryption { get; set; } = false;

		/// <summary>
		/// List of protocols to be tried, and the order they should be tried in.
		/// </summary>
		public List<SysSslProtocols> ProtocolPriority { get; set; } = new List<SysSslProtocols> {
			SysSslProtocols.Tls11 | SysSslProtocols.Tls12,
			// Do not EVER use "Default". It boils down to "SSL or TLS1.0" or worse.
			// Do not use "None" - it can connect to TLS13, but Session Resume won't work, so a successful AutoDetect will be a false truth.
		};

	}
}