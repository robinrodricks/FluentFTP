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
			lock (m_lock) {
				if (m_stream != null && m_stream.IsConnected) {
					try {
						if (DisconnectWithQuit) {
							Execute("QUIT");
						}
					}
					catch (Exception ex) {
						LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): Exception caught and discarded while closing control connection: " + ex.ToString());
					}
					finally {
						m_stream.Close();
					}
				}

			}
		}

	}
}
