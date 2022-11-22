using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

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
		public async Task<bool> MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(dest));
			}

			path = path.GetFtpPath();
			dest = dest.GetFtpPath();

			LogFunction(nameof(MoveFile), new object[] { path, dest, existsMode });

			if (existsMode != FtpRemoteExists.NoCheck) {
				if (await FileExists(path, token)) {
					// check if dest file exists and act accordingly
					bool destExists = await FileExists(dest, token);
					if (destExists) {
						switch (existsMode) {
							case FtpRemoteExists.Overwrite:
								await DeleteFile(dest, token);
								break;

							case FtpRemoteExists.Skip:
								return false;
						}
					}
				}
				else {
					return false;
				}
			}

			// move the file
			await Rename(path, dest, token);

			return true;

		}

	}
}