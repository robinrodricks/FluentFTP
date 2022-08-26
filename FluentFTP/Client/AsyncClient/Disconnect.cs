using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

#if ASYNC
		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
		public async Task DisconnectAsync(CancellationToken token = default(CancellationToken)) {
			if (m_stream != null && m_stream.IsConnected) {
				try {
					if (DisconnectWithQuit) {
						await ExecuteAsync("QUIT", token);
					}
				}
				catch (Exception ex) {
					LogStatus(FtpTraceLevel.Warn, "AsyncFtpClient.Disconnect(): Exception caught and discarded while closing control connection: " + ex.ToString());
				}
				finally {
					m_stream.Close();
				}
			}
		}
#endif

	}
}
