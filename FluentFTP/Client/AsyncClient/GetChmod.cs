using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an integer in the CHMOD format.
		/// Throws FtpCommandException if there is an issue.
		/// Returns 0 if the server did not specify a permission value.
		/// Use `GetFilePermissions` if you required the permissions in the FtpPermission format.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task<int> GetChmod(string path, CancellationToken token = default(CancellationToken)) {
			FtpListItem item = await GetFilePermissions(path, token);
			return item != null ? item.Chmod : 0;
		}

	}
}
