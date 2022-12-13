using System;
using System.Collections.Generic;
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

				if (!IsConnected) {
					if (command == "QUIT") {
						LogWithPrefix(FtpTraceLevel.Info, "Not sending QUIT because the connection has already been closed.");
						return new FtpReply() {
							Code = "200",
							Message = "Connection already closed."
						};
					}

					LogWithPrefix(FtpTraceLevel.Info, "Reconnect due to disconnected control connection");

					// Reconnect and then execute the command
					((IInternalFtpClient)this).ConnectInternal(true);
				}
				// Automatic reconnect on reaching SslSessionLength?
				else if (m_stream.IsEncrypted && Config.SslSessionLength > 0 && !Status.InCriticalSequence && m_stream.SocketReadLineCount > Config.SslSessionLength) {
					LogWithPrefix(FtpTraceLevel.Info, "Reconnect due to SslSessionLength reached");

					m_stream.Close();
					m_stream = null;

					((IInternalFtpClient)this).ConnectInternal(true);
				}
				// Check for stale data on the socket?
				else if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
					var staleData = ReadStaleData(true, "prior to command execution");

					if (staleData != null) {
						LogWithPrefix(FtpTraceLevel.Info, "Reconnect due to stale data");

						m_stream.Close();
						m_stream = null;

						((IInternalFtpClient)this).ConnectInternal(true);
					}
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