using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <returns>FtpReply representing the response from the server</returns>
		public Task<FtpReply> GetReply(CancellationToken token = default(CancellationToken)) {
			return GetReplyAsyncInternal(token, null, false, 0);
		}

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		public Task<FtpReply> GetReplyAsyncInternal(CancellationToken token, string command) {
			return GetReplyAsyncInternal(token, command, false, 0);
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
		public Task<FtpReply> GetReplyAsyncInternal(CancellationToken token, string command, bool exhaustNoop) {
			return GetReplyAsyncInternal(token, command, exhaustNoop, exhaustNoop ? 10000 : 0);
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
		protected async Task<FtpReply> GetReplyAsyncInternal(CancellationToken token, string command, bool exhaustNoop, int timeOut) {

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

			if (exhaustNoop) {
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
					((response.StartsWith("200") && (response.IndexOf("NOOP", StringComparison.InvariantCultureIgnoreCase) >= 0)) ||
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

			reply = ProcessGetReply(reply, command);

			return reply;
		}

	}
}