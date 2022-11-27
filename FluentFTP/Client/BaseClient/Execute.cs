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
				// Automatic reconnect on reaching MaxSslReadLines?
				else if (Config.MaxSslReadLines > 0 && !Status.InCriticalSequence && m_stream.SocketReadLineCount > Config.MaxSslReadLines) {
					LogWithPrefix(FtpTraceLevel.Info, "Reconnect due to MaxSslReadLines reached");

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

					if (Config.MaxSslReadLines > 0) {
						DetermineCriticalSequence(command);
					}

				}
			}

			return reply;
		}

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

		protected void DetermineCriticalSequence(string cmd) {
			// Check against a list of commands that would be
			// the start of a critical sequence and commands
			// that denote the end of a critical sequence.
			// A critical sequence will not be interrupted by an
			// automatic reconnect.

			List<string> criticalStartingCommands = new List<string>()
			{
				"EPRT",
				"EPSV",
				"LPSV",
				"PASV",
				"SPSV",
				"PORT",
				"LPRT",
			};

			List<string> criticalTerminatingCommands = new List<string>()
			{
				"ABOR",
				"LIST",
				"NLST",
				"MLSD",
				"STOR",
				"STOU",
				"APPE",
				"REST",
				"RETR",
				"THMB",
			};

			if (criticalStartingCommands.Contains(cmd.Split(new char[] { ' ' })[0])) {
				Status.InCriticalSequence = true;
				return;
			}

			if (criticalTerminatingCommands.Contains(cmd.Split(new char[] { ' ' })[0])) {
				Status.InCriticalSequence = false;
				return;
			}
		}

	}
}
