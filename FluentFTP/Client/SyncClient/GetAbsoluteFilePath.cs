using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Concat a path and a filename
		/// </summary>
		protected string GetAbsoluteFilePath(string path, string fileName) {
			string filePath = null;
			if (ServerHandler != null && ServerHandler.IsCustomGetAbsoluteFilePath()) {
				filePath = ServerHandler.GetAbsoluteFilePath(this, path, fileName);
			}

			if (filePath != null) {
				return filePath;
			}

			path = !path.EndsWith("/") ? path + "/" + fileName : path + fileName;

			return path;
		}

	}
}
