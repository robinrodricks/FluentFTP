using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Ensure a relative dir is absolute by prepending the working dir
		/// </summary>
		protected string GetAbsoluteDir(string path) {
			string dirPath = null;
			if (ServerHandler != null && ServerHandler.IsCustomGetAbsoluteDir()) {
				dirPath = ServerHandler.GetAbsoluteDir(this, path);
			}

			if (dirPath != null) {
				return dirPath;
			}

			path = GetAbsolutePath(path);

			path = !path.EndsWith("/") ? path + "/" : path;

			return path;
		}

#if ASYNC
		/// <summary>
		/// Ensure a relative dir is absolute by prepending the working dir
		/// </summary>
		protected async Task<string> GetAbsoluteDirAsync(string path, CancellationToken token) {
			string dirPath = null;
			if (ServerHandler != null && ServerHandler.IsCustomGetAbsoluteDir()) {
				dirPath = await ServerHandler.GetAbsoluteDirAsync(this, path, token);
			}

			if (dirPath != null) {
				return dirPath;
			}

			path = await GetAbsolutePathAsync(path, token);

			path = !path.EndsWith("/") ? path + "/" : path;

			return path;
		}
#endif

	}
}
