using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
		/// </summary>
		/// <param name="timeout"/>How to wait for connection confirmation
		/// <returns>bool connection status</returns>
		bool IInternalFtpClient.IsStillConnectedInternal(int timeout) {

			bool connected = false;
			if (IsConnected && IsAuthenticated) {
				try {
					if (((IInternalFtpClient)this).NoopInternal(true)) {
						var rep = ((IInternalFtpClient)this).GetReplyInternal("NOOP (<-IsStillConnected/Noop)", false, timeout);
						// Successfully getting a reply from a NOOP command means we are still connected, UNLESS the reply code is 421,
						// which means the server is closing the connection after this command and we are then no longer connected.
						// From the RFC: 421 Service NOT available, closing control connection. This may be a reply to any command if the service knows it must shut down.
						if (rep.Code != "421") {
							connected = true;
						}
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Exception: " + ex.Message);
				}
				if (!connected) {
					// This will clean up the SocketStream
					LogWithPrefix(FtpTraceLevel.Verbose, "IsStillConnected: Disconnecting for cleanup");
					bool saveDisconnectWithQuit = Config.DisconnectWithQuit;
					Config.DisconnectWithQuit = false;
					((IInternalFtpClient)this).DisconnectInternal();
					Config.DisconnectWithQuit = saveDisconnectWithQuit;
				}
			}
			if (!connected) {
				LogWithPrefix(FtpTraceLevel.Verbose, "IsStillConnected: Control connection is not connected");
			}
			return connected;
		}

	}
}
