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

			bool reconnect = false;
			string reconnectReason = string.Empty;

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

			if (Config.NoopAddNoopCmd && IsAuthenticated && !((IInternalFtpClient)this).IsStillConnectedInternal()) {
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
				var staleData = ReadStaleData("prior to command execution of \"" + command.Split()[0] + "\"");

				if (staleData != null) {
					reconnect = true;
					reconnectReason = "stale data present on";
				}
			}

			if (reconnect) {
				string sslLengthInfo = string.Empty;
				if (m_stream is not null && m_stream.IsEncrypted) {
					sslLengthInfo = " (SslSessionLength: " + m_stream.SslSessionLength + ")";
				}
				LogWithPrefix(FtpTraceLevel.Warn, "Reconnect needed due to " + reconnectReason + " control connection" + sslLengthInfo);
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

			// send command to FTP server and get the reply
			m_sema.Wait();
			try {
				m_stream.WriteLine(m_textEncoding, command);
				LastCommandExecuted = command;
				LastCommandTimestamp = DateTime.UtcNow;

				// get the reply
				reply = ((IInternalFtpClient)this).GetReplyInternal(command, false);
				if (reply.Success) {
					OnPostExecute(command);

					if (Config.SslSessionLength > 0) {
						ConnectModule.CheckCriticalSequence(this, command);
					}
				}
			}
			finally {
				m_sema.Release();
			}

			return reply;
		}

		/// <summary>
		/// Things to do after executing a command
		/// </summary>
		/// <param name="command"></param>
		protected void OnPostExecute(string command) {

			// Update stored values

			// CWD LastWorkingDir
			if (command.TrimEnd() == "CWD" || command.StartsWith("CWD ", StringComparison.Ordinal)) {
				// At least for a successful absolute Unix CWD, we know where we are.
				string parms = command.Length <= 4 ? string.Empty : command.Substring(4);
				if (parms.IsAbsolutePath()) {
					Status.LastWorkingDir = parms;
					return;
				}

				// Sadly, there are cases where a successful CWD does not let us easily
				// calculate the resulting working directory! So, we must ask the server
				// where we now are. So, such a CWD results in a PWD command following it.
				// Otherwise we would need to identify all cases (and special servers) where
				// we would need to do special handling.
				Status.LastWorkingDir = null;
				ReadCurrentWorkingDirectory();
			}

			// TYPE CurrentDataType
			else if (command.StartsWith("TYPE I", StringComparison.Ordinal)) {
				Status.CurrentDataType = FtpDataType.Binary;
			}
			else if (command.StartsWith("TYPE A", StringComparison.Ordinal)) {
				Status.CurrentDataType = FtpDataType.ASCII;
			}

		}
	}
}