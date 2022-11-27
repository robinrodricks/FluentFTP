using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

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
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(dest));
			}

			path = path.GetFtpPath();
			dest = dest.GetFtpPath();

			LogFunction(nameof(MoveFile), new object[] { path, dest, existsMode });

			if (existsMode != FtpRemoteExists.NoCheck) {
				if (FileExists(path)) {
					// check if dest file exists and act accordingly
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
				else {
					return false;
				}
			}

			// move the file
			Rename(path, dest);

			return true;

			}

	}
}