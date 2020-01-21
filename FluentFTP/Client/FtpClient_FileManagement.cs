using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IFtpClient, IDisposable {
		#region Delete File

		/// <summary>
		/// Deletes a file on the server
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <example><code source="..\Examples\DeleteFile.cs" lang="cs" /></example>
		public void DeleteFile(string path) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc("DeleteFile", new object[] { path });

				if (!(reply = Execute("DELE " + path.GetFtpPath())).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

#if !CORE
		private delegate void AsyncDeleteFile(string path);

		/// <summary>
		/// Begins an asynchronous operation to delete the specified file on the server
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteFile(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDeleteFile func;

			lock (m_asyncmethods) {
				ar = (func = DeleteFile).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginDeleteFile"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDeleteFile</param>
		/// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
		public void EndDeleteFile(IAsyncResult ar) {
			GetAsyncDelegate<AsyncDeleteFile>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Deletes a file from the server asynchronously
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="token">Cancellation Token</param>
		public async Task DeleteFileAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(DeleteFileAsync), new object[] { path });

			if (!(reply = await ExecuteAsync("DELE " + path.GetFtpPath(), token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif

		#endregion

		#region File Exists

		/// <summary>
		/// Checks if a file exists on the server.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <returns>True if the file exists</returns>
		/// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
		public bool FileExists(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

#if !CORE14
			lock (m_lock) {
#endif

				LogFunc("FileExists", new object[] { path });

				// calc the absolute filepath
				path = GetAbsolutePath(path.GetFtpPath());

				// since FTP does not include a specific command to check if a file exists
				// here we check if file exists by attempting to get its filesize (SIZE)
				if (HasFeature(FtpCapability.SIZE)) {
					// Fix #328: get filesize in ASCII or Binary mode as required by server
					var sizeReply = new FtpSizeReply();
					GetFileSizeInternal(path, sizeReply);
					if (sizeReply.Reply.Code[0] == '2') {
						return true;
					}

					if (sizeReply.Reply.Code[0] == '5' && sizeReply.Reply.Message.IsKnownError(fileNotFoundStrings)) {
						return false;
					}

					// Fix #179: Server returns 550 if file not found or no access to file
					if (sizeReply.Reply.Code.Substring(0, 3) == "550") {
						return false;
					}
				}

				// check if file exists by attempting to get its date modified (MDTM)
				if (HasFeature(FtpCapability.MDTM)) {
					var reply = Execute("MDTM " + path);
					var ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}

					if (ch == '5' && reply.Message.IsKnownError(fileNotFoundStrings)) {
						return false;
					}
				}

				// check if file exists by getting a name listing (NLST)
				var fileList = GetNameListing(path.GetFtpDirectoryName());
				return FtpExtensions.FileExistsInNameListing(fileList, path);


				// check if file exists by attempting to download it (RETR)
				/*try {
					Stream stream = OpenRead(path);
					stream.Close();
					return true;
				} catch (FtpException ex) {
				}*/

				return false;
#if !CORE14
			}

#endif
		}

#if !CORE
		private delegate bool AsyncFileExists(string path);

		/// <summary>
		/// Begins an asynchronous operation to check if a file exists on the 
		/// server by taking a  file listing of the parent directory in the path
		/// and comparing the results the path supplied.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
		public IAsyncResult BeginFileExists(string path, AsyncCallback callback, object state) {
			AsyncFileExists func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = FileExists).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginFileExists"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="o:BeginFileExists"/></param>
		/// <returns>True if the file exists, false otherwise</returns>
		/// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
		public bool EndFileExists(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncFileExists>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Checks if a file exists on the server asynchronously.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>True if the file exists, false otherwise</returns>
		public async Task<bool> FileExistsAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(FileExistsAsync), new object[] { path });

			// calc the absolute filepath
			path = await GetAbsolutePathAsync(path.GetFtpPath(), token);

			// since FTP does not include a specific command to check if a file exists
			// here we check if file exists by attempting to get its filesize (SIZE)
			if (HasFeature(FtpCapability.SIZE)) {

				// Fix #328: get filesize in ASCII or Binary mode as required by server
				FtpSizeReply sizeReply = new FtpSizeReply();
				await GetFileSizeInternalAsync(path, token, sizeReply);
				if (sizeReply.Reply.Code[0] == '2') {
					return true;
				}

				if (sizeReply.Reply.Code[0] == '5' && sizeReply.Reply.Message.IsKnownError(fileNotFoundStrings)) {
					return false;
				}

				// Fix #179: Server returns 550 if file not found or no access to file
				if (sizeReply.Reply.Code.Substring(0, 3) == "550") {
					return false;
				}
			}

			// check if file exists by attempting to get its date modified (MDTM)
			if (HasFeature(FtpCapability.MDTM)) {
				FtpReply reply = await ExecuteAsync("MDTM " + path, token);
				var ch = reply.Code[0];
				if (ch == '2') {
					return true;
				}

				if (ch == '5' && reply.Message.IsKnownError(fileNotFoundStrings)) {
					return false;
				}
			}

			// check if file exists by getting a name listing (NLST)
			string[] fileList = await GetNameListingAsync(path.GetFtpDirectoryName(), token);
			return FtpExtensions.FileExistsInNameListing(fileList, path);

			// check if file exists by attempting to download it (RETR)
			/*try {
				Stream stream = OpenRead(path);
				stream.Close();
				return true;
			} catch (FtpException ex) {
			}*/

			return false;
		}
#endif

		#endregion

		#region Rename File/Directory

		/// <summary>
		/// Renames an object on the remote file system.
		/// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
		/// Throws exceptions if the file does not exist, or if the destination file already exists.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <example><code source="..\Examples\Rename.cs" lang="cs" /></example>
		public void Rename(string path, string dest) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc("Rename", new object[] { path, dest });

				// calc the absolute filepaths
				path = GetAbsolutePath(path.GetFtpPath());
				dest = GetAbsolutePath(dest.GetFtpPath());

				if (!(reply = Execute("RNFR " + path)).Success) {
					throw new FtpCommandException(reply);
				}

				if (!(reply = Execute("RNTO " + dest)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

#if !CORE
		private delegate void AsyncRename(string path, string dest);

		/// <summary>
		/// Begins an asynchronous operation to rename an object on the remote file system.
		/// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
		/// Throws exceptions if the file does not exist, or if the destination file already exists.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
		public IAsyncResult BeginRename(string path, string dest, AsyncCallback callback, object state) {
			AsyncRename func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = Rename).BeginInvoke(path, dest, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginRename"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginRename"/></param>
		/// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
		public void EndRename(IAsyncResult ar) {
			GetAsyncDelegate<AsyncRename>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Renames an object on the remote file system asynchronously.
		/// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
		/// Throws exceptions if the file does not exist, or if the destination file already exists.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="token">Cancellation Token</param>
		public async Task RenameAsync(string path, string dest, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

			LogFunc(nameof(RenameAsync), new object[] { path, dest });

			// calc the absolute filepaths
			path = await GetAbsolutePathAsync(path.GetFtpPath(), token);
			dest = await GetAbsolutePathAsync(dest.GetFtpPath(), token);

			if (!(reply = await ExecuteAsync("RNFR " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			if (!(reply = await ExecuteAsync("RNTO " + dest, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif

		#endregion

		#region Move File

		/// <summary>
		/// Moves a file on the remote file system from one directory to another.
		/// Always checks if the source file exists. Checks if the dest file exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest file exists? And if it does should we overwrite/skip the operation?</param>
		/// <returns>Whether the file was moved</returns>
		public bool MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

			LogFunc("MoveFile", new object[] { path, dest, existsMode });

			if (FileExists(path)) {
				// check if dest file exists and act accordingly
				if (existsMode != FtpRemoteExists.NoCheck) {
					var destExists = FileExists(dest);
					if (destExists) {
						switch (existsMode) {
							case FtpRemoteExists.Overwrite:
								DeleteFile(dest);
								break;

							case FtpRemoteExists.Skip:
								return false;
						}
					}
				}

				// move the file
				Rename(path, dest);

				return true;
			}

			return false;
		}

#if !CORE
		private delegate bool AsyncMoveFile(string path, string dest, FtpRemoteExists existsMode);

		/// <summary>
		/// Begins an asynchronous operation to move a file on the remote file system, from one directory to another.
		/// Always checks if the source file exists. Checks if the dest file exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest file exists? And if it does should we overwrite/skip the operation?</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginMoveFile(string path, string dest, FtpRemoteExists existsMode, AsyncCallback callback, object state) {
			AsyncMoveFile func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = MoveFile).BeginInvoke(path, dest, existsMode, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginMoveFile"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginMoveFile"/></param>
		public void EndMoveFile(IAsyncResult ar) {
			GetAsyncDelegate<AsyncMoveFile>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Moves a file asynchronously on the remote file system from one directory to another.
		/// Always checks if the source file exists. Checks if the dest file exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest file exists? And if it does should we overwrite/skip the operation?</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>Whether the file was moved</returns>
		public async Task<bool> MoveFileAsync(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

			LogFunc(nameof(MoveFileAsync), new object[] { path, dest, existsMode });

			if (await FileExistsAsync(path, token)) {
				// check if dest file exists and act accordingly
				if (existsMode != FtpRemoteExists.NoCheck) {
					bool destExists = await FileExistsAsync(dest, token);
					if (destExists) {
						switch (existsMode) {
							case FtpRemoteExists.Overwrite:
								await DeleteFileAsync(dest, token);
								break;

							case FtpRemoteExists.Skip:
								return false;
						}
					}
				}

				// move the file
				await RenameAsync(path, dest, token);

				return true;
			}

			return false;
		}
#endif

		#endregion

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
				LogFunc("SetFilePermissions", new object[] { path, permissions });

				if (!(reply = Execute("SITE CHMOD " + permissions.ToString() + " " + path.GetFtpPath())).Success) {
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
		/// <param name="token">Cancellation Token</param>
		public async Task SetFilePermissionsAsync(string path, int permissions, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(SetFilePermissionsAsync), new object[] { path, permissions });

			if (!(reply = await ExecuteAsync("SITE CHMOD " + permissions.ToString() + " " + path.GetFtpPath(), token)).Success) {
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
		/// <param name="token">Cancellation Token</param>
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
			SetFilePermissions(path, FtpExtensions.CalcChmod(owner, group, other));
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
		/// <param name="token">Cancellation Token</param>
		public Task SetFilePermissionsAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken)) {
			return SetFilePermissionsAsync(path, FtpExtensions.CalcChmod(owner, group, other), token);
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
		/// <param name="token">Cancellation Token</param>
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

			LogFunc("GetFilePermissions", new object[] { path });

			var fullPath = path.GetFtpPath();
			foreach (var i in GetListing(path)) {
				if (i.FullName == fullPath) {
					return i;
				}
			}

			return null;
		}

#if ASYNC
		/// <summary>
		/// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
		/// Throws FtpCommandException if there is an issue.
		/// Returns null if the server did not specify a permission value.
		/// Use `GetChmod` if you required the integer value instead.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="token">Cancellation Token</param>
		public async Task<FtpListItem> GetFilePermissionsAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetFilePermissionsAsync), new object[] { path });

			var fullPath = path.GetFtpPath();
			foreach (FtpListItem i in await GetListingAsync(path, token)) {
				if (i.FullName == fullPath) {
					return i;
				}
			}

			return null;
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
		/// <param name="token">Cancellation Token</param>
		public async Task<int> GetChmodAsync(string path, CancellationToken token = default(CancellationToken)) {
			FtpListItem item = await GetFilePermissionsAsync(path, token);
			return item != null ? item.Chmod : 0;
		}
#endif

		#endregion

		#region Dereference Link

		/// <summary>
		/// Recursively dereferences a symbolic link. See the
		/// MaximumDereferenceCount property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		public FtpListItem DereferenceLink(FtpListItem item) {
			return DereferenceLink(item, MaximumDereferenceCount);
		}

		/// <summary>
		/// Recursively dereferences a symbolic link
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <param name="recMax">The maximum depth of recursion that can be performed before giving up.</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		public FtpListItem DereferenceLink(FtpListItem item, int recMax) {
			LogFunc("DereferenceLink", new object[] { item.FullName, recMax });

			var count = 0;
			return DereferenceLink(item, recMax, ref count);
		}

		/// <summary>
		/// Dereference a FtpListItem object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		private FtpListItem DereferenceLink(FtpListItem item, int recMax, ref int count) {
			if (item.Type != FtpFileSystemObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}

			foreach (var obj in GetListing(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList)) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
						if (++count == recMax) {
							return null;
						}

						return DereferenceLink(obj, recMax, ref count);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						var modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue) {
							obj.Modified = modify;
						}
					}

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
						obj.Size = GetFileSize(obj.FullName);
					}

					return obj;
				}
			}

			return null;
		}

#if !CORE
		private delegate FtpListItem AsyncDereferenceLink(FtpListItem item, int recMax);

		/// <summary>
		/// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, int recMax, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDereferenceLink func;

			lock (m_asyncmethods) {
				ar = (func = DereferenceLink).BeginInvoke(item, recMax, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object. See the
		/// <see cref="MaximumDereferenceCount"/> property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, AsyncCallback callback, object state) {
			return BeginDereferenceLink(item, MaximumDereferenceCount, callback, state);
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginDereferenceLink"/>
		/// </summary>
		/// <param name="ar">IAsyncResult</param>
		/// <returns>A <see cref="FtpListItem"/>, or null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public FtpListItem EndDereferenceLink(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncDereferenceLink>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Dereference a FtpListItem object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		private async Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, IntRef count, CancellationToken token = default(CancellationToken)) {
			if (item.Type != FtpFileSystemObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}

			var listing = await GetListingAsync(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList, token);

			foreach (FtpListItem obj in listing) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
						if (++count.Value == recMax) {
							return null;
						}

						return await DereferenceLinkAsync(obj, recMax, count, token);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						var modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue) {
							obj.Modified = modify;
						}
					}

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
						obj.Size = GetFileSize(obj.FullName);
					}

					return obj;
				}
			}

			return null;
		}

		/// <summary>
		/// Dereference a <see cref="FtpListItem"/> object asynchronously
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(DereferenceLinkAsync), new object[] { item.FullName, recMax });

			var count = new IntRef { Value = 0 };
			return DereferenceLinkAsync(item, recMax, count, token);
		}

		/// <summary>
		/// Dereference a <see cref="FtpListItem"/> object asynchronously
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, CancellationToken token = default(CancellationToken)) {
			return DereferenceLinkAsync(item, MaximumDereferenceCount, token);
		}
