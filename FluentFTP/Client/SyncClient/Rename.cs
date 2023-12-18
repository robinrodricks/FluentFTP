using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Renames an object on the remote file system.
		/// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
		/// Throws exceptions if the file does not exist, or if the destination file already exists.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		public void Rename(string path, string dest) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			if (dest.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(dest));
			}

			path = path.GetFtpPath();
			dest = dest.GetFtpPath();

			LogFunction(nameof(Rename), new object[] { path, dest });

			// calc the absolute filepaths
			path = GetAbsolutePath(path);
			dest = GetAbsolutePath(dest);

			if (!(reply = Execute("RNFR " + path)).Success) {
				throw new FtpCommandException(reply);
			}

			if (!(reply = Execute("RNTO " + dest)).Success) {
				throw new FtpCommandException(reply);
			}

		}

	}
}