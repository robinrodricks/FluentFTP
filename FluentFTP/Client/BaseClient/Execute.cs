using System;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Executes a command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <returns>The servers reply to the command</returns>
		FtpReply IInternalFtpClient.ExecuteInternal(string command) {
			FtpReply reply;

			lock (m_lock) {
				if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
					ReadStaleData(true, false, true);
				}

				if (!IsConnected) {
					if (command == "QUIT") {
						LogWithPrefix(FtpTraceLevel.Info, "Not sending QUIT because the connection has already been closed.");
						return new FtpReply() {
							Code = "200",
							Message = "Connection already closed."
						};
					}

					((IInternalFtpClient)this).ConnectInternal();
				}

				// hide sensitive data from logs
				var commandTxt = command;
				if (command.StartsWith("USER", StringComparison.Ordinal)) {
					commandTxt = "USER ***";
				}

				if (command.StartsWith("PASS", StringComparison.Ordinal)) {
					commandTxt = "PASS ***";
				}

				// A CWD will invalidate the cached value.
				if (command.StartsWith("CWD ", StringComparison.Ordinal)) {
					Status.LastWorkingDir = null;
				}

				Log(FtpTraceLevel.Info, "Command:  " + commandTxt);

				// send command to FTP server
				m_stream.WriteLine(m_textEncoding, command);
				LastCommandTimestamp = DateTime.UtcNow;
				reply = GetReplyInternal();
			}

			return reply;
		}

	}
}