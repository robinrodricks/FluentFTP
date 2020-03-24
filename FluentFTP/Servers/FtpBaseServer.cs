using System;
using System.Collections.Generic;
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers {

	/// <summary>
	/// The base class used for all FTP server specific support.
	/// </summary>
	public abstract class FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public virtual FtpServer ToEnum() {
			return FtpServer.Unknown;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public virtual bool DetectedByWelcome(string message) {
			return false;
		}

		/// <summary>
		/// Return true if your server is detected by the given SYST response message.
		/// Its a fallback method if the server did not send an identifying welcome message.
		/// </summary>
		public virtual bool DetectedBySyst(string message) {
			return false;
		}

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public virtual bool SupportsRecursiveList() {
			return false;
		}

		/// <summary>
		/// Return your FTP server's default capabilities.
		/// Used if your server does not broadcast its capabilities using the FEAT command.
		/// </summary>
		public virtual string[] AssumeCapabilities(List<FtpCapability> capabilities) {
			return null;
		}

		/// <summary>
		/// Return true if the path is an absolute path according to your server's convention.
		/// </summary>
		public virtual bool IsAbsolutePath(FtpClient client, string path) {
			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public virtual FtpParser GetParser(FtpClient client) {
			return FtpParser.Unix;
		}

		/// <summary>
		/// Perform server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual bool ServerDeleteDirectory(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options) {
			return false;
		}

#if ASYNC
		/// <summary>
		/// Perform async server-specific delete directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual async Task<bool> ServerDeleteDirectoryAsync(FtpClient client, string path, string ftppath, bool deleteContents, FtpListOption options, CancellationToken token) {
			return false;
		}
#endif

		/// <summary>
		/// Perform server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual bool ServerCreateDirectory(FtpClient client, string path, string ftppath, bool force) {
			return false;
		}

#if ASYNC
		/// <summary>
		/// Perform async server-specific create directory commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public virtual async Task<bool> ServerCreateDirectoryAsync(FtpClient client, string path, string ftppath, bool force, CancellationToken token) {
			return false;
		}
#endif

	}
}
