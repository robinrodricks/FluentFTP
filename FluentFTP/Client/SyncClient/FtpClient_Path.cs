using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Ensure a relative path is absolute by prepending the working dir
		/// </summary>
		protected string GetAbsolutePath(string path) {

			if (ServerHandler != null && ServerHandler.IsCustomGetAbsolutePath()) {
				return ServerHandler.GetAbsolutePath(this, path);
			}

			if (path == null || path.Trim().Length == 0) {
				// if path not given, then use working dir
				var pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0) {
					path = pwd;
				}
				else {
					path = "/";
				}
			}

			// FIX : #153 ensure this check works with unix & windows
			// FIX : #454 OpenVMS paths can be a single character
			else if (!path.StartsWith("/") && !(path.Length > 1 && path[1] == ':')) {

				// if its a server-specific absolute path then don't add base dir
				if (ServerHandler != null && ServerHandler.IsAbsolutePath(path)) {
					return path;
				}

				// if relative path given then add working dir to calc full path
				var pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0 && path != pwd) {
					if (path.StartsWith("./")) {
						path = path.Remove(0, 2);
					}

					path = (pwd + "/" + path).GetFtpPath();
				}
			}

			return path;
		}

#if ASYNC
		/// <summary>
		/// Ensure a relative path is absolute by prepending the working dir
		/// </summary>
		protected async Task<string> GetAbsolutePathAsync(string path, CancellationToken token) {

			if (ServerHandler != null && ServerHandler.IsCustomGetAbsolutePath()) {
				return await ServerHandler.GetAbsolutePathAsync(this, path, token);
			}

			if (path == null || path.Trim().Length == 0) {
				// if path not given, then use working dir
				string pwd = await GetWorkingDirectoryAsync(token);
				if (pwd != null && pwd.Trim().Length > 0) {
					path = pwd;
				}
				else {
					path = "/";
				}
			}

			// FIX : #153 ensure this check works with unix & windows
			// FIX : #454 OpenVMS paths can be a single character
			else if (!path.StartsWith("/") && !(path.Length > 1 && path[1] == ':')) {

				// if its a server-specific absolute path then don't add base dir
				if (ServerHandler != null && ServerHandler.IsAbsolutePath(path)) {
					return path;
				}

				// if relative path given then add working dir to calc full path
				string pwd = await GetWorkingDirectoryAsync(token);
				if (pwd != null && pwd.Trim().Length > 0 && path != pwd) {
					if (path.StartsWith("./")) {
						path = path.Remove(0, 2);
					}

					path = (pwd + "/" + path).GetFtpPath();
				}
			}

			return path;
		}
#endif

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

#if ASYNC
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
#endif


	}
}
