using System;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Executes a command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <returns>The servers reply to the command</returns>
		FtpReply IInternalFtpClient.ExecuteInternal(string command) {
			FtpReply reply;

			bool reconnect = false;
			string reconnectReason = string.Empty;

			lock (m_lock) {

				// Automatic reconnect because we lost the control channel?
				if (!IsConnected) {
					if (command == "QUIT") {
						LogWithPrefix(FtpTraceLevel.Info, "Not sending QUIT because the connection has already been closed.");
						return new FtpReply() {
							Code = "200",
							Message = "Connection already closed."
						};
					}

					reconnect = true;
					reconnectReason = "disconnected";
				}
				// Automatic reconnect on reaching SslSessionLength?
				else if (m_stream.IsEncrypted && Config.SslSessionLength > 0 && !Status.InCriticalSequence && m_stream.SslSessionLength > Config.SslSessionLength) {
					reconnect = true;
					reconnectReason = "max SslSessionLength reached on";
				}
				// Check for stale data on the socket?
				else if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
					var staleData = ReadStaleData(true, "prior to command execution");

					if (staleData != null) {
						reconnect = true;
						reconnectReason = "stale data present on";
					}
				}

				if (reconnect) {
					LogWithPrefix(FtpTraceLevel.Warn, "Reconnect needed due to " + reconnectReason + " control connection (SslSessionLength: " + m_stream.SslSessionLength + ")");
					LogWithPrefix(FtpTraceLevel.Info, "Command stashed: " + command);

					if (IsConnected) {
						if (Status.LastWorkingDir == null) {
							Status.InCriticalSequence = true;
							((IInternalFtpClient)this).GetWorkingDirectoryInternal();
						}

						m_stream.Close();
						m_stream = null;
					}

					((IInternalFtpClient)this).ConnectInternal(true);

					Log(FtpTraceLevel.Info, "");
					LogWithPrefix(FtpTraceLevel.Info, "Executing stashed command");
					Log(FtpTraceLevel.Info, "");
				}

				// hide sensitive data from logs
				string cleanedCommand = LogMaskModule.MaskCommand(this, command);

				Log(FtpTraceLevel.Info, "Command:  " + cleanedCommand);

				// send command to FTP server
				m_stream.WriteLine(m_textEncoding, command);
				LastCommandTimestamp = DateTime.UtcNow;
				reply = GetReplyInternal(command);
				if (reply.Success) {
					OnPostExecute(command);

					if (Config.SslSessionLength > 0) {
						ConnectModule.CheckCriticalSequence(this, command);
					}

				}
			}

			return reply;
		}

		/// <summary>
		/// Things to do after executing a command
		/// </summary>
		/// <param name="command"></param>
		protected void OnPostExecute(string command) {

			// Update stored values
			if (command.TrimEnd() == "CWD" || command.StartsWith("CWD ", StringComparison.Ordinal)) {
				Status.LastWorkingDir = null;
				ReadCurrentWorkingDirectory();
			}
			else if (command.StartsWith("TYPE I", StringComparison.Ordinal)) {
				Status.CurrentDataType = FtpDataType.Binary;
			}
			else if (command.StartsWith("TYPE A", StringComparison.Ordinal)) {
				Status.CurrentDataType = FtpDataType.ASCII;
			}
		}
	}
}