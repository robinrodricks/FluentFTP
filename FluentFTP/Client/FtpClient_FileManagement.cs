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
using FluentFTP.Servers;
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
				LogFunc(nameof(DeleteFile), new object[] { path });

				if (!(reply = Execute("DELE " + path.GetFtpPath())).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

#if !ASYNC
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
		/// <param name="token">The token that can be used to cancel the entire process</param>
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

				LogFunc(nameof(FileExists), new object[] { path });

				// calc the absolute filepath
				path = GetAbsolutePath(path.GetFtpPath());

				// since FTP does not include a specific command to check if a file exists
				// here we check if file exists by attempting to get its filesize (SIZE)
				if (HasFeature(FtpCapability.SIZE)) {
					// Fix #328: get filesize in ASCII or Binary mode as required by server
					var sizeReply = new FtpSizeReply();
					GetFileSizeInternal(path, sizeReply);

					// handle known errors to the SIZE command
					var sizeKnownError = CheckFileExistsBySize(sizeReply);
					if (sizeKnownError.HasValue) {
						return sizeKnownError.Value;
					}
				}

				// check if file exists by attempting to get its date modified (MDTM)
				if (HasFeature(FtpCapability.MDTM)) {
					var reply = Execute("MDTM " + path);
					var ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}
					if (ch == '5' && reply.Message.IsKnownError(FtpServerStrings.fileNotFound)) {
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

		private bool? CheckFileExistsBySize(FtpSizeReply sizeReply) {

			// file surely exists
			if (sizeReply.Reply.Code[0] == '2') {
				return true;
			}

			// file surely does not exist
			if (sizeReply.Reply.Code[0] == '5' && sizeReply.Reply.Message.IsKnownError(FtpServerStrings.fileNotFound)) {
				return false;
			}
			
			// Fix #518: This check is too broad and must be disabled, need to fallback to MDTM or NLST instead.
			// Fix #179: Add a generic check to since server returns 550 if file not found or no access to file.
			/*if (sizeReply.Reply.Code.Substring(0, 3) == "550") {
				return false;
			}*/

			// fallback to MDTM or NLST
			return null;
		}

#if !ASYNC
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
		/// <param name="token">The token that can be used to cancel the entire process</param>
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

				// handle known errors to the SIZE command
				var sizeKnownError = CheckFileExistsBySize(sizeReply);
				if (sizeKnownError.HasValue) {
					return sizeKnownError.Value;
				}
			}

			// check if file exists by attempting to get its date modified (MDTM)
			if (HasFeature(FtpCapability.MDTM)) {
				FtpReply reply = await ExecuteAsync("MDTM " + path, token);
				var ch = reply.Code[0];
				if (ch == '2') {
					return true;
				}

				if (ch == '5' && reply.Message.IsKnownError(FtpServerStrings.fileNotFound)) {
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
				LogFunc(nameof(Rename), new object[] { path, dest });

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

#if !ASYNC
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
		/// <param name="token">The token that can be used to cancel the entire process</param>
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

			LogFunc(nameof(MoveFile), new object[] { path, dest, existsMode });

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

#if !ASYNC
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
		/// <param name="token">The token that can be used to cancel the entire process</param>
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

	}
}