using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Asynchronously removes a directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task DeleteDirectory(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(DeleteDirectory), new object[] { path });
			return DeleteDirInternalAsync(path, true, FtpListOption.Recursive, true, true, token);
		}

		/// <summary>
		/// Asynchronously removes a directory and all its contents.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task DeleteDirectory(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(DeleteDirectory), new object[] { path, options });
			return DeleteDirInternalAsync(path, true, options, true, true, token);
		}

		/// <summary>
		/// Asynchronously removes a directory. Used by <see cref="DeleteDirectory(string, CancellationToken)"/> and
		/// <see cref="DeleteDirectory(string, FtpListOption, CancellationToken)"/>.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="deleteContents">Delete the contents before deleting the folder</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="deleteFinalDir">Delete the top level dir too</param>
		/// <param name="firstCall">Internally used to determine top level</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns></returns>
		protected async Task DeleteDirInternalAsync(string path, bool deleteContents, FtpListOption options, bool deleteFinalDir, bool firstCall, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			path = path.GetFtpPath();

			// server-specific directory deletion
			// don't use it if requested to leave the top level dir, because
			// server specific RMDIRs usually brutally delete all
			if (deleteFinalDir && !path.IsFtpRootDirectory()) {

				// ask the server handler to delete a directory
				if (ServerHandler != null) {
					if (await ServerHandler.DeleteDirectoryAsync(this, path, path, deleteContents, options, token)) {
						return;
					}
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
					itemList = await GetListing(path, options, token);
				}
				else {
					itemList = (await GetListing(path, options, token)).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();
				}

				// delete the item based on the type
				foreach (var item in itemList) {
					switch (item.Type) {
						case FtpObjectType.File:
							await DeleteFile(item.FullName, token);
							break;

						case FtpObjectType.Directory:
							await DeleteDirInternalAsync(item.FullName, recurse, options, true, false, token);
							break;

						default:
							throw new FtpException("Don't know how to delete object type: " + item.Type);
					}
				}
			}

			// SKIP DELETING ROOT DIRS

			// can't delete the working directory and
			// can't delete the server root.
			if (path.IsFtpRootDirectory()) {
				return;
			}

			// DELETE ACTUAL DIRECTORY

			if (!firstCall || deleteFinalDir) {
				if (!(reply = await Execute("RMD " + path, token)).Success) {
					throw new FtpCommandException(reply);
				}
			}
		}

	}
}
