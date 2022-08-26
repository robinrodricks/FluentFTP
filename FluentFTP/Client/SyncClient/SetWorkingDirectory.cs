using System;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Sets the work directory on the server
		/// </summary>
		/// <param name="path">The path of the directory to change to</param>
		public void SetWorkingDirectory(string path) {

			path = path.GetFtpPath();

			LogFunc(nameof(SetWorkingDirectory), new object[] { path });

			FtpReply reply;

			// exit if invalid path
			if (path == "." || path == "./") {
				return;
			}

			lock (m_lock) {
				// modify working dir
				if (!(reply = Execute("CWD " + path)).Success) {
					throw new FtpCommandException(reply);
				}

				// invalidate the cached path
				// This is redundant, Execute(...) will see the CWD and do this
				//Status.LastWorkingDir = null;

			}
		}


#if ASYNC
		/// <summary>
		/// Sets the working directory on the server asynchronously
		/// </summary>
		/// <param name="path">The directory to change to</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetWorkingDirectoryAsync(string path, CancellationToken token = default(CancellationToken)) {

			path = path.GetFtpPath();

			LogFunc(nameof(SetWorkingDirectoryAsync), new object[] { path });

			FtpReply reply;

			// exit if invalid path
			if (path == "." || path == "./") {
				return;
			}

			// modify working dir
			if (!(reply = await ExecuteAsync("CWD " + path, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// invalidate the cached path
			// This is redundant, Execute(...) will see the CWD and do this
			//Status.LastWorkingDir = null;
		}


#endif

	}
}
