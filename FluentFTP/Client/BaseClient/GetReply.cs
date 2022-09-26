using System;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using FluentFTP.Helpers;
using System.Text.RegularExpressions;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Diagnostics;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal(bool exhaustNoop = false, string command = null) {

			var reply = new FtpReply();

			lock (m_lock) {

				if (!IsConnected) {
					throw new InvalidOperationException("No connection to the server has been established.");
				}

				if (string.IsNullOrEmpty(command)) {
					Log(FtpTraceLevel.Verbose, "Status:   Waiting for a response");
				}
				else {
					Log(FtpTraceLevel.Verbose, "Status:   Waiting for response to: " + OnPostExecute(command));
				}

				// Implement this: https://lists.apache.org/thread/xzpclw1015qncvczt8hg3nom2p5vtcf5
				// Can not use the normal timeout mechanism though, as a System.TimeoutException
				// causes the stream to disconnect.

				string sequence = string.Empty;

				string response;

				var sw = new Stopwatch();

				long elapsedTime = 0;
				long previousElapsedTime = 0;

				sw.Start();

				do {
					elapsedTime = sw.ElapsedMilliseconds;

					// Maximum wait time for collecting NOOP responses: 10 seconds
					if (exhaustNoop && elapsedTime > 10000) {
						break;
					}

					if (!exhaustNoop) {

						// If we are not exhausting NOOPs, i.e. doing a normal GetReply(...)
						// we do a blocking ReadLine(...). This can throw a
						// System.TimeoutException which will disconnect us.

						m_stream.ReadTimeout = Config.ReadTimeout;
						response = m_stream.ReadLine(Encoding);

					}
					else {

						// If we are exhausting NOOPs, use a non-blocking ReadLine(...)
						// as we don't want a timeout exception, which would disconnect us.

						if (m_stream.SocketDataAvailable > 0) {
							response = m_stream.ReadLine(Encoding);
						}
						else {
							if (elapsedTime > (previousElapsedTime + 1000)) {
								previousElapsedTime = elapsedTime;
								Log(FtpTraceLevel.Verbose, "Status:   Waiting - " + ((10000 - elapsedTime) / 1000).ToString() + " seconds left");
							}
							response = null;
							Thread.Sleep(100);
						}

					}

					if (string.IsNullOrEmpty(response)) {
						continue;
					}

					sequence += "," + response.Split(' ')[0];

					if (exhaustNoop &&
						// NOOP responses can actually come in quite a few flavors
						(response.StartsWith("200 NOOP") || response.StartsWith("500"))) {


						Log(FtpTraceLevel.Verbose, "Skipped:  " + response);

						continue;
					}

					if (DecodeStringToReply(response, ref reply)) {

						if (exhaustNoop) {
							// We need to perhaps exhaust more NOOP responses
							continue;
						}
						else {
							// On a normal GetReply(...) we are happy to collect the
							// first valid response
							break;
						}

					}

					// Accumulate non-valid response text too, prior to a valid response
					reply.InfoMessages += response + "\n";

				} while (true);

				sw.Stop();

				Log(FtpTraceLevel.Verbose, "Status:   GetReply(...) sequence: " + sequence.TrimStart(','));

				reply = ProcessGetReply(reply, command);

			} // lock

			return reply;
		}

		protected FtpReply ProcessGetReply(FtpReply reply, string command) {

			// log multiline response messages
			if (reply.InfoMessages != null) {
				reply.InfoMessages = reply.InfoMessages.Trim();
			}

			if (!string.IsNullOrEmpty(reply.InfoMessages)) {
				//this.LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
				Log(FtpTraceLevel.Verbose, reply.InfoMessages.Split('\n').AddPrefix("Response: ", true).Join("\n"));

				//this.LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
			}

			// if reply received
			if (reply.Code != null) {

				// hide sensitive data from logs
				var maskedReply = reply.Message;
				if (command != null) {
					maskedReply = LogMaskModule.MaskReply(this, reply, reply.Message, command);
				}

				// log response code + message
				Log(FtpTraceLevel.Info, "Response: " + reply.Code + " " + maskedReply);
			}

			LastReply = reply;

			return reply;
		}

		/// <summary>
		/// Decodes the given FTP response string into a FtpReply, separating the FTP return code and message.
		/// Returns true if the string was decoded correctly or false if it is not a standard format FTP response.
		/// </summary>
		protected bool DecodeStringToReply(string text, ref FtpReply reply) {
			Match m = Regex.Match(text, "^(?<code>[0-9]{3}) (?<message>.*)$");
			if (m.Success) {
				reply.Code = m.Groups["code"].Value;
				reply.Message = m.Groups["message"].Value;
			}
			return m.Success;
		}

	}
}