using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Deletes a file on the server
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		public void DeleteFile(string path) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(DeleteFile), new object[] { path });

			if (!(reply = Execute("DELE " + path)).Success) {
				throw new FtpCommandException(reply);
			}
		}

	}
}