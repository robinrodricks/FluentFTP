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

			// modify working dir
			if (!(reply = Execute("CWD " + path)).Success) {
				throw new FtpCommandException(reply);
			}

		}

	}
}
