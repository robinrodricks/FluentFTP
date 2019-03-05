using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("DeleteFile", new object[] { path });

				if (!(reply = Execute("DELE " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncDeleteFile(string path);

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
				ar = (func = new AsyncDeleteFile(DeleteFile)).BeginInvoke(path, callback, state);
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
		public async Task DeleteFileAsync(string path, CancellationToken token = default(CancellationToken))
		{
			FtpReply reply;

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(DeleteFileAsync), new object[] { path });

			if (!(reply = await ExecuteAsync("DELE " + path.GetFtpPath(), token)).Success)
				throw new FtpCommandException(reply);
		}
#endif

		#endregion

		#region Delete Directory

		/// <summary>
		/// Deletes the specified directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("DeleteDirectory", new object[] { path });
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("DeleteDirectory", new object[] { path, options });
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
			string ftppath = path.GetFtpPath();


#if !CORE14
			lock (m_lock) {
#endif



				// DELETE CONTENTS OF THE DIRECTORY
				if (deleteContents) {

					// when GetListing is called with recursive option, then it does not
					// make any sense to call another DeleteDirectory with force flag set.
					// however this requires always delete files first.
					bool recurse = !WasGetListingRecursive(options);

					// items that are deeper in directory tree are listed first, 
					// then files will be listed before directories. This matters
					// only if GetListing was called with recursive option.
					FtpListItem[] itemList;
					if (recurse) {
						itemList = GetListing(path, options);
					} else {
						itemList = GetListing(path, options).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();
					}

					// delete the item based on the type
					foreach (FtpListItem item in itemList) {
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
				if (ftppath == "." || ftppath == "./" || ftppath == "/") {
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
			return ((options & FtpListOption.Recursive) == FtpListOption.Recursive);
		}

#if !CORE
		delegate void AsyncDeleteDirectory(string path, FtpListOption options);

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
				ar = (func = new AsyncDeleteDirectory(DeleteDirectory)).BeginInvoke(path, options, callback, state);
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
		/// <param name="token">Cancellation Token</param>
		public Task DeleteDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(DeleteDirectoryAsync), new object[] { path });
			return DeleteDirInternalAsync(path, true, FtpListOption.ForceList | FtpListOption.Recursive, token);
		}

		/// <summary>
		/// Asynchronously removes a directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">Cancellation Token</param>
		public Task DeleteDirectoryAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(DeleteDirectoryAsync), new object[] { path, options });
			return DeleteDirInternalAsync(path, true, options, token);
		}

		/// <summary>
		/// Asynchronously removes a directory. Used by <see cref="DeleteDirectoryAsync(string)"/> and
		/// <see cref="DeleteDirectoryAsync(string, FtpListOption)"/>.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="deleteContents">Delete the contents before deleting the folder</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns></returns>
		private async Task DeleteDirInternalAsync(string path, bool deleteContents, FtpListOption options, CancellationToken token = default(CancellationToken))
		{
			FtpReply reply;
			string ftppath = path.GetFtpPath();

			// DELETE CONTENTS OF THE DIRECTORY
			if (deleteContents)
			{
				// when GetListing is called with recursive option, then it does not
				// make any sense to call another DeleteDirectory with force flag set.
				// however this requires always delete files first.
				bool recurse = !WasGetListingRecursive(options);

				// items that are deeper in directory tree are listed first, 
				// then files will be listed before directories. This matters
				// only if GetListing was called with recursive option.
				FtpListItem[] itemList;
				if (recurse)
				{
					itemList = await GetListingAsync(path, options, token);
				}
				else
				{
					itemList = (await GetListingAsync(path, options, token)).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();
				}

				// delete the item based on the type
				foreach (FtpListItem item in itemList)
				{
					switch (item.Type)
					{
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
			if (ftppath == "." || ftppath == "./" || ftppath == "/")
			{
				return;
			}

			// DELETE ACTUAL DIRECTORY

			if (!(reply = await ExecuteAsync("RMD " + ftppath, token)).Success)
			{
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

			// dont verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("DirectoryExists", new object[] { path });

			// quickly check if root path, then it always exists!
			string ftppath = path.GetFtpPath();
			if (ftppath == "." || ftppath == "./" || ftppath == "/") {
				return true;
			}

			// check if a folder exists by changing the working dir to it
#if !CORE14
			lock (m_lock) {
#endif
				pwd = GetWorkingDirectory();

				if (Execute("CWD " + ftppath).Success) {
					FtpReply reply = Execute("CWD " + pwd.GetFtpPath());

					if (!reply.Success)
						throw new FtpException("DirectoryExists(): Failed to restore the working directory.");

					return true;
				}
#if !CORE14
			}
#endif

			return false;
		}

#if !CORE
		delegate bool AsyncDirectoryExists(string path);

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
				ar = (func = new AsyncDirectoryExists(DirectoryExists)).BeginInvoke(path, callback, state);
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
		/// <param name="token">Cancellation Token</param>
		/// <returns>True if the directory exists. False otherwise.</returns>
		public async Task<bool> DirectoryExistsAsync(string path, CancellationToken token = default(CancellationToken)) {
            string pwd;

            // dont verify args as blank/null path is OK
            //if (path.IsBlank())
            //	throw new ArgumentException("Required parameter is null or blank.", "path");

            this.LogFunc(nameof(DirectoryExistsAsync), new object[] { path });

            // quickly check if root path, then it always exists!
            string ftppath = path.GetFtpPath();
            if (ftppath == "." || ftppath == "./" || ftppath == "/")
            {
                return true;
            }

            // check if a folder exists by changing the working dir to it
            pwd = await GetWorkingDirectoryAsync(token);

            if ((await ExecuteAsync("CWD " + ftppath, token)).Success)
            {
                FtpReply reply = await ExecuteAsync("CWD " + pwd.GetFtpPath(), token);

                if (!reply.Success)
                    throw new FtpException("DirectoryExists(): Failed to restore the working directory.");

                return true;
            }

            return false;
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

#if !CORE14
			lock (m_lock) {
#endif

				this.LogFunc("FileExists", new object[] { path });

				// calc the absolute filepath
				path = GetAbsolutePath(path.GetFtpPath());

				// since FTP does not include a specific command to check if a file exists
				// here we check if file exists by attempting to get its filesize (SIZE)
				if (HasFeature(FtpCapability.SIZE)) {
					FtpReply reply = Execute("SIZE " + path);
					char ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}
					if (ch == '5' && IsKnownError(reply.Message, fileNotFoundStrings)) {
						return false;
					}

					// Fix #179: Server returns 550 if file not found or no access to file
					if (reply.Code.Substring(0, 3) == "550") {
						return false;
					}
				}

				// check if file exists by attempting to get its date modified (MDTM)
				if (HasFeature(FtpCapability.MDTM)) {
					FtpReply reply = Execute("MDTM " + path);
					char ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}
					if (ch == '5' && IsKnownError(reply.Message, fileNotFoundStrings)) {
						return false;
					}
				}

				// check if file exists by getting a name listing (NLST)
				string[] fileList = GetNameListing(path.GetFtpDirectoryName());
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
		delegate bool AsyncFileExists(string path);

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
				ar = (func = new AsyncFileExists(FileExists)).BeginInvoke(path, callback, state);
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
		public async Task<bool> FileExistsAsync(string path, CancellationToken token = default(CancellationToken))
		{

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(FileExistsAsync), new object[] { path });

			// calc the absolute filepath
			path = await GetAbsolutePathAsync(path.GetFtpPath(), token);

			// since FTP does not include a specific command to check if a file exists
			// here we check if file exists by attempting to get its filesize (SIZE)
			if (HasFeature(FtpCapability.SIZE))
			{
				FtpReply reply = await ExecuteAsync("SIZE " + path, token);
				char ch = reply.Code[0];
				if (ch == '2')
				{
					return true;
				}
				if (ch == '5' && IsKnownError(reply.Message, fileNotFoundStrings))
				{
					return false;
				}
			}

			// check if file exists by attempting to get its date modified (MDTM)
			if (HasFeature(FtpCapability.MDTM))
			{
				FtpReply reply = await ExecuteAsync("MDTM " + path, token);
				char ch = reply.Code[0];
				if (ch == '2')
				{
					return true;
				}
				if (ch == '5' && IsKnownError(reply.Message, fileNotFoundStrings))
				{
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

		#region Create Directory

		/// <summary>
		/// Creates a directory on the server. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		public void CreateDirectory(string path) {
			CreateDirectory(path, true);
		}

		/// <summary>
		/// Creates a directory on the server
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to force all non-existent pieces of the path to be created</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		public void CreateDirectory(string path, bool force) {

			// dont verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("CreateDirectory", new object[] { path, force });

			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./" || ftppath == "/")
				return;

#if !CORE14
			lock (m_lock) {
#endif
				path = path.GetFtpPath().TrimEnd('/');

				if (force && !DirectoryExists(path.GetFtpDirectoryName())) {
					this.LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
					CreateDirectory(path.GetFtpDirectoryName(), true);
				} else if (DirectoryExists(path))
					return;

				this.LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + ftppath);

				if (!(reply = Execute("MKD " + ftppath)).Success)
					throw new FtpCommandException(reply);
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncCreateDirectory(string path, bool force);

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
				ar = (func = new AsyncCreateDirectory(CreateDirectory)).BeginInvoke(path, force, callback, state);
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
		/// <param name="token">Cancellation Token</param>
		public async Task CreateDirectoryAsync(string path, bool force, CancellationToken token = default(CancellationToken))
		{
			// dont verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(CreateDirectoryAsync), new object[] { path, force });

			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./" || ftppath == "/")
				return;

			path = path.GetFtpPath().TrimEnd('/');

			if (force && !await DirectoryExistsAsync(path.GetFtpDirectoryName(), token))
			{
				this.LogStatus(FtpTraceLevel.Verbose, "Create non-existent parent directory: " + path.GetFtpDirectoryName());
				await CreateDirectoryAsync(path.GetFtpDirectoryName(), true, token);
			}
			else if (await DirectoryExistsAsync(path, token))
				return;

			this.LogStatus(FtpTraceLevel.Verbose, "CreateDirectory " + ftppath);

			if (!(reply = await ExecuteAsync("MKD " + ftppath, token)).Success)
				throw new FtpCommandException(reply);
		}

		/// <summary>
		/// Creates a remote directory asynchronously. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="token">Cancellation Token</param>
		public Task CreateDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {
			return CreateDirectoryAsync(path, true, token);
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("Rename", new object[] { path, dest });

				// calc the absolute filepaths
				path = GetAbsolutePath(path.GetFtpPath());
				dest = GetAbsolutePath(dest.GetFtpPath());

				if (!(reply = Execute("RNFR " + path)).Success)
					throw new FtpCommandException(reply);

				if (!(reply = Execute("RNTO " + dest)).Success)
					throw new FtpCommandException(reply);
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncRename(string path, string dest);

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
				ar = (func = new AsyncRename(Rename)).BeginInvoke(path, dest, callback, state);
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
		public async Task RenameAsync(string path, string dest, CancellationToken token = default(CancellationToken))
		{
			FtpReply reply;

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

			this.LogFunc(nameof(RenameAsync), new object[] { path, dest });

			// calc the absolute filepaths
			path = await GetAbsolutePathAsync(path.GetFtpPath(), token);
			dest = await GetAbsolutePathAsync(dest.GetFtpPath(), token);

			if (!(reply = await ExecuteAsync("RNFR " + path, token)).Success)
				throw new FtpCommandException(reply);

			if (!(reply = await ExecuteAsync("RNTO " + dest, token)).Success)
				throw new FtpCommandException(reply);
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
		public bool MoveFile(string path, string dest, FtpExists existsMode = FtpExists.Overwrite) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

			this.LogFunc("MoveFile", new object[] { path, dest, existsMode });

			if (FileExists(path)) {

				// check if dest file exists and act accordingly
				if (existsMode != FtpExists.NoCheck) {
					bool destExists = FileExists(dest);
					if (destExists) {
						switch (existsMode) {
							case FtpExists.Overwrite:
								DeleteFile(dest);
								break;
							case FtpExists.Skip:
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
		delegate bool AsyncMoveFile(string path, string dest, FtpExists existsMode);

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
		public IAsyncResult BeginMoveFile(string path, string dest, FtpExists existsMode, AsyncCallback callback, object state) {
			AsyncMoveFile func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = new AsyncMoveFile(MoveFile)).BeginInvoke(path, dest, existsMode, callback, state);
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
		public async Task<bool> MoveFileAsync(string path, string dest, FtpExists existsMode = FtpExists.Overwrite, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

			this.LogFunc(nameof(MoveFileAsync), new object[] { path, dest, existsMode });

			if (await FileExistsAsync(path, token))
			{

				// check if dest file exists and act accordingly
				if (existsMode != FtpExists.NoCheck)
				{
					bool destExists = await FileExistsAsync(dest, token);
					if (destExists)
					{
						switch (existsMode)
						{
							case FtpExists.Overwrite:
								await DeleteFileAsync(dest, token);
								break;
							case FtpExists.Skip:
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
		public bool MoveDirectory(string path, string dest, FtpExists existsMode = FtpExists.Overwrite) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

			this.LogFunc("MoveDirectory", new object[] { path, dest, existsMode });

			if (DirectoryExists(path)) {

				// check if dest directory exists and act accordingly
				if (existsMode != FtpExists.NoCheck) {
					bool destExists = DirectoryExists(dest);
					if (destExists) {
						switch (existsMode) {
							case FtpExists.Overwrite:
								DeleteDirectory(dest);
								break;
							case FtpExists.Skip:
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

#if !CORE
		delegate bool AsyncMoveDirectory(string path, string dest, FtpExists existsMode);

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
		public IAsyncResult BeginMoveDirectory(string path, string dest, FtpExists existsMode, AsyncCallback callback, object state) {
			AsyncMoveDirectory func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = new AsyncMoveDirectory(MoveDirectory)).BeginInvoke(path, dest, existsMode, callback, state);
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
		/// <param name="token">Cancellation Token</param>
		/// <returns>Whether the directory was moved</returns>
		public async Task<bool> MoveDirectoryAsync(string path, string dest, FtpExists existsMode = FtpExists.Overwrite, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (dest.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "dest");

			this.LogFunc(nameof(MoveDirectoryAsync), new object[] { path, dest, existsMode });

			if (await DirectoryExistsAsync(path, token))
			{

				// check if dest directory exists and act accordingly
				if (existsMode != FtpExists.NoCheck)
				{
					bool destExists = await DirectoryExistsAsync(dest, token);
					if (destExists)
					{
						switch (existsMode)
						{
							case FtpExists.Overwrite:
								await DeleteDirectoryAsync(dest, token);
								break;
							case FtpExists.Skip:
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("SetFilePermissions", new object[] { path, permissions });

				if (!(reply = Execute("SITE CHMOD " + permissions.ToString() + " " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
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
		public async Task SetFilePermissionsAsync(string path, int permissions, CancellationToken token = default(CancellationToken))
		{
			FtpReply reply;

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(SetFilePermissionsAsync), new object[] { path, permissions });

			if (!(reply = await ExecuteAsync("SITE CHMOD " + permissions.ToString() + " " + path.GetFtpPath(), token)).Success)
				throw new FtpCommandException(reply);
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
		public Task ChmodAsync(string path, int permissions, CancellationToken token = default(CancellationToken))
		{
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
			SetFilePermissions(path, CalcChmod(owner, group, other));
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
		public Task SetFilePermissionsAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken))
		{
			return SetFilePermissionsAsync(path, CalcChmod(owner, group, other), token);
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
		public Task ChmodAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken))
		{
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("GetFilePermissions", new object[] { path });

			string fullPath = path.GetFtpPath();
			foreach (FtpListItem i in GetListing(path)) {
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
		public async Task<FtpListItem> GetFilePermissionsAsync(string path, CancellationToken token = default(CancellationToken))
		{
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(GetFilePermissionsAsync), new object[] { path });

			string fullPath = path.GetFtpPath();
			foreach (FtpListItem i in await GetListingAsync(path, token))
			{
				if (i.FullName == fullPath)
				{
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
			FtpListItem item = GetFilePermissions(path);
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
		public async Task<int> GetChmodAsync(string path, CancellationToken token = default(CancellationToken))
		{
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

			this.LogFunc("DereferenceLink", new object[] { item.FullName, recMax });

			int count = 0;
			return DereferenceLink(item, recMax, ref count);
		}

		/// <summary>
		/// Derefence a FtpListItem object
		/// </summary>
		/// <param name="item">The item to derefence</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		FtpListItem DereferenceLink(FtpListItem item, int recMax, ref int count) {
			if (item.Type != FtpFileSystemObjectType.Link)
				throw new FtpException("You can only derefernce a symbolic link. Please verify the item type is Link.");

			if (item.LinkTarget == null)
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");

			foreach (FtpListItem obj in GetListing(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList)) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
						if (++count == recMax)
							return null;

						return DereferenceLink(obj, recMax, ref count);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						DateTime modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue)
							obj.Modified = modify;
					}

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE))
						obj.Size = GetFileSize(obj.FullName);

					return obj;
				}
			}

			return null;
		}

#if !CORE
		delegate FtpListItem AsyncDereferenceLink(FtpListItem item, int recMax);

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
				ar = (func = new AsyncDereferenceLink(DereferenceLink)).BeginInvoke(item, recMax, callback, state);
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
		/// Derefence a FtpListItem object
		/// </summary>
		/// <param name="item">The item to derefence</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		async Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, IntRef count, CancellationToken token = default(CancellationToken))
        {
            if (item.Type != FtpFileSystemObjectType.Link)
                throw new FtpException("You can only derefernce a symbolic link. Please verify the item type is Link.");

            if (item.LinkTarget == null)
                throw new FtpException("The link target was null. Please check this before trying to dereference the link.");

	        var listing = await GetListingAsync(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList, token);

			foreach (FtpListItem obj in listing)
            {
                if (item.LinkTarget == obj.FullName)
                {
                    if (obj.Type == FtpFileSystemObjectType.Link)
                    {
                        if (++count.Value == recMax)
                            return null;

                        return await DereferenceLinkAsync(obj, recMax, count, token);
                    }

                    if (HasFeature(FtpCapability.MDTM))
                    {
                        DateTime modify = GetModifiedTime(obj.FullName);

                        if (modify != DateTime.MinValue)
                            obj.Modified = modify;
                    }

                    if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE))
                        obj.Size = GetFileSize(obj.FullName);

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
            this.LogFunc(nameof(DereferenceLinkAsync), new object[] { item.FullName, recMax });

            IntRef count = new IntRef { Value = 0 };
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

		#region Set Working Dir

		/// <summary>
		/// Sets the work directory on the server
		/// </summary>
		/// <param name="path">The path of the directory to change to</param>
		/// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
		public void SetWorkingDirectory(string path) {

			this.LogFunc("SetWorkingDirectory", new object[] { path });

			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./")
				return;

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("CWD " + ftppath)).Success)
					throw new FtpCommandException(reply);
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncSetWorkingDirectory(string path);

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
				ar = (func = new AsyncSetWorkingDirectory(SetWorkingDirectory)).BeginInvoke(path, callback, state);
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
		/// <param name="token">Cancellation Token</param>
		public async Task SetWorkingDirectoryAsync(string path, CancellationToken token = default(CancellationToken))
		{
			this.LogFunc(nameof(SetWorkingDirectoryAsync), new object[] { path });

			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./")
				return;

			if (!(reply = await ExecuteAsync("CWD " + ftppath, token)).Success)
				throw new FtpCommandException(reply);
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

			this.LogFunc("GetWorkingDirectory");

			FtpReply reply;
			Match m;

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("PWD")).Success)
					throw new FtpCommandException(reply);
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

			this.LogStatus(FtpTraceLevel.Warn, "Failed to parse working directory from: " + reply.Message);

			return "./";
		}

#if !CORE
		delegate string AsyncGetWorkingDirectory();

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
				ar = (func = new AsyncGetWorkingDirectory(GetWorkingDirectory)).BeginInvoke(callback, state);
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
            this.LogFunc(nameof(GetWorkingDirectoryAsync));

            FtpReply reply;
            Match m;

            if (!(reply = await ExecuteAsync("PWD", token)).Success)
                throw new FtpCommandException(reply);

            if ((m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success)
            {
                return m.Groups["pwd"].Value;
            }

            // check for MODCOMP ftp path mentioned in forums: https://netftp.codeplex.com/discussions/444461
            if ((m = Regex.Match(reply.Message, "PWD = (?<pwd>.*)")).Success)
            {
                return m.Groups["pwd"].Value;
            }

            this.LogStatus(FtpTraceLevel.Warn, "Failed to parse working directory from: " + reply.Message);

            return "./";
        }
#endif
		#endregion

		#region Get File Size

		/// <summary>
		/// Gets the size of a remote file
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <returns>-1 if the command fails, otherwise the file size</returns>
		/// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
		public virtual long GetFileSize(string path) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("GetFileSize", new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) {
				return -1;
			}

			FtpReply reply;
			long length = 0;

#if !CORE14
			lock (m_lock) {
#endif
				// Switch to binary mode since some servers don't support SIZE command for ASCII files.
				// 
				// NOTE: We do this inside the lock so we're guaranteed to switch it back to the original
				// type in a thread-safe manner
				var savedDataType = CurrentDataType;
				if (savedDataType != FtpDataType.Binary) {
					this.SetDataTypeInternal(FtpDataType.Binary);
				}

				if (!(reply = Execute("SIZE " + path.GetFtpPath())).Success)
					length = -1;
				else if (!long.TryParse(reply.Message, out length))
					length = -1;

				if (savedDataType != FtpDataType.Binary)
					this.SetDataTypeInternal(savedDataType);
#if !CORE14
			}
#endif


			return length;
		}

#if !CORE
		delegate long AsyncGetFileSize(string path);

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
				ar = (func = new AsyncGetFileSize(GetFileSize)).BeginInvoke(path, callback, state);
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
		/// Retrieve the size of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		public async Task<long> GetFileSizeAsync(string path, CancellationToken token = default(CancellationToken)) {
            // verify args
            if (path.IsBlank())
                throw new ArgumentException("Required parameter is null or blank.", "path");

            this.LogFunc(nameof(GetFileSizeAsync), new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) { 
				return -1;
			}

            FtpReply reply;
            long length = 0;

            // Switch to binary mode since some servers don't support SIZE command for ASCII files.
            // 
            var savedDataType = CurrentDataType;
            if (savedDataType != FtpDataType.Binary)
            {
                await this.SetDataTypeAsync(FtpDataType.Binary, token);
            }

            if (!(reply = await ExecuteAsync("SIZE " + path.GetFtpPath(), token)).Success)
                length = -1;
            else if (!long.TryParse(reply.Message, out length))
                length = -1;

            if (savedDataType != FtpDataType.Binary)
                await this.SetDataTypeAsync(savedDataType, token);

            return length;
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("GetModifiedTime", new object[] { path, type });

			DateTime date = DateTime.MinValue;
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif
				// get modified date of a file
				if ((reply = Execute("MDTM " + path.GetFtpPath())).Success) {
					date = reply.Message.GetFtpDate(DateTimeStyles.AssumeUniversal);

					// convert server timezone to UTC, based on the TimeOffset property
					if (type != FtpDate.Original && m_listParser.hasTimeOffset) {
						date = (date - m_listParser.timeOffset);
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
		delegate DateTime AsyncGetModifiedTime(string path, FtpDate type);

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
				ar = (func = new AsyncGetModifiedTime(GetModifiedTime)).BeginInvoke(path, type, callback, state);
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
            if (path.IsBlank())
                throw new ArgumentException("Required parameter is null or blank.", "path");

            this.LogFunc(nameof(GetModifiedTimeAsync), new object[] { path, type });

            DateTime date = DateTime.MinValue;
            FtpReply reply;

            // get modified date of a file
            if ((reply = await ExecuteAsync("MDTM " + path.GetFtpPath(), token)).Success)
            {
                date = reply.Message.GetFtpDate(DateTimeStyles.AssumeUniversal);

                // convert server timezone to UTC, based on the TimeOffset property
                if (type != FtpDate.Original && m_listParser.hasTimeOffset)
                {
                    date = (date - m_listParser.timeOffset);
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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (date == null)
				throw new ArgumentException("Required parameter is null or blank.", "date");

			this.LogFunc("SetModifiedTime", new object[] { path, date, type });

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
				if (type != FtpDate.Original && m_listParser.hasTimeOffset) {
					date = (date + m_listParser.timeOffset);
				}

				// set modified date of a file
				string timeStr = date.ToString("yyyyMMddHHmmss");
				if ((reply = Execute("MFMT " + timeStr + " " + path.GetFtpPath())).Success) {

				}
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncSetModifiedTime(string path, DateTime date, FtpDate type);

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
				ar = (func = new AsyncSetModifiedTime(SetModifiedTime)).BeginInvoke(path, date, type, callback, state);
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
		public async Task SetModifiedTimeAsync(string path, DateTime date, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken))
		{
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			if (date == null)
				throw new ArgumentException("Required parameter is null or blank.", "date");

			this.LogFunc(nameof(SetModifiedTimeAsync), new object[] { path, date, type });

			FtpReply reply;

			// convert local to UTC if wanted
#if !CORE
			if (type == FtpDate.Local) {
				date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
			}
#endif

			// convert UTC to server timezone, based on the TimeOffset property
			if (type != FtpDate.Original && m_listParser.hasTimeOffset)
			{
				date = (date + m_listParser.timeOffset);
			}

			// set modified date of a file
			string timeStr = date.ToString("yyyyMMddHHmmss");
			if ((reply = await ExecuteAsync("MFMT " + timeStr + " " + path.GetFtpPath(), token)).Success)
			{

			}
		}
#endif

		#endregion

	}
}