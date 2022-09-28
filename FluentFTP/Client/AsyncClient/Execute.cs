using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

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

			if (Config.StaleDataCheck && Status.AllowCheckStaleData) {
#if NETSTANDARD
				await ReadStaleDataAsync(true, true, "prior to command execution", token);
#else
				ReadStaleData(true, true, "prior to command execution");
#endif
			}

			if (!IsConnected) {
				if (command == "QUIT") {
					LogWithPrefix(FtpTraceLevel.Info, "Not sending QUIT because the connection has already been closed.");
					return new FtpReply() {
						Code = "200",
						Message = "Connection already closed."
					};
				}

				await Connect(token);
			}

			// hide sensitive data from logs
			string commandTxt = OnPostExecute(command);

			Log(FtpTraceLevel.Info, "Command:  " + commandTxt);

			// send command to FTP server
			await m_stream.WriteLineAsync(m_textEncoding, command, token);
			LastCommandTimestamp = DateTime.UtcNow;
			reply = await GetReplyAsyncInternal(token, command);

			return reply;
		}

	}
}