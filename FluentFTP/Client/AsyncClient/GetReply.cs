using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <returns>FtpReply representing the response from the server</returns>
		public async Task<FtpReply> GetReply(CancellationToken token = default(CancellationToken)) {
			return await ((IInternalFtpClient)this).GetReplyInternal(token, null, false, 0);
		}
	}
}