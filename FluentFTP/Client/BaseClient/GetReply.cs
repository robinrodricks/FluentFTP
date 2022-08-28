using System;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using FluentFTP.Helpers;
using System.Text.RegularExpressions;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Retrieves a reply from the server. Do not execute this method
		/// unless you are sure that a reply has been sent, i.e., you
		/// executed a command. Doing so will cause the code to hang
		/// indefinitely waiting for a server reply that is never coming.
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		protected FtpReply GetReplyInternal() {
			var reply = new FtpReply();
			string buf;

			lock (m_lock) {
				if (!IsConnected) {
					throw new InvalidOperationException("No connection to the server has been established.");
				}

				m_stream.ReadTimeout = Config.ReadTimeout;
				while ((buf = m_stream.ReadLine(Encoding)) != null) {
					if (DecodeStringToReply(buf, ref reply)) {
						break;
					}
					reply.InfoMessages += buf + "\n";
				}

				reply = ProcessGetReply(reply);
			}

			return reply;
		}

		protected FtpReply ProcessGetReply(FtpReply reply) {
			// log multiline response messages
			if (reply.InfoMessages != null) {
				reply.InfoMessages = reply.InfoMessages.Trim();
			}

			if (!string.IsNullOrEmpty(reply.InfoMessages)) {
				//this.LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
				LogLine(FtpTraceLevel.Verbose, reply.InfoMessages.Split('\n').AddPrefix("Response: ", true).Join("\n"));

				//this.LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
			}

			// if reply received
			if (reply.Code != null) {

				// hide sensitive data from logs
				var logMsg = reply.Message;
				if (reply.Code == "331" && logMsg.StartsWith("User ", StringComparison.Ordinal) && logMsg.Contains(" OK")) {
					logMsg = logMsg.Replace(Credentials.UserName, "***");
				}

				// log response code + message
				LogLine(FtpTraceLevel.Info, "Response: " + reply.Code + " " + logMsg);
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