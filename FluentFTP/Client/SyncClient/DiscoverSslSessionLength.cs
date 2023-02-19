using System;

namespace FluentFTP {
	public partial class FtpClient {


		/// <summary>
		/// Automatically discover the SSL command limit of your currently connected FTP server.
		/// It returns the value that can be used to set SslSessionLength.
		/// </summary>
		/// <param name="command">The command to issue</param>
		/// <param name="maxTries">Maximum number of commands to issue</param>
		/// <returns>The detected command limit, 0 if infinite</returns>
		public int DiscoverSslSessionLength(string command = "PWD", int maxTries = 2000) {
			if (!IsEncrypted) {
				return 0;
			}

			int connects = Status.ConnectCount;

			int oldLength = Config.SslSessionLength;

			Config.SslSessionLength = 0;

			for (int i = 0; i < maxTries; i++) {
				//Console.WriteLine("Try " + i);
				try {
					Execute(command);
				}
				catch {
					Log(FtpTraceLevel.Verbose, "Exception: ");
					break;
				}

				if (Status.ConnectCount > connects) {
					Log(FtpTraceLevel.Verbose, "Reconnect detected");
					break;
				}
			}

			Execute(command);

			if (Status.ConnectCount > connects) {
				Log(FtpTraceLevel.Verbose, "Failure ocurred at: " + m_stream.SslSessionLength);
				return m_stream.SslSessionLength;
			}

			Config.SslSessionLength = oldLength;

			return 0;
		}
	}
}