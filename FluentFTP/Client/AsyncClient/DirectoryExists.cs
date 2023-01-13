using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Tests if the specified directory exists on the server asynchronously. This
		/// method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <param name='path'>The full or relative path of the directory to check for</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if the directory exists. False otherwise.</returns>
		public async Task<bool> DirectoryExists(string path, CancellationToken token = default(CancellationToken)) {
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
			pwd = await GetWorkingDirectory(token);

			if ((await Execute("CWD " + path, token)).Success) {
				FtpReply reply = await Execute("CWD " + pwd, token);

				if (!reply.Success) {
					throw new FtpException("DirectoryExists(): Failed to restore the working directory.");
				}

				return true;
			}

			return false;
		}

	}
}
