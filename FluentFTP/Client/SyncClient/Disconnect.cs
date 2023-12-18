using System;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		public void Disconnect() {
			LogFunction(nameof(Disconnect), null);

			if (m_stream != null && m_stream.IsConnected) {
				try {
					if (Config.DisconnectWithQuit) {
						Execute("QUIT");
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Warn, "FtpClient.Disconnect(): Exception caught and discarded while closing control connection", ex);
				}
				finally {
					m_stream.Close();
				}
			}
		}

	}
}
