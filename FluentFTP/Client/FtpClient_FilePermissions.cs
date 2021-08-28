using System;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient : IFtpClient, IDisposable {

		#region File Permissions / Chmod

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

#if !CORE14
			lock (m_lock) {
#endif
				path = path.GetFtpPath();

				LogFunc(nameof(SetFilePermissions), new object[] { path, permissions });

				if (!(reply = Execute("SITE CHMOD " + permissions.ToString() + " " + path)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
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

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="permissions">The permissions in CHMOD format</param>
		public void Chmod(string path, int permissions) {
			SetFilePermissions(path, permissions);
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
		public Task ChmodAsync(string path, int permissions, CancellationToken token = default(CancellationToken)) {
			return SetFilePermissionsAsync(path, permissions, token);
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
		/// <param name="owner">The owner permissions</param>
		/// <param name="group">The group permissions</param>
		/// <param name="other">The other permissions</param>
		public void Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other) {
			SetFilePermissions(path, owner, group, other);
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
		public Task ChmodAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken)) {
			return SetFilePermissionsAsync(path, owner, group, other, token);
		}
#endif

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
		/// Throws FtpCommandException if there is an issue.
		/// Returns null if the server did not specify a permission value.
		/// Use `GetChmod` if you required the integer value instead.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		public FtpListItem GetFilePermissions(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetFilePermissions), new object[] { path });

			var result = GetObjectInfo(path);

			return result;
		}

#if ASYNC
		/// <summary>
		/// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
		/// Throws FtpCommandException if there is an issue.
		/// Returns null if the server did not specify a permission value.
		/// Use `GetChmod` if you required the integer value instead.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task<FtpListItem> GetFilePermissionsAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetFilePermissionsAsync), new object[] { path });

			var result = await GetObjectInfoAsync(path, false, token);

			return result;
		}
#endif

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an integer in the CHMOD format.
		/// Throws FtpCommandException if there is an issue.
		/// Returns 0 if the server did not specify a permission value.
		/// Use `GetFilePermissions` if you required the permissions in the FtpPermission format.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		public int GetChmod(string path) {
			var item = GetFilePermissions(path);
			return item != null ? item.Chmod : 0;
		}

#if ASYNC
		/// <summary>
		/// Retrieve the permissions of the given file/folder as an integer in the CHMOD format.
		/// Throws FtpCommandException if there is an issue.
		/// Returns 0 if the server did not specify a permission value.
		/// Use `GetFilePermissions` if you required the permissions in the FtpPermission format.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task<int> GetChmodAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpListItem item = await GetFilePermissionsAsync(path, token);
			return item != null ? item.Chmod : 0;
		}
#endif

		#endregion

	}
}