using System;
using System.Threading;

namespace FluentFTP {
	public partial class FtpClient {


		/// <summary>
		/// Attempt to find out the servers SSL command limit.
		/// </summary>
		/// <param name="command">The command to issue</param>
		/// <param name="maxTrys">Maximum how many commands to issue</param>
		/// <param name="delay">How many ms to wait between commands</param>
		/// <returns>The detected command limit, 0 if infinite</returns>
		public int FindSrvCmdLimit(string command = "PWD", int maxTrys = 2000, int delay = 50) {
			if (!IsEncrypted) {
				return 0;
			}

			int connects = Status.ConnectCount;

			int oldLength = Config.SslSessionLength;

			Config.SslSessionLength = 0;

			for (int i = 0; i < maxTrys; i++) {
				Thread.Sleep(delay);

				try {
					Execute(command);
				}
				catch (Exception ex) {
					Log(FtpTraceLevel.Verbose, "Exception happened: " + ex.Message);
					break;
				}

				if (Status.ConnectCount > connects) {
					Log(FtpTraceLevel.Verbose, "Reconnect detected");
					break;
				}
			}

			Execute(command);

			Config.SslSessionLength = oldLength;

			if (Status.ConnectCount > connects) {
				Log(FtpTraceLevel.Verbose, "************************************************");
				Log(FtpTraceLevel.Verbose, "Failure ocurred at: " + m_stream.SslSessionLength);
				Log(FtpTraceLevel.Verbose, "************************************************");
				return m_stream.SslSessionLength;
			}

			return 0;
		}
	}
}