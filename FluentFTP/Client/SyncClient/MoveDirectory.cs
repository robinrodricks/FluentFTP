using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

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

			path = path.GetFtpPath();
			dest = dest.GetFtpPath();

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

			path = path.GetFtpPath();
			dest = dest.GetFtpPath();

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

	}
}
