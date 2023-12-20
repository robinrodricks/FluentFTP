using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Tests if the specified directory exists on the server. This
		/// method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <param name="path">The path of the directory</param>
		/// <returns>True if it exists, false otherwise.</returns>
		public bool DirectoryExists(string path) {
			string pwd;

			// don't verify args as blank/null path is OK
			//if (path.IsBlank())
			//	throw new ArgumentException("Required parameter is null or blank.", "path");

			path = path.GetFtpPath();

			LogFunction(nameof(DirectoryExists), new object[] { path });

			// quickly check if root path, then it always exists!
			if (path.IsFtpRootDirectory()) {
				return true;
			}

			// check if a folder exists by changing the working dir to it
			pwd = GetWorkingDirectory();

			if (Execute("CWD " + path).Success) {
				var reply = Execute("CWD " + pwd);

				if (!reply.Success) {
					throw new FtpException("DirectoryExists(): Failed to restore the working directory.");
				}

				return true;
			}


			return false;
		}

	}
}