#endif

		#endregion

		#region Get File Size

		/// <summary>
		/// Gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <returns>-1 if the command fails, otherwise the file size</returns>
		/// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
		public virtual long GetFileSize(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc("GetFileSize", new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) {
				return -1;
			}

			var sizeReply = new FtpSizeReply();
#if !CORE14
			lock (m_lock) {
#endif
				GetFileSizeInternal(path, sizeReply);
#if !CORE14
			}
#endif
			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		private void GetFileSizeInternal(string path, FtpSizeReply sizeReply) {
			long length = -1;

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (_FileSizeASCIINotSupported) {
				SetDataTypeNoLock(FtpDataType.Binary);
			}

			// execute the SIZE command
			var reply = Execute("SIZE " + path.GetFtpPath());
			sizeReply.Reply = reply;
			if (!reply.Success) {
				length = -1;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!_FileSizeASCIINotSupported && reply.Message.IsKnownError(fileSizeNotInASCIIStrings)) {
					// set the flag so mode switching is done
					_FileSizeASCIINotSupported = true;

					// retry getting the file size
					GetFileSizeInternal(path, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = -1;
			}

			sizeReply.FileSize = length;
		}

#if !CORE
		private delegate long AsyncGetFileSize(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve the size of a remote file
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetFileSize func;

			lock (m_asyncmethods) {
				ar = (func = GetFileSize).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetFileSize"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetFileSize"/></param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public long EndGetFileSize(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetFileSize>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Asynchronously gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		public async Task<long> GetFileSizeAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetFileSizeAsync), new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) {
				return -1;
			}

			FtpSizeReply sizeReply = new FtpSizeReply();
			await GetFileSizeInternalAsync(path, token, sizeReply);

			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		private async Task GetFileSizeInternalAsync(string path, CancellationToken token, FtpSizeReply sizeReply) {
			long length = -1;

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (_FileSizeASCIINotSupported) {
				await SetDataTypeNoLockAsync(FtpDataType.Binary, token);
			}

			// execute the SIZE command
			var reply = await ExecuteAsync("SIZE " + path.GetFtpPath(), token);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				sizeReply.FileSize = -1;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!_FileSizeASCIINotSupported && reply.Message.IsKnownError(fileSizeNotInASCIIStrings)) {
					// set the flag so mode switching is done
					_FileSizeASCIINotSupported = true;

					// retry getting the file size
					await GetFileSizeInternalAsync(path, token, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = -1;
			}

			sizeReply.FileSize = length;

			return;
		}


#endif

		#endregion

		#region Get Modified Time

		/// <summary>
		/// Gets the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
		public virtual DateTime GetModifiedTime(string path, FtpDate type = FtpDate.Original) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc("GetModifiedTime", new object[] { path, type });

			var date = DateTime.MinValue;
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif

				// get modified date of a file
				if ((reply = Execute("MDTM " + path.GetFtpPath())).Success) {
					date = reply.Message.GetFtpDate(TimeConversion);

					// convert server timezone to UTC, based on the TimeOffset property
					if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
						date = date - m_listParser.TimeOffset;
					}

					// convert to local time if wanted
#if !CORE
					if (type == FtpDate.Local) {
						date = TimeZone.CurrentTimeZone.ToLocalTime(date);
					}

#endif
				}

#if !CORE14
			}
#endif

			return date;
		}

