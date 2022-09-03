using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
		/// Throws FtpCommandException if there is an issue.
		/// Returns null if the server did not specify a permission value.
		/// Use `GetChmod` if you required the integer value instead.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		public FtpListItem GetFilePermissions(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetFilePermissions), new object[] { path });

			var result = GetObjectInfo(path);

			return result;
		}

	}
}
