using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
		/// </summary>
		/// <param name="timeout"/>How to wait for connection confirmation
		/// <returns>bool connection status</returns>
		public bool IsStillConnected(int timeout = 10000) {
			LogFunction(nameof(IsStillConnected), new object[] { timeout });

			bool connected = false;
			if (IsConnected && IsAuthenticated) {
				try {
					if (Noop(true) && GetReplyInternal("NOOP", false, timeout).Success) {
						connected = true;
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Exception: " + ex.Message);
				}
				if (!connected) {
					// This will clean up the SocketStream
					bool saveDisconnectWithQuit = Config.DisconnectWithQuit;
					Config.DisconnectWithQuit = false;
					Disconnect();
					Config.DisconnectWithQuit = saveDisconnectWithQuit;
				}
			}
			if (!connected) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Control connections is not connected");
			}
			return connected;
		}

	}
}