#if !CORE
		private delegate DateTime AsyncGetModifiedTime(string path, FtpDate type);

		/// <summary>
		/// Begins an asynchronous operation to get the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public IAsyncResult BeginGetModifiedTime(string path, FtpDate type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetModifiedTime func;

			lock (m_asyncmethods) {
				ar = (func = GetModifiedTime).BeginInvoke(path, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetModifiedTime"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetModifiedTime"/></param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public DateTime EndGetModifiedTime(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetModifiedTime>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public async Task<DateTime> GetModifiedTimeAsync(string path, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetModifiedTimeAsync), new object[] { path, type });

			var date = DateTime.MinValue;
			FtpReply reply;

			// get modified date of a file
			if ((reply = await ExecuteAsync("MDTM " + path.GetFtpPath(), token)).Success) {
				date = reply.Message.GetFtpDate(TimeConversion);

				// convert server timezone to UTC, based on the TimeOffset property
				if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
					date = date - m_listParser.TimeOffset;
				}

				// convert to local time if wanted
#if !CORE
				if (type == FtpDate.Local) {
					date = TimeZone.CurrentTimeZone.ToLocalTime(date);
				}

#endif
			}

			return date;
		}
#endif

		#endregion

		#region Set Modified Time

		/// <summary>
		/// Changes the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		public virtual void SetModifiedTime(string path, DateTime date, FtpDate type = FtpDate.Original) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			LogFunc("SetModifiedTime", new object[] { path, date, type });

			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif

				// convert local to UTC if wanted
