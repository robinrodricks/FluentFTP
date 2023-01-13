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
		/// Gets the current working directory asynchronously
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		public async Task<string> GetWorkingDirectory(CancellationToken token = default(CancellationToken)) {

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				await ReadCurrentWorkingDirectory(token);
			}

			return Status.LastWorkingDir;
		}

		/// <summary>
		/// Get the reply from the PWD command
		/// </summary>
		/// <returns>The current working directory reply.</returns>
		protected async Task<FtpReply> ReadCurrentWorkingDirectory(CancellationToken token) {

			FtpReply reply;

			// read the absolute path of the current working dir
			if (!(reply = await Execute("PWD", token)).Success) {
				throw new FtpCommandException(reply);
			}

			// cache the last working dir
			Status.LastWorkingDir = ParseWorkingDirectory(reply);
			return reply;
		}

	}
}
