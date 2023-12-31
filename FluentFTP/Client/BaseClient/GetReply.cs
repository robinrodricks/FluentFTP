using System;
using System.Linq;
using FluentFTP.Helpers;
using System.Text.RegularExpressions;
using FluentFTP.Client.Modules;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		FtpReply IInternalFtpClient.GetReplyInternal() {
			return ((IInternalFtpClient)this).GetReplyInternal(null, false, 0, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		FtpReply IInternalFtpClient.GetReplyInternal(string command) {
			return ((IInternalFtpClient)this).GetReplyInternal(command, false, 0, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <returns>FtpReply representing the response from the server</returns>
		FtpReply IInternalFtpClient.GetReplyInternal(string command, bool exhaustNoop) {
			return ((IInternalFtpClient)this).GetReplyInternal(command, exhaustNoop, exhaustNoop ? 10000 : 0, true);
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
		FtpReply IInternalFtpClient.GetReplyInternal(string command, bool exhaustNoop, int timeOut) {
			return ((IInternalFtpClient)this).GetReplyInternal(command, exhaustNoop, timeOut, true);
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
		FtpReply IInternalFtpClient.GetReplyInternal(string command, bool exhaustNoop, int timeOut, bool useSema) {
			var reply = new FtpReply();

			if (string.IsNullOrEmpty(command)) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for a response");
			}
			else {
				LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for response to: " + LogMaskModule.MaskCommand(this, command));
			}

			Status.IgnoreStaleData = false;

			string sequence = string.Empty;

			string response;

			var sw = new Stopwatch();

			long elapsedTime;
			long previousElapsedTime = 0;

			if (useSema) {
				m_sema.Wait();
			}

			try {

				if (exhaustNoop) {
					Status.DaemonEnable = false;

					// tickle the server
					m_stream.WriteLine(Encoding, "NOOP");
				}

				sw.Start();

				do {
					if (!IsConnected) {
						throw new InvalidOperationException("No connection to the server exists.");
					}

					elapsedTime = sw.ElapsedMilliseconds;

					response = null;

					if (exhaustNoop) {

						// If we are exhausting NOOPs, use a non-blocking ReadLine(...)
						// as we don't want a timeout exception, which would disconnect us.

						if (elapsedTime > timeOut) {
							break;
						}

						if (m_stream.SocketDataAvailable > 0) {
							response = m_stream.ReadLine(Encoding);
						}
						else {
							if (elapsedTime > (previousElapsedTime + 1000)) {
								previousElapsedTime = elapsedTime;
								LogWithPrefix(FtpTraceLevel.Verbose, "Waiting - " + ((timeOut - elapsedTime) / 1000).ToString() + " seconds left");
								// if we have more then 5 seconds left, tickle the server some more
								if (timeOut - elapsedTime >= 5000) {
									LogWithPrefix(FtpTraceLevel.Verbose, "Sending NOOP (<-GetReply)");
									m_stream.WriteLine(Encoding, "NOOP");
								}
							}
						}

					}
					else {

						// If we are not exhausting NOOPs, i.e. doing a normal GetReply(...)

						if (elapsedTime > Config.ReadTimeout) {
							throw new System.TimeoutException();
						}

						// we normally need blocking reads apart from some special cases indicated
						// by parameter timeOut having been set to -1

						if (timeOut >= 0) {
							// BLOCKING read
							m_stream.ReadTimeout = Config.ReadTimeout;
							response = m_stream.ReadLine(Encoding);
						}
						else {
							// NON BLOCKING read
							if (m_stream.SocketDataAvailable > 0) {
								response = m_stream.ReadLine(Encoding);
							}
						}

					}

					if (string.IsNullOrEmpty(response)) {
						Thread.Sleep(100);
						continue;
					}

					sequence += "," + response.Split(' ')[0];

					if (exhaustNoop &&
						((response.StartsWith("200") && (response.IndexOf("NOOP", StringComparison.OrdinalIgnoreCase) >= 0)) ||
						response.StartsWith("500"))) {

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

					// Accumulate all responses
					reply.InfoMessages += response + "\n";

				} while (true);

				sw.Stop();

				if (exhaustNoop) {
					LogWithPrefix(FtpTraceLevel.Verbose, "GetReply(...) sequence: " + sequence.TrimStart(','));
				}

				Status.DaemonEnable = true;

			}
			finally {
				if (useSema) {
					m_sema.Release();
				}
			}

			reply = ProcessGetReply(reply, command);

			return reply;
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <returns>FtpReply representing the response from the server</returns>
		async Task<FtpReply> IInternalFtpClient.GetReplyInternal(CancellationToken token) {
			return await ((IInternalFtpClient)this).GetReplyInternal(token, null, false, 0, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		async Task<FtpReply> IInternalFtpClient.GetReplyInternal(CancellationToken token, string command) {
			return await ((IInternalFtpClient)this).GetReplyInternal(token, command, false, 0, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <returns>FtpReply representing the response from the server</returns>
		async Task<FtpReply> IInternalFtpClient.GetReplyInternal(CancellationToken token, string command, bool exhaustNoop) {
			return await ((IInternalFtpClient)this).GetReplyInternal(token, command, exhaustNoop, exhaustNoop ? 10000 : 0, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <param name="timeOut">-1 non-blocking, no timeout, >0 exhaustNoop mode, timeOut in seconds</param>
		/// <returns>FtpReply representing the response from the server</returns>
		async Task<FtpReply> IInternalFtpClient.GetReplyInternal(CancellationToken token, string command, bool exhaustNoop, int timeOut) {
			return await ((IInternalFtpClient)this).GetReplyInternal(token, command, exhaustNoop, timeOut, true);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <param name="timeOut">-1 non-blocking, no timeout, >0 exhaustNoop mode, timeOut in seconds</param>
		/// <returns>FtpReply representing the response from the server</returns>
		async Task<FtpReply> IInternalFtpClient.GetReplyInternal(CancellationToken token, string command, bool exhaustNoop, int timeOut, bool useSema) {
			var reply = new FtpReply();

			if (string.IsNullOrEmpty(command)) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for a response");
			}
			else {
				LogWithPrefix(FtpTraceLevel.Verbose, "Waiting for response to: " + LogMaskModule.MaskCommand(this, command));
			}

			Status.IgnoreStaleData = false;

			string sequence = string.Empty;

			string response;

			var sw = new Stopwatch();

			long elapsedTime;
			long previousElapsedTime = 0;

			if (useSema) {
				await m_sema.WaitAsync();
			}

			try {

				if (exhaustNoop) {
					Status.DaemonEnable = false;

					// tickle the server
					m_stream.WriteLine(Encoding, "NOOP");
				}

				sw.Start();

				do {
					if (!IsConnected) {
						throw new InvalidOperationException("No connection to the server exists.");
					}

					elapsedTime = sw.ElapsedMilliseconds;

					response = null;

					if (exhaustNoop) {

						// If we are exhausting NOOPs, use a non-blocking ReadLine(...)
						// as we don't want a timeout exception, which would disconnect us.

						if (elapsedTime > timeOut) {
							break;
						}

						if (m_stream.SocketDataAvailable > 0) {
							response = await m_stream.ReadLineAsync(Encoding, token);
						}
						else {
							if (elapsedTime > (previousElapsedTime + 1000)) {
								previousElapsedTime = elapsedTime;
								LogWithPrefix(FtpTraceLevel.Verbose, "Waiting - " + ((timeOut - elapsedTime) / 1000).ToString() + " seconds left");
								// if we have more then 5 seconds left, tickle the server some more
								if (timeOut - elapsedTime >= 5000) {
									LogWithPrefix(FtpTraceLevel.Verbose, "Sending NOOP (<-GetReply)");
									await m_stream.WriteLineAsync(Encoding, "NOOP", token);
								}
							}
						}

					}
					else {

						// If we are not exhausting NOOPs, i.e. doing a normal GetReply(...)

						if (elapsedTime > Config.ReadTimeout) {
							throw new System.TimeoutException();
						}

						// we normally need blocking reads apart from some special cases indicated
						// by parameter timeOut having been set to -1

						if (timeOut >= 0) {
							// BLOCKING read
							m_stream.ReadTimeout = Config.ReadTimeout;
							response = await m_stream.ReadLineAsync(Encoding, token);
						}
						else {
							// NON BLOCKING read
							if (m_stream.SocketDataAvailable > 0) {
								response = await m_stream.ReadLineAsync(Encoding, token);
							}
						}

					}

					if (string.IsNullOrEmpty(response)) {
						Thread.Sleep(100);
						continue;
					}

					sequence += "," + response.Split(' ')[0];

					if (exhaustNoop &&
						((response.StartsWith("200") && (response.IndexOf("NOOP", StringComparison.OrdinalIgnoreCase) >= 0)) ||
						response.StartsWith("500"))) {

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

					// Accumulate all responses
					reply.InfoMessages += response + "\n";

				} while (true);

				sw.Stop();

				if (exhaustNoop) {
					LogWithPrefix(FtpTraceLevel.Verbose, "GetReply(...) sequence: " + sequence.TrimStart(','));
				}

				Status.DaemonEnable = true;

			}
			finally {
				if (useSema) {
					m_sema.Release();
				}
			}

			reply = ProcessGetReply(reply, command);

			return reply;
		}

		/// <summary>
		/// Process the returned data after command was executed
		/// </summary>
		/// <param name="reply"></param>
		/// <param name="command"></param>
		/// <returns></returns>
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

				if (Config.LogDurations) {
					// log response code + message + duration
					TimeSpan duration = DateTime.UtcNow.Subtract(LastCommandTimestamp);
					Log(FtpTraceLevel.Info, "Response: " + reply.Code + " " + maskedReply + " [" + duration.ToShortString() + "]");
				}
				else {
					// log response code + message
					Log(FtpTraceLevel.Info, "Response: " + reply.Code + " " + maskedReply);
				}
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
		protected static bool DecodeStringToReply(string text, ref FtpReply reply) {
			Match m = Regex.Match(text, "^(?<code>[0-9]{3}) (?<message>.*)$");
			if (m.Success) {
				reply.Code = m.Groups["code"].Value;
				reply.Message = m.Groups["message"].Value;
			}
			return m.Success;
		}

	}
}