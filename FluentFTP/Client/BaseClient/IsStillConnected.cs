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
		bool IInternalFtpClient.IsStillConnectedInternal(int timeout = 10000) {

			bool connected = false;
			if (IsConnected && IsAuthenticated) {
				try {
					if (((IInternalFtpClient)this).NoopInternal(true) && ((IInternalFtpClient)this).GetReplyInternal("NOOP (<-IsStillConnected/Noop)", false, timeout).Success) {
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
					((IInternalFtpClient)this).DisconnectInternal();
					Config.DisconnectWithQuit = saveDisconnectWithQuit;
				}
			}
			if (!connected) {
				LogWithPrefix(FtpTraceLevel.Verbose, "IsStillConnected: Control connections is not connected");
			}
			return connected;
		}

	}
}
