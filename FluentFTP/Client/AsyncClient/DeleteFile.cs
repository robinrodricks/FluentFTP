using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Deletes a file from the server asynchronously
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task DeleteFile(string path, CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(DeleteFile), new object[] { path });

			if (!(reply = await Execute("DELE " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}
		}

	}
}