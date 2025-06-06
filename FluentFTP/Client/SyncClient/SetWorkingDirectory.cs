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
		/// Sets the work directory on the server
		/// </summary>
		/// <param name="path">The path of the directory to change to</param>
		public void SetWorkingDirectory(string path) {

			path = path.GetFtpPath();

			LogFunction(nameof(SetWorkingDirectory), new object[] { path });

			FtpReply reply;

			// exit if invalid path
			if (path is "." or "./") {
				return;
			}

			// If PreserveTrailingSlashCmdList enabled for CWD... but: Don't do it for root dir and any
			// directories that already end with a slash (which shouldn't happen, but let's be safe)
			if (Config.PreserveTrailingSlashCmdList != null && Config.PreserveTrailingSlashCmdList.Contains("CWD") && !path.EndsWith("/")) {
				path += "/";
			}

			// modify working dir
			if (!(reply = Execute("CWD " + path)).Success) {
				throw new FtpCommandException(reply);
			}

		}

	}
}
