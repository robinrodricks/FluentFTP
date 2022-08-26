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


	}
}
