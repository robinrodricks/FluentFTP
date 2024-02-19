using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		public void Disconnect() {
			LogFunction(nameof(Disconnect), null);

			if (IsConnected) {
				try {
					if (Config.DisconnectWithQuit) {
						Execute("QUIT");
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Warn, "FtpClient.Disconnect(): Exception caught and discarded while closing control connection", ex);
				}
				finally {
					// When debugging, the stream might have already been taken down
					// from the remote side, thus causing an exception here, so check for null
					if (m_stream != null) {
						m_stream.Close();
					}
				}
			}
		}

	}
}
