using System;
using FluentFTP.Client.Modules;
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
					ReadStaleData(true, true, "prior to command execution");
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
				string commandClean = OnPostExecute(command);

				Log(FtpTraceLevel.Info, "Command:  " + commandClean);

				// send command to FTP server
				m_stream.WriteLine(m_textEncoding, command);
				m_stream.Flush();
				LastCommandTimestamp = DateTime.UtcNow;
				reply = GetReplyInternal(false, command); 
			}

			return reply;
		}

		protected string OnPostExecute(string command) {

			// hide sensitive data from logs
			command = LogMaskModule.MaskCommand(this, command);

			// A CWD will invalidate the cached value.
			if (command.StartsWith("CWD ", StringComparison.Ordinal)) {
				Status.LastWorkingDir = null;
			}

			return command;
		}

	}
}