#if !CORE
				if (type == FtpDate.Local) {
					date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
				}
#endif

				// convert UTC to server timezone, based on the TimeOffset property
				if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
					date = date + m_listParser.TimeOffset;
				}

				// set modified date of a file
				var timeStr = date.ToString("yyyyMMddHHmmss");
				if ((reply = Execute("MFMT " + timeStr + " " + path.GetFtpPath())).Success) {
				}

#if !CORE14
			}

#endif
		}

#if !CORE
		private delegate void AsyncSetModifiedTime(string path, DateTime date, FtpDate type);

		/// <summary>
		/// Begins an asynchronous operation to get the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginSetModifiedTime(string path, DateTime date, FtpDate type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetModifiedTime func;

			lock (m_asyncmethods) {
				ar = (func = SetModifiedTime).BeginInvoke(path, date, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginSetModifiedTime"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetModifiedTime"/></param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public void EndSetModifiedTime(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetModifiedTime>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="token">Cancellation Token</param>
		public async Task SetModifiedTimeAsync(string path, DateTime date, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			LogFunc(nameof(SetModifiedTimeAsync), new object[] { path, date, type });

			FtpReply reply;

			// convert local to UTC if wanted
#if !CORE
			if (type == FtpDate.Local) {
				date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
			}
#endif

			// convert UTC to server timezone, based on the TimeOffset property
			if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
				date = date + m_listParser.TimeOffset;
			}

			// set modified date of a file
			var timeStr = date.ToString("yyyyMMddHHmmss");
			if ((reply = await ExecuteAsync("MFMT " + timeStr + " " + path.GetFtpPath(), token)).Success) {
			}
		}
#endif

		#endregion
	}
}