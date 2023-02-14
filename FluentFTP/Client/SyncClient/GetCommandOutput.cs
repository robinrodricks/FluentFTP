using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;
using System.Security.Authentication;
using FluentFTP.Proxy.SyncProxy;

namespace FluentFTP {
	public partial class FtpClient {


		/// <summary>
		/// Gets a command output from the server. Each <see cref="FtpListItem"/> object returned
		/// contains information about the line that was able to be retrieved. 
		/// </summary>
		/// </remarks>
		/// <param name="command">The command to issue which produces output</param>
		/// <returns>An list of string objects</returns>
		public List<string> GetCommandOutput(string command) {
			return GetCommandOutputInternal(command, true);
		}

		/// <summary>
		/// Get the records of a command output and retry if temporary failure.
		/// </summary>
		protected List<string> GetCommandOutputInternal(string command, bool retry) {

			List<string> rawlisting = new List<string> { "Lines captured:" };

			try {
				// read in raw file listing from data stream
				try {
					using (var stream = OpenDataStream(command, 0)) {
						try {
							if (this is FtpClientSocks4Proxy || this is FtpClientSocks4aProxy) {
								// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
								// we need to skip them otherwise they will be downloaded to the file
								// moreover, these bytes cause "Failed to get the EPSV port" error
								stream.Read(new byte[6], 0, 6);
							}

							Log(FtpTraceLevel.Verbose, "+---------------------------------------+");

							if (Config.BulkListing) {
								// increases performance of GetListing by reading multiple lines of the file listing at once
								foreach (var line in stream.ReadAllLines(Encoding, Config.BulkListingLength)) {
									if (!Strings.IsNullOrWhiteSpace(line)) {
										rawlisting.Add(line);
										Log(FtpTraceLevel.Verbose, "Lines  :  " + line);
									}
								}
							}
							else {
								// GetListing will read file listings line-by-line (actually byte-by-byte)
								string buf;
								while ((buf = stream.ReadLine(Encoding)) != null) {
									if (buf.Length > 0) {
										rawlisting.Add(buf);
										Log(FtpTraceLevel.Verbose, "Lines  :  " + buf);
									}
								}
							}

							Log(FtpTraceLevel.Verbose, "-----------------------------------------");
						}
						finally {
							// We want to close/dispose it NOW, and not when the GM
							// gets around to it (after the "using" expires).
							stream.Close();
						}
					}
				}
				catch (AuthenticationException) {
					FtpReply reply = GetReplyInternal("*GETCOMMANDOUTPUT*", false, -1); // no exhaustNoop, but non-blocking
					if (!reply.Success) {
						throw new FtpCommandException(reply);
					}
					throw;
				}
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when listing an empty directory
				// and the connection fails because no communication socket is provided by the server
			}
			catch (FtpCommandException ftpEx) {
				// Fix for #589 - CompletionCode is null
				if (ftpEx.CompletionCode == null) {
					throw new FtpException(ftpEx.Message, ftpEx);
				}
				// Some FTP servers throw 550 for empty folders. Absorb these.
				if (!ftpEx.CompletionCode.StartsWith("550")) {
					throw ftpEx;
				}
			}
			catch (IOException ioEx) {
				// Some FTP servers forcibly close the connection, we absorb these errors,
				// unless we have lost the control connection itself
				if (m_stream.IsConnected == false) {
					if (retry) {
						// retry once more, but do not go into a infinite recursion loop here
						// note: this will cause an automatic reconnect in Execute(...)
						Log(FtpTraceLevel.Verbose, "Warning:  Retry GetCommandOutput once more due to control connection disconnect");
						return GetCommandOutputInternal(command, false);
					}
					else {
						throw;
					}
				}

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.ContainsAnyCI(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					Log(FtpTraceLevel.Verbose, "Warning:  Retry GetCommandOutput once more due to unexpected EOF");
					return GetCommandOutputInternal(command, false);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}

	}
}
