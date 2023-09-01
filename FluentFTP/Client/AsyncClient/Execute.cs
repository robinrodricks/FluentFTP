using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Performs an asynchronous execution of the specified command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The servers reply to the command</returns>
		public async Task<FtpReply> Execute(string command, CancellationToken token = default(CancellationToken)) {
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

			// Automatic reconnect on reaching SslSessionLength?
			else if (m_stream.IsEncrypted && Config.SslSessionLength > 0 && !Status.InCriticalSequence && m_stream.SslSessionLength > Config.SslSessionLength) {
				reconnect = true;
				reconnectReason = "max SslSessionLength reached on";
			}

			// Check for stale data on the socket?
			else if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
#if NETSTANDARD
				var staleData = await ReadStaleDataAsync(true, "prior to command execution", token);
#else
				var staleData = ReadStaleData(true, "prior to command execution");
#endif

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
						await GetWorkingDirectory();
					}

					m_stream.Close();
					m_stream = null;
				}

				await Connect(true, token);

				Log(FtpTraceLevel.Info, "");
				LogWithPrefix(FtpTraceLevel.Info, "Executing stashed command");
				Log(FtpTraceLevel.Info, "");
			}

			// hide sensitive data from logs
			string cleanedCommand = LogMaskModule.MaskCommand(this, command);

			Log(FtpTraceLevel.Info, "Command:  " + cleanedCommand);

			// send command to FTP server
			await m_stream.WriteLineAsync(m_textEncoding, command, token);
			LastCommandExecuted = command;
			LastCommandTimestamp = DateTime.UtcNow;
			reply = await GetReplyAsyncInternal(token, command);
			if (reply.Success) {
				await OnPostExecute(command, token);

				if (Config.SslSessionLength > 0) {
					ConnectModule.CheckCriticalSequence(this, command);
				}
			}

			return reply;
		}

		/// <summary>
		/// Things to do after executing a command
		/// </summary>
		/// <param name="command"></param>
		protected async Task OnPostExecute(string command, CancellationToken token) {

			// Update stored values
			if (command.TrimEnd() == "CWD" || command.StartsWith("CWD ", StringComparison.Ordinal)) {
				Status.LastWorkingDir = null;
				await ReadCurrentWorkingDirectory(token);
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