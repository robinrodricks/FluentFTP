using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="owner">The owner permissions</param>
		/// <param name="group">The group permissions</param>
		/// <param name="other">The other permissions</param>
		public void SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other) {
			SetFilePermissions(path, Permissions.CalcChmod(owner, group, other));
		}

#if ASYNC
		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="owner">The owner permissions</param>
		/// <param name="group">The group permissions</param>
		/// <param name="other">The other permissions</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task SetFilePermissionsAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken)) {
			return SetFilePermissionsAsync(path, Permissions.CalcChmod(owner, group, other), token);
		}
#endif


		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="permissions">The permissions in CHMOD format</param>
		public void SetFilePermissions(string path, int permissions) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			lock (m_lock) {
				path = path.GetFtpPath();

				LogFunc(nameof(SetFilePermissions), new object[] { path, permissions });

				if (!(reply = Execute("SITE CHMOD " + permissions.ToString() + " " + path)).Success) {
					throw new FtpCommandException(reply);
				}

			}
		}

#if ASYNC
		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="permissions">The permissions in CHMOD format</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetFilePermissionsAsync(string path, int permissions, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(SetFilePermissionsAsync), new object[] { path, permissions });

			if (!(reply = await ExecuteAsync("SITE CHMOD " + permissions.ToString() + " " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif
	}
}
