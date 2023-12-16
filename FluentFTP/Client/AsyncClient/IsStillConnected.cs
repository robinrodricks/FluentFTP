using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
		/// </summary>
		public async Task<bool> IsStillConnected(CancellationToken token = default(CancellationToken)) {
			if (IsConnected && IsAuthenticated) {
				if (await NoopAsync(token)) {
					if ((await GetReply(token)).Success) {
						return true;
					}
				}
			}
			return false;
		}

	}
}
