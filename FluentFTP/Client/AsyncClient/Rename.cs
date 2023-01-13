using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Renames an object on the remote file system asynchronously.
		/// Low level method that should NOT be used in most cases. Prefer MoveFile() and MoveDirectory().
		/// Throws exceptions if the file does not exist, or if the destination file already exists.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The new full or relative path including the new name of the object</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task Rename(string path, string dest, CancellationToken token = default(CancellationToken)) {
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
			path = await GetAbsolutePathAsync(path, token);
			dest = await GetAbsolutePathAsync(dest, token);

			if (!(reply = await Execute("RNFR " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			if (!(reply = await Execute("RNTO " + dest, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}

	}
}