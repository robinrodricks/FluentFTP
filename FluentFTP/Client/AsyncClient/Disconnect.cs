using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
		public async Task Disconnect(CancellationToken token = default(CancellationToken)) {
			LogFunction(nameof(Disconnect), null);

			if (m_stream != null && m_stream.IsConnected) {
				try {
					if (Config.DisconnectWithQuit) {
						await Execute("QUIT", token);
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Warn, "AsyncFtpClient.Disconnect(): Exception caught and discarded while closing control connection", ex);
				}
				finally {
					m_stream.Close();
				}
			}
		}

	}
}
