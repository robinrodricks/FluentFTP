using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {



		/// <summary>
		/// Populates the capabilities flags based on capabilities
		/// supported by this server. This method is overridable
		/// so that new features can be supported
		/// </summary>
		/// <param name="reply">The reply object from the FEAT command. The InfoMessages property will
		/// contain a list of the features the server supported delimited by a new line '\n' character.</param>
		protected virtual void GetFeatures(FtpReply reply) {
			ServerFeatureModule.Detect(m_capabilities, ref m_hashAlgorithms, reply.InfoMessages.Split('\n'));
		}

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		string IInternalFtpClient.GetWorkingDirectoryInternal() {

			// this case occurs immediately after connection and after the working dir has changed
			if (Status.LastWorkingDir == null) {
				ReadCurrentWorkingDirectory();
			}

			return Status.LastWorkingDir;
		}


		protected FtpReply ReadCurrentWorkingDirectory() {
			FtpReply reply;

			lock (m_lock) {
				// read the absolute path of the current working dir
				if (!(reply = ((IInternalFtpClient)this).ExecuteInternal("PWD")).Success) {
					throw new FtpCommandException(reply);
				}
			}

			// cache the last working dir
			Status.LastWorkingDir = ParseWorkingDirectory(reply);
			return reply;
		}

		/// <summary>
		/// When last command was sent (NOOP or other), for having <see cref="Noop"/>
		/// respect the <see cref="NoopInterval"/>.
		/// </summary>
		protected DateTime m_lastCommandUtc;

		/// <summary>
		/// Executes a command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <returns>The servers reply to the command</returns>
		FtpReply IInternalFtpClient.ExecuteInternal(string command) {
			FtpReply reply;

			lock (m_lock) {
				if (StaleDataCheck && Status.AllowCheckStaleData) {
					ReadStaleData(true, false, true);
				}

				if (!IsConnected) {
					if (command == "QUIT") {
						LogStatus(FtpTraceLevel.Info, "Not sending QUIT because the connection has already been closed.");
						return new FtpReply() {
							Code = "200",
							Message = "Connection already closed."
						};
					}

					((IInternalFtpClient)this).ConnectInternal();
				}

				// hide sensitive data from logs
				var commandTxt = command;
				if (!FtpTrace.LogUserName && command.StartsWith("USER", StringComparison.Ordinal)) {
					commandTxt = "USER ***";
				}

				if (!FtpTrace.LogPassword && command.StartsWith("PASS", StringComparison.Ordinal)) {
					commandTxt = "PASS ***";
				}

				// A CWD will invalidate the cached value.
				if (command.StartsWith("CWD ", StringComparison.Ordinal)) {
					Status.LastWorkingDir = null;
				}

				LogLine(FtpTraceLevel.Info, "Command:  " + commandTxt);

				// send command to FTP server
				m_stream.WriteLine(m_textEncoding, command);
				m_lastCommandUtc = DateTime.UtcNow;
				reply = GetReplyInternal();
			}

			return reply;
		}

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

				m_stream.ReadTimeout = m_readTimeout;
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
				if (!FtpTrace.LogUserName && reply.Code == "331" && logMsg.StartsWith("User ", StringComparison.Ordinal) && logMsg.Contains(" OK")) {
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

		/// <summary>
		/// Open a local port on the given ActivePort or a random port.
		/// </summary>
		/// <param name="stream"></param>
		protected void StartListeningOnPort(FtpDataStream stream) {
			if (m_ActivePorts.IsBlank()) {
				// Use random port
				stream.Listen(m_stream.LocalEndPoint.Address, 0);
			}
			else {
				var success = false;

				// Use one of the specified ports
				foreach (var port in m_ActivePorts) {
					try {
						stream.Listen(m_stream.LocalEndPoint.Address, port);
						success = true;
						break;
					}
					catch (SocketException se) {
						if (se.SocketErrorCode != SocketError.AddressAlreadyInUse) {
							throw;
						}
					}
				}

				// No usable port found
				if (!success) {
					throw new Exception("No valid active data port available!");
				}
			}
		}

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		public FtpReply CloseDataStream(FtpDataStream stream) {
			LogFunc(nameof(CloseDataStream));

			var reply = new FtpReply();

			if (stream == null) {
				throw new ArgumentException("The data stream parameter was null");
			}

			lock (m_lock) {
				try {
					if (IsConnected) {
						// if the command that required the data connection was
						// not successful then there will be no reply from
						// the server, however if the command was successful
						// the server will send a reply when the data connection
						// is closed.
						if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
							if (!(reply = GetReplyInternal()).Success) {
								throw new FtpCommandException(reply);
							}
						}
					}
				}
				finally {
					// if this is a clone of the original control
					// connection we should Dispose()
					if (IsClone) {
						((IInternalFtpClient)this).DisconnectInternal();
						Dispose();
					}
				}

			}

			return reply;
		}


	}
}