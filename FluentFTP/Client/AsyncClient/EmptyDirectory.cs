using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Deletes the contents of the specified directory, without deleting the directory itself.
		/// </summary>
		/// <param name="path">The full or relative path of the directorys contents to delete</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task EmptyDirectory(string path, CancellationToken token = default(CancellationToken)) {
			return EmptyDirectory(path, FtpListOption.Recursive, token);
		}

		/// <summary>
		/// Deletes the contents of the specified directory, without deleting the directory itself.
		/// </summary>
		/// <param name="path">The full or relative path of the directorys contents to delete</param>
		/// <param name="options">Useful to delete hidden files or dot-files.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public Task EmptyDirectory(string path, FtpListOption options = FtpListOption.Recursive, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(EmptyDirectory), new object[] { path, options });
			return DeleteDirInternalAsync(path, true, options, false, true, token);
		}


	}
}
