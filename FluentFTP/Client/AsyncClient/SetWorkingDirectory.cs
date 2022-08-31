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
		/// Sets the working directory on the server asynchronously
		/// </summary>
		/// <param name="path">The directory to change to</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetWorkingDirectory(string path, CancellationToken token = default(CancellationToken)) {

			path = path.GetFtpPath();

			LogFunction(nameof(SetWorkingDirectory), new object[] { path });

			FtpReply reply;

			// exit if invalid path
			if (path == "." || path == "./") {
				return;
			}

			// modify working dir
			if (!(reply = await Execute("CWD " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// invalidate the cached path
			// This is redundant, Execute(...) will see the CWD and do this
			//Status.LastWorkingDir = null;
		}


#endif

	}
}
