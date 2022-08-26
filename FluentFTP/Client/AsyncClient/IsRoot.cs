using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

#if ASYNC
		/// <summary>
		/// Is the current working directory the root?
		/// </summary>
		/// <returns>true if root.</returns>
		public async Task<bool> IsRootAsync(CancellationToken token = default(CancellationToken)) {

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				await ReadCurrentWorkingDirectoryAsync(token);
			}

			if (Status.LastWorkingDir.IsFtpRootDirectory()) {
				return true;
			}

			// execute server-specific check if the current working dir is a root directory
			if (ServerHandler != null && ServerHandler.IsRoot(this, Status.LastWorkingDir)) {
				return true;
			}

			return false;
		}
#endif
	}
}
