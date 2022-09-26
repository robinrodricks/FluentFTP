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

		// TODO: add example
		/// <summary>
		/// Retrieves a reply from the server. Do not execute this method
		/// unless you are sure that a reply has been sent, i.e., you
		/// executed a command. Doing so will cause the code to hang
		/// indefinitely waiting for a server reply that is never coming.
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		public async Task<FtpReply> GetReply(CancellationToken token) {
			return await GetReplyAsyncInternal(token);
		}

		protected async Task<FtpReply> GetReplyAsyncInternal(CancellationToken token, bool exhaustNoop = false, string command = null, string commandClean = null) {
			var reply = new FtpReply();
			string response;

			if (!IsConnected) {
				throw new InvalidOperationException("No connection to the server has been established.");
			}

			if (string.IsNullOrEmpty(commandClean)) {
				Log(FtpTraceLevel.Verbose, "Status:   Waiting for a response");
			}
			else {
				Log(FtpTraceLevel.Verbose, "Status:   Waiting for response to: " + commandClean);
			}

			// Implement this: https://lists.apache.org/thread/xzpclw1015qncvczt8hg3nom2p5vtcf5
			if (exhaustNoop) {
				m_stream.ReadTimeout = 10000;
			}
			else {
				m_stream.ReadTimeout = Config.ReadTimeout;
			}
			try {
				while ((response = await m_stream.ReadLineAsync(Encoding, token)) != null) {
					if (exhaustNoop &&
						(response.StartsWith("200") || response.StartsWith("500"))) {
						Log(FtpTraceLevel.Verbose, "Status:   exhausted: " + response);
						continue;
					}
					if (DecodeStringToReply(response, ref reply)) {
						if (exhaustNoop) {
							continue;
						}
						else {
							break;
						}
					}
					reply.InfoMessages += response + "\n";
				}
			}
			catch (TimeoutException) {
				if (!exhaustNoop) {
					throw;
				}
			}

			reply = ProcessGetReply(reply, command);

			return reply;
		}

	}
}