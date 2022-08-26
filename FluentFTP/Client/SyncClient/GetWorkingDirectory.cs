using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		public string GetWorkingDirectory() {
			return ((IInternalFtpClient)this).GetWorkingDirectoryInternal();
		}
#if ASYNC
		/// <summary>
		/// Gets the current working directory asynchronously
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		public async Task<string> GetWorkingDirectoryAsync(CancellationToken token = default(CancellationToken)) {

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				await ReadCurrentWorkingDirectoryAsync(token);
			}

			return Status.LastWorkingDir;
		}

#endif
#if ASYNC

		protected async Task<FtpReply> ReadCurrentWorkingDirectoryAsync(CancellationToken token) {

			FtpReply reply;

			// read the absolute path of the current working dir
			if (!(reply = await ExecuteAsync("PWD", token)).Success) {
				throw new FtpCommandException(reply);
			}

			// cache the last working dir
			Status.LastWorkingDir = ParseWorkingDirectory(reply);
			return reply;
		}
#endif

	}
}
