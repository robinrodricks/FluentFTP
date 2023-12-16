using System;
using System.ComponentModel.Design;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
		/// <paramref name="timeout"/>How to wait for connection confirmation
		/// </summary>
		public async Task<bool> IsStillConnected(int timeout = 10000, CancellationToken token = default(CancellationToken)) {
			bool connected = false;
			if (IsConnected && IsAuthenticated) {
				int saveNoopInterval = Config.NoopInterval;
				LastCommandTimestamp = DateTime.MinValue;
				Config.NoopInterval = 1;
				if (await NoopAsync(token)) {
					try {
						if ((await GetReplyAsyncInternal(token, "NOOP", false, timeout)).Success) {
							connected = true;
						}
					}
					catch { }
				}
				Config.NoopInterval = saveNoopInterval;
				if (!connected) {
					// This will clean up the SocketStream
					bool saveDisconnectWithQuit = Config.DisconnectWithQuit;
					Config.DisconnectWithQuit = false;
					await Disconnect(token);
					Config.DisconnectWithQuit = saveDisconnectWithQuit;
				}
			}
			return connected;
		}

	}
}
