using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
 		/// <paramref name="timeout"/>How to wait for connection confirmation
		/// </summary>
		public bool IsStillConnected(int timeout = 10000) {
			bool connected = false;
			if (IsConnected && IsAuthenticated) {
				int saveNoopInterval = Config.NoopInterval;
				LastCommandTimestamp = DateTime.MinValue;
				Config.NoopInterval = 1;
				if (Noop()) {
					if (GetReplyInternal("NOOP", false, timeout).Success) {
						connected = true;
					}
				}
				Config.NoopInterval = saveNoopInterval;
				if (!connected) {
					// This will clean up the SocketStream
					bool saveDisconnectWithQuit = Config.DisconnectWithQuit;
					Config.DisconnectWithQuit = false;
					Disconnect();
					Config.DisconnectWithQuit = saveDisconnectWithQuit;
				}
			}
			return connected;
		}

	}
}
