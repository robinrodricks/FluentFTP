using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for ProFTPD FTP servers
	/// </summary>
	internal class ProFtpdServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.ProFTPD;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect ProFTPd server
			// Welcome message: "ProFTPD 1.3.5rc3 Server (***) [::ffff:***]"
			if (message.Contains("ProFTPD")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public override bool RecursiveList() {

			// Has support, per: http://www.proftpd.org/docs/howto/ListOptions.html
			return true;
		}

		/// <summary>
		/// Perform server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override bool DeleteDirectory(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options) {

			// Support #378 - Support RMDIR command for ProFTPd
			if (deleteContents && client.HasFeature(FtpCapability.SITE_RMDIR)) {
				if ((client.Execute("SITE RMDIR " + ftppath)).Success) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE RMDIR command to quickly delete directory: " + ftppath);
					return true;
				}
				else {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE RMDIR command to quickly delete directory: " + ftppath);
				}
			}

			return false;
		}

		/// <summary>
		/// Perform async server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override async Task<bool> DeleteDirectoryAsync(AsyncFtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options, CancellationToken token) {

			// Support #378 - Support RMDIR command for ProFTPd
			if (deleteContents && client.HasFeature(FtpCapability.SITE_RMDIR)) {
				if ((await client.Execute("SITE RMDIR " + ftppath, token)).Success) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE RMDIR command to quickly delete: " + ftppath);
					return true;
				}
				else {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE RMDIR command to quickly delete: " + ftppath);
				}
			}

			return false;
		}

		/// <summary>
		/// Perform server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override bool CreateDirectory(FtpClient client, string path, string ftppath, bool force) {

			// Support #378 - Support MKDIR command for ProFTPd
			if (client.HasFeature(FtpCapability.SITE_MKDIR)) {
				if ((client.Execute("SITE MKDIR " + ftppath)).Success) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE MKDIR command to quickly create: " + ftppath);
					return true;
				}
				else {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE MKDIR command to quickly create: " + ftppath);
				}
			}

			return false;
		}

		/// <summary>
		/// Perform async server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override async Task<bool> CreateDirectoryAsync(AsyncFtpClient client, string path, string ftppath, bool force, CancellationToken token) {

			// Support #378 - Support MKDIR command for ProFTPd
			if (client.HasFeature(FtpCapability.SITE_MKDIR)) {
				if ((await client.Execute("SITE MKDIR " + ftppath, token)).Success) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Used the server-specific SITE MKDIR command to quickly create: " + ftppath);
					return true;
				}
				else {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Failed to use the server-specific SITE MKDIR command to quickly create: " + ftppath);
				}
			}

			return false;
		}

	}
}
