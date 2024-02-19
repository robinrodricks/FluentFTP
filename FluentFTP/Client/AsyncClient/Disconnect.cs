using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
		public async Task Disconnect(CancellationToken token = default(CancellationToken)) {
			LogFunction(nameof(Disconnect), null);

			if (IsConnected) {
				try {
					if (Config.DisconnectWithQuit) {
						await Execute("QUIT", token);
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Warn, "AsyncFtpClient.Disconnect(): Exception caught and discarded while closing control connection", ex);
				}
				finally {
					// When debugging, the stream might have already been taken down
					// from the remote side, thus causing an exception here, so check for null
					if (m_stream != null) {
						await m_stream.CloseAsync(token);
					}
				}
			}
		}

	}
}
