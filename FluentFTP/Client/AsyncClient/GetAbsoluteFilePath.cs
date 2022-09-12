using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Concat a path and a filename
		/// </summary>
		protected async Task<string> GetAbsoluteFilePathAsync(string path, string fileName, CancellationToken token) {
			string filePath = null;
			if (ServerHandler != null && ServerHandler.IsCustomGetAbsoluteFilePath()) {
				filePath = await ServerHandler.GetAbsoluteFilePathAsync(this, path, fileName, token);
			}

			if (filePath != null) {
				return filePath;
			}

			path = !path.EndsWith("/") ? path + "/" + fileName : path + fileName;

			return path;
		}

	}
}
