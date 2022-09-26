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
		/// Retrieves a reply from the server. Do not execute this method
		/// unless you are sure that a reply has been sent, i.e., you
		/// executed a command. Doing so will cause the code to hang
		/// indefinitely waiting for a server reply that is never coming.
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal(bool exhaustNoop = false, string command = null, string commandClean = null) {
			var reply = new FtpReply();
			string response = null, responseBuf;
			int timeOutCounter = 0;

			lock (m_lock) {
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
				// Can not use the timeout mechanism though, as a System.TimeoutException
				// causes the stream to disconnect.

				var waitStarted = DateTime.Now;
				var sw = new Stopwatch();

				sw.Start();

				do {
					var swTime = sw.ElapsedMilliseconds;

					if (exhaustNoop && swTime > 10000) {
						break;
					}

					if (exhaustNoop) {
						if (m_stream.SocketDataAvailable > 0) {
							/*
							byte[] responseBuf = new byte[m_stream.SocketDataAvailable];
							m_stream.Read(responseBuf, 0, responseBuf.Length);
							response = Encoding.GetString(responseBuf).TrimEnd('\0', '\r', '\n');
							*/
							response = m_stream.ReadLine(Encoding);
						}
						else {
							response = null;
							Thread.Sleep(100);
						}
					}
					else {
						m_stream.ReadTimeout = Config.ReadTimeout;
						// This can throw System.TimeoutException which will disconnect us.
						response = m_stream.ReadLine(Encoding);
					}

					if (string.IsNullOrEmpty(response)) {
						continue;
					}

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

				} while (true);

				sw.Stop();

				reply = ProcessGetReply(reply, command);
			}

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