using System;
using System.Text.RegularExpressions;
using System.Linq;
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
		#region Delete Directory

		/// <summary>
		/// Deletes the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(DeleteDirectory), new object[] { path });
			DeleteDirInternal(path, true, FtpListOption.ForceList | FtpListOption.Recursive);
		}

		/// <summary>
		/// Deletes the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path, FtpListOption options) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(DeleteDirectory), new object[] { path, options });
			DeleteDirInternal(path, true, options);
		}

		/// <summary>
		/// Deletes the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="deleteContents">If the directory is not empty, remove its contents</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		private void DeleteDirInternal(string path, bool deleteContents, FtpListOption options) {
			FtpReply reply;
			var ftppath = path.GetFtpPath();


#if !CORE14
			lock (m_lock) {
#endif


				// server-specific directory deletion
				if (!ftppath.IsFtpRootDirectory()) {
					if (FtpServerSpecificHandler.ServerDeleteDirectory(this, path, ftppath, deleteContents, options)) {
						return;
					}
				}


				// DELETE CONTENTS OF THE DIRECTORY
				if (deleteContents) {
					// when GetListing is called with recursive option, then it does not
					// make any sense to call another DeleteDirectory with force flag set.
					// however this requires always delete files first.
					var recurse = !WasGetListingRecursive(options);

					// items that are deeper in directory tree are listed first, 
					// then files will be listed before directories. This matters
					// only if GetListing was called with recursive option.
					FtpListItem[] itemList;
					if (recurse) {
						itemList = GetListing(path, options);
					}
					else {
						itemList = GetListing(path, options).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();
					}

					// delete the item based on the type
					foreach (var item in itemList) {
						switch (item.Type) {
							case FtpFileSystemObjectType.File:
								DeleteFile(item.FullName);
								break;

							case FtpFileSystemObjectType.Directory:
								DeleteDirInternal(item.FullName, recurse, options);
								break;

							default:
								throw new FtpException("Don't know how to delete object type: " + item.Type);
						}
					}
				}


				// SKIP DELETING ROOT DIRS

				// can't delete the working directory and
				// can't delete the server root.
				if (ftppath.IsFtpRootDirectory()) {
					return;
				}


				// DELETE ACTUAL DIRECTORY

				if (!(reply = Execute("RMD " + ftppath)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

		/// <summary>
		/// Checks whether <see cref="o:GetListing"/> will be called recursively or not.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		private bool WasGetListingRecursive(FtpListOption options) {
			// FIX: GetListing() now supports recursive listing for all types of lists (name list, file list, machine list)
			//		even if the server does not support recursive listing, because it does its own internal recursion.
			return (options & FtpListOption.Recursive) == FtpListOption.Recursive;
		}

#if !ASYNC
		private delegate void AsyncDeleteDirectory(string path, FtpListOption options);

		/// <summary>
		/// Begins an asynchronous operation to delete the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteDirectory(string path, AsyncCallback callback, object state) {
			return BeginDeleteDirectory(path, FtpListOption.ForceList | FtpListOption.Recursive, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to delete the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteDirectory(string path, FtpListOption options, AsyncCallback callback, object state) {
			AsyncDeleteDirectory func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = DeleteDirectory).BeginInvoke(path, options, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginDeleteDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDeleteDirectory</param>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public void EndDeleteDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncDeleteDirectory>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Asynchronously removes a directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task DeleteDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(DeleteDirectoryAsync), new object[] { path });
			return DeleteDirInternalAsync(path, true, FtpListOption.ForceList | FtpListOption.Recursive, token);
		}

		/// <summary>
		/// Asynchronously removes a directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task DeleteDirectoryAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(DeleteDirectoryAsync), new object[] { path, options });
			return DeleteDirInternalAsync(path, true, options, token);
		}

		/// <summary>
		/// Asynchronously removes a directory. Used by <see cref="DeleteDirectoryAsync(string)"/> and
		/// <see cref="DeleteDirectoryAsync(string, FtpListOption)"/>.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="deleteContents">Delete the contents before deleting the folder</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns></returns>
		private async Task DeleteDirInternalAsync(string path, bool deleteContents, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			var ftppath = path.GetFtpPath();

			// server-specific directory deletion
			if (!ftppath.IsFtpRootDirectory()) {
				if (await FtpServerSpecificHandler.ServerDeleteDirectoryAsync(this, path, ftppath, deleteContents, options, token)) {
					return;
				}
			}

			// DELETE CONTENTS OF THE DIRECTORY
			if (deleteContents) {
				// when GetListing is called with recursive option, then it does not
				// make any sense to call another DeleteDirectory with force flag set.
				// however this requires always delete files first.
				var recurse = !WasGetListingRecursive(options);

				// items that are deeper in directory tree are listed first, 
				// then files will be listed before directories. This matters
				// only if GetListing was called with recursive option.
				FtpListItem[] itemList;
				if (recurse) {
					itemList = await GetListingAsync(path, options, token);
				}
				else {
					itemList = (await GetListingAsync(path, options, token)).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();
				}

				// delete the item based on the type
				foreach (var item in itemList) {
					switch (item.Type) {
						case FtpFileSystemObjectType.File:
							await DeleteFileAsync(item.FullName, token);
							break;

						case FtpFileSystemObjectType.Directory:
							await DeleteDirInternalAsync(item.FullName, recurse, options, token);
							break;

						default:
							throw new FtpException("Don't know how to delete object type: " + item.Type);
					}
				}
			}

			// SKIP DELETING ROOT DIRS

			// can't delete the working directory and
			// can't delete the server root.
			if (ftppath.IsFtpRootDirectory()) {
				return;
			}

			// DELETE ACTUAL DIRECTORY

			if (!(reply = await ExecuteAsync("RMD " + ftppath, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif

		#endregion

		#region Directory Exists

		/// <summary>
		/// Tests if the specified directory exists on the server. This
		/// method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <param name="path">The path of the directory</param>
		/// <returns>True if it exists, false otherwise.</returns>
		/// <example><code source="..\Examples\DirectoryExists.cs" lang="cs" /></example>
		public bool DirectoryExists(string path) {
			string pwd;

			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			LogFunc(nameof(DirectoryExists), new object[] { path });

			// quickly check if root path, then it always exists!
			var ftppath = path.GetFtpPath();
			if (ftppath == "." || ftppath == "./" || ftppath == "/") {
				return true;
			}

			// check if a folder exists by changing the working dir to it
#if !CORE14
			lock (m_lock) {
#endif
				pwd = GetWorkingDirectory();

				if (Execute("CWD " + ftppath).Success) {
					var reply = Execute("CWD " + pwd.GetFtpPath());

					if (!reply.Success) {
						throw new FtpException("DirectoryExists(): Failed to restore the working directory.");
					}

					return true;
				}

#if !CORE14
			}
#endif

			return false;
		}

#if !ASYNC
		private delegate bool AsyncDirectoryExists(string path);

		/// <summary>
		/// Begins an asynchronous operation to test if the specified directory exists on the server. 
		/// This method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <returns>IAsyncResult</returns>
		/// <param name='path'>The full or relative path of the directory to check for</param>
		/// <param name='callback'>Async callback</param>
		/// <param name='state'>State object</param>
		/// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
		public IAsyncResult BeginDirectoryExists(string path, AsyncCallback callback, object state) {
			AsyncDirectoryExists func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = DirectoryExists).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginDirectoryExists"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDirectoryExists</param>
		/// <returns>True if the directory exists. False otherwise.</returns>
		/// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
		public bool EndDirectoryExists(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncDirectoryExists>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Tests if the specified directory exists on the server asynchronously. This
		/// method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <param name='path'>The full or relative path of the directory to check for</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if the directory exists. False otherwise.</returns>
		public async Task<bool> DirectoryExistsAsync(string path, CancellationToken token = default(CancellationToken)) {
			string pwd;

			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			LogFunc(nameof(DirectoryExistsAsync), new object[] { path });

			// quickly check if root path, then it always exists!
			var ftppath = path.GetFtpPath();
			if (ftppath == "." || ftppath == "./" || ftppath == "/") {
				return true;
			}

			// check if a folder exists by changing the working dir to it
			pwd = await GetWorkingDirectoryAsync(token);

			if ((await ExecuteAsync("CWD " + ftppath, token)).Success) {
				FtpReply reply = await ExecuteAsync("CWD " + pwd.GetFtpPath(), token);

				if (!reply.Success) {
					throw new FtpException("DirectoryExists(): Failed to restore the working directory.");
				}

				return true;
			}

			return false;
		}
#endif

		#endregion

		#region Create Directory

		/// <summary>
		/// Creates a directory on the server. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		public bool CreateDirectory(string path) {
			return CreateDirectory(path, true);
		}

		/// <summary>
		/// Creates a directory on the server
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to force all non-existent pieces of the path to be created</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		/// <returns>True if directory was created, false if it was skipped</returns>
		public bool CreateDirectory(string path, bool force) {
			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			LogFunc(nameof(CreateDirectory), new object[] { path, force });

			FtpReply reply;
			var ftppath = path.GetFtpPath();

			// cannot create root or working directory
			if (ftppath.IsFtpRootDirectory()) {
				return false;
			}

#if !CORE14
			lock (m_lock) {
#endif

				// server-specific directory creation
				if (FtpServerSpecificHandler.ServerCreateDirectory(this, path, ftppath, force)) {
					return true;
				}

				path = path.GetFtpPath().TrimEnd('/');

				if (force && !DirectoryExists(path.GetFtpDirectoryName())) {
					LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
					CreateDirectory(path.GetFtpDirectoryName(), true);
				}

				// fix: improve performance by skipping the directory exists check
				/*else if (DirectoryExists(path)) {
					return false;
				}*/

				LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + ftppath);

				if (!(reply = Execute("MKD " + ftppath)).Success) {

					// if the error indicates the directory already exists, its not an error
					if (reply.Code == "550") {
						return false;
					}
					if (reply.Code[0] == '5' && reply.Message.IsKnownError(FtpServerStrings.folderExists)) {
						return false;
					}

					throw new FtpCommandException(reply);
				}
				return true;

#if !CORE14
			}

#endif
		}

#if !ASYNC
		private delegate bool AsyncCreateDirectory(string path, bool force);

		/// <summary>
		/// Begins an asynchronous operation to create a remote directory. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginCreateDirectory(string path, AsyncCallback callback, object state) {
			return BeginCreateDirectory(path, true, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to create a remote directory
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginCreateDirectory(string path, bool force, AsyncCallback callback, object state) {
			AsyncCreateDirectory func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = CreateDirectory).BeginInvoke(path, force, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginCreateDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="o:BeginCreateDirectory"/></param>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public void EndCreateDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncCreateDirectory>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Creates a remote directory asynchronously
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if directory was created, false if it was skipped</returns>
		public async Task<bool> CreateDirectoryAsync(string path, bool force, CancellationToken token = default(CancellationToken)) {
			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			LogFunc(nameof(CreateDirectoryAsync), new object[] { path, force });

			FtpReply reply;
			var ftppath = path.GetFtpPath();

			// cannot create root or working directory
			if (ftppath.IsFtpRootDirectory()) {
				return false;
			}

			// server-specific directory creation
			if (await FtpServerSpecificHandler.ServerCreateDirectoryAsync(this, path, ftppath, force, token)) {
				return true;
			}

			path = path.GetFtpPath().TrimEnd('/');

			if (force && !await DirectoryExistsAsync(path.GetFtpDirectoryName(), token)) {
				LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
				await CreateDirectoryAsync(path.GetFtpDirectoryName(), true, token);
			}

			// fix: improve performance by skipping the directory exists check
			/*else if (await DirectoryExistsAsync(path, token)) {
				return false;
			}*/

			LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + ftppath);

			if (!(reply = await ExecuteAsync("MKD " + ftppath, token)).Success) {

				// if the error indicates the directory already exists, its not an error
				if (reply.Code == "550") {
					return false;
				}
				if (reply.Code[0] == '5' && reply.Message.IsKnownError(FtpServerStrings.folderExists)) {
					return false;
				}

				throw new FtpCommandException(reply);
			}
			return true;
		}

		/// <summary>
		/// Creates a remote directory asynchronously. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task<bool> CreateDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			return CreateDirectoryAsync(path, true, token);
		}
#endif

		#endregion

		#region Move Directory

		/// <summary>
		/// Moves a directory on the remote file system from one directory to another.
		/// Always checks if the source directory exists. Checks if the dest directory exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest directory exists? And if it does should we overwrite/skip the operation?</param>
		/// <returns>Whether the directory was moved</returns>
		public bool MoveDirectory(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

			LogFunc(nameof(MoveDirectory), new object[] { path, dest, existsMode });

			if (DirectoryExists(path)) {
				// check if dest directory exists and act accordingly
				if (existsMode != FtpRemoteExists.NoCheck) {
					var destExists = DirectoryExists(dest);
					if (destExists) {
						switch (existsMode) {
							case FtpRemoteExists.Overwrite:
								DeleteDirectory(dest);
								break;

							case FtpRemoteExists.Skip:
								return false;
						}
					}
				}

				// move the directory
				Rename(path, dest);

				return true;
			}

			return false;
		}

#if !ASYNC
		private delegate bool AsyncMoveDirectory(string path, string dest, FtpRemoteExists existsMode);

		/// <summary>
		/// Begins an asynchronous operation to move a directory on the remote file system, from one directory to another.
		/// Always checks if the source directory exists. Checks if the dest directory exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest directory exists? And if it does should we overwrite/skip the operation?</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginMoveDirectory(string path, string dest, FtpRemoteExists existsMode, AsyncCallback callback, object state) {
			AsyncMoveDirectory func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = MoveDirectory).BeginInvoke(path, dest, existsMode, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginMoveDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginMoveDirectory"/></param>
		public void EndMoveDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncMoveDirectory>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Moves a directory asynchronously on the remote file system from one directory to another.
		/// Always checks if the source directory exists. Checks if the dest directory exists based on the `existsMode` parameter.
		/// Only throws exceptions for critical errors.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="existsMode">Should we check if the dest directory exists? And if it does should we overwrite/skip the operation?</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>Whether the directory was moved</returns>
		public async Task<bool> MoveDirectoryAsync(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "dest");
			}

			LogFunc(nameof(MoveDirectoryAsync), new object[] { path, dest, existsMode });

			if (await DirectoryExistsAsync(path, token)) {
				// check if dest directory exists and act accordingly
				if (existsMode != FtpRemoteExists.NoCheck) {
					bool destExists = await DirectoryExistsAsync(dest, token);
					if (destExists) {
						switch (existsMode) {
							case FtpRemoteExists.Overwrite:
								await DeleteDirectoryAsync(dest, token);
								break;

							case FtpRemoteExists.Skip:
								return false;
						}
					}
				}

				// move the directory
				await RenameAsync(path, dest, token);

				return true;
			}

			return false;
		}
#endif

		#endregion

		#region Set Working Dir

		/// <summary>
		/// Sets the work directory on the server
		/// </summary>
		/// <param name="path">The path of the directory to change to</param>
		/// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
		public void SetWorkingDirectory(string path) {
			LogFunc(nameof(SetWorkingDirectory), new object[] { path });

			FtpReply reply;
			var ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./") {
				return;
			}

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("CWD " + ftppath)).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}

#endif
		}

#if !ASYNC
		private delegate void AsyncSetWorkingDirectory(string path);

		/// <summary>
		/// Begins an asynchronous operation to set the working directory on the server
		/// </summary>
		/// <param name="path">The directory to change to</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginSetWorkingDirectory(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetWorkingDirectory func;

			lock (m_asyncmethods) {
				ar = (func = SetWorkingDirectory).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginSetWorkingDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetWorkingDirectory"/></param>
		/// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
		public void EndSetWorkingDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetWorkingDirectory>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Sets the working directory on the server asynchronously
		/// </summary>
		/// <param name="path">The directory to change to</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetWorkingDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(SetWorkingDirectoryAsync), new object[] { path });

			FtpReply reply;
			var ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./") {
				return;
			}

			if (!(reply = await ExecuteAsync("CWD " + ftppath, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}

#endif

		#endregion

		#region Get Working Dir

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		/// <example><code source="..\Examples\GetWorkingDirectory.cs" lang="cs" /></example>
		public string GetWorkingDirectory() {
			LogFunc(nameof(GetWorkingDirectory));

			FtpReply reply;
			Match m;

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("PWD")).Success) {
					throw new FtpCommandException(reply);
				}

#if !CORE14
			}
#endif

			if ((m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success) {
				return m.Groups["pwd"].Value;
			}

			// check for MODCOMP ftp path mentioned in forums: https://netftp.codeplex.com/discussions/444461
			if ((m = Regex.Match(reply.Message, "PWD = (?<pwd>.*)")).Success) {
				return m.Groups["pwd"].Value;
			}

			LogStatus(FtpTraceLevel.Warn, "Failed to parse working directory from: " + reply.Message);

			return "./";
		}

#if !ASYNC
		private delegate string AsyncGetWorkingDirectory();

		/// <summary>
		/// Begins an asynchronous operation to get the working directory
		/// </summary>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginGetWorkingDirectory(AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetWorkingDirectory func;

			lock (m_asyncmethods) {
				ar = (func = GetWorkingDirectory).BeginInvoke(callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetWorkingDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetWorkingDirectory"/></param>
		/// <returns>The current working directory</returns>
		/// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
		public string EndGetWorkingDirectory(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetWorkingDirectory>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the current working directory asynchronously
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		public async Task<string> GetWorkingDirectoryAsync(CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(GetWorkingDirectoryAsync));

			FtpReply reply;
			Match m;

			if (!(reply = await ExecuteAsync("PWD", token)).Success) {
				throw new FtpCommandException(reply);
			}

			if ((m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success) {
				return m.Groups["pwd"].Value;
			}

			// check for MODCOMP ftp path mentioned in forums: https://netftp.codeplex.com/discussions/444461
			if ((m = Regex.Match(reply.Message, "PWD = (?<pwd>.*)")).Success) {
				return m.Groups["pwd"].Value;
			}

			LogStatus(FtpTraceLevel.Warn, "Failed to parse working directory from: " + reply.Message);

			return "./";
		}

#endif

		#endregion
	}
}