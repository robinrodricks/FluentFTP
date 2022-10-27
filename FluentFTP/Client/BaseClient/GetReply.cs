using System;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using FluentFTP.Helpers;
using System.Text.RegularExpressions;
using FluentFTP.Client.Modules;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal() {
			return GetReplyInternal(null, false, 0);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal(string command) {
			return GetReplyInternal(command, false, 0);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal(string command, bool exhaustNoop) {
			return GetReplyInternal(command, exhaustNoop, exhaustNoop ? 10000 : 0);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
 		/// <param name="timeOut">-1 non-blocking, no timeout, >0 exhaustNoop mode, timeOut in seconds</param>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal(string command, bool exhaustNoop, int timeOut) {
			var reply = new FtpReply();

			lock (m_lock) {

				if (!IsConnected) {
					throw new InvalidOperationException("No connection to the server has been established.");
				}

				if (string.IsNullOrEmpty(command)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for a response");
				}
				else {
					LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for response to: " + OnPostExecute(command));
				}

				// Implement this: https://lists.apache.org/thread/xzpclw1015qncvczt8hg3nom2p5vtcf5
				// Can not use the normal timeout mechanism though, as a System.TimeoutException
				// causes the stream to disconnect.

				string sequence = string.Empty;

				string response;

				var sw = new Stopwatch();

				long elapsedTime;
				long previousElapsedTime = 0;

				sw.Start();

				if (exhaustNoop) {
					m_stream.WriteLine(Encoding, "NOOP");
				}

				do {
					elapsedTime = sw.ElapsedMilliseconds;

					// Maximum wait time for collecting NOOP responses: parameter timeOut
					if (exhaustNoop && elapsedTime > timeOut) {
						break;
					}

					if (!exhaustNoop) {

						// If we are not exhausting NOOPs, i.e. doing a normal GetReply(...)
						// we do a blocking ReadLine(...). This can throw a
						// System.TimeoutException which will disconnect us.
						// Unless timeOut is -1, then we do a single non-blocking read,
						// otherwise we totally disregard timeOut
						if (timeOut >= 0) {
							m_stream.ReadTimeout = Config.ReadTimeout;
							response = m_stream.ReadLine(Encoding);
						}
						else {
							response = string.Empty;
							if (m_stream.SocketDataAvailable > 0) {
								response = m_stream.ReadLine(Encoding);
							}
						}

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
								LogWithPrefix(FtpTraceLevel.Verbose, "Waiting - " + ((10000 - elapsedTime) / 1000).ToString() + " seconds left");
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
						// NOOP responses can actually come in quite a few flavors, so watch out..
						(response.StartsWith("200 NOOP", StringComparison.InvariantCultureIgnoreCase) || response.StartsWith("500"))) {

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

				if (exhaustNoop) {
					LogWithPrefix(FtpTraceLevel.Verbose, "GetReply(...) sequence: " + sequence.TrimStart(','));
				}

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

			reply.Command = string.IsNullOrEmpty(command) ? string.Empty : LogMaskModule.MaskCommand(this, command);

			if (LastReplies == null) {
				LastReplies = new List<FtpReply>();
				LastReplies.Add(reply);
			}
			else {
				LastReplies.Insert(0, reply);
				if (LastReplies.Count > 5) {
					LastReplies.RemoveAt(5);
				}
			}

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