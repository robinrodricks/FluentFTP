using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
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
		public async Task<FtpReply> Execute(string command, CancellationToken token) {
			FtpReply reply;


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
				await Connect(true, token);
            }
            // Automatic reconnect on reaching MaxSslReadLines?
            else if (Config.MaxSslReadLines > 0 && !Status.InCriticalSequence && m_stream.SocketReadLineCount > Config.MaxSslReadLines) {
				LogWithPrefix(FtpTraceLevel.Info, "Reconnect due to MaxSslReadLines reached");

				m_stream.Close();
                m_stream = null;

                await Connect(true, token);
			}
            // Check for stale data on the socket?
            else if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
#if NETSTANDARD
				var staleData = await ReadStaleDataAsync(true, "prior to command execution", token);
#else
				var staleData = ReadStaleData(true, "prior to command execution");
#endif

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
			await m_stream.WriteLineAsync(m_textEncoding, command, token);
			LastCommandTimestamp = DateTime.UtcNow;
			reply = await GetReplyAsyncInternal(token, command);
			if (reply.Success) {
				OnPostExecute(command);

                if (Config.MaxSslReadLines > 0) {
                    DetermineCriticalSequence(command);
                }
			}

			return reply;
		}

	}
}