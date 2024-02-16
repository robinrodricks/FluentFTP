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
using FluentFTP.Proxy.AsyncProxy;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Execute a custom FTP command and read the data channel to return its multiline output.
		/// </summary>
		/// <param name="command">The command to issue which produces output</param>
		/// <returns>A list of string objects corresponding to the multi-line response by the server</returns>
		public async Task<List<string>> ExecuteDownloadText(string command, CancellationToken token = default(CancellationToken)) {

			return await ExecuteDownloadTextInternal(command, true, token);

		}

		/// <summary>
		/// Execute a custom FTP command and return its multiline output.
		/// </summary>
		/// <param name="command">The command to issue which produces output</param>
		/// <param name="retry">Retry the command execution on temporary failure?</param>
		/// <returns>A list of string objects corresponding to the multi-line response by the server</returns>
		protected async Task<List<string>> ExecuteDownloadTextInternal(string command, bool retry, CancellationToken token) {

			List<string> rawlisting = new List<string> { "Lines captured:" };

			try {
				// read in raw command output from data stream
				try {
					await using (FtpDataStream stream = await OpenDataStreamAsync(command, 0, token)) {
						try {
							if (this is AsyncFtpClientSocks4Proxy || this is AsyncFtpClientSocks4aProxy) {
								// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
								// we need to skip them otherwise they will be downloaded to the file
								// moreover, these bytes cause "Failed to get the EPSV port" error
								await stream.ReadAsync(new byte[6], 0, 6);
							}

							Log(FtpTraceLevel.Verbose, "+---------------------------------------+");

							if (Config.BulkListing) {
								// increases performance of GetListing by reading multiple lines of the command output at once
								foreach (var line in await stream.ReadAllLinesAsync(Encoding, Config.BulkListingLength, token)) {
									if (!Strings.IsNullOrWhiteSpace(line)) {
										rawlisting.Add(line);
										Log(FtpTraceLevel.Verbose, "Lines  :  " + line);
									}
								}
							}
							else {
								// Read command output line-by-line (actually byte-by-byte)
								string buf;
								while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
									if (buf.Length > 0) {
										rawlisting.Add(buf);
										Log(FtpTraceLevel.Verbose, "Lines  :  " + buf);
									}
								}
							}

							Log(FtpTraceLevel.Verbose, "-----------------------------------------");
						}
						finally {
							await stream.CloseAsync(token);
						}
					}
				}
				catch (AuthenticationException) {
					FtpReply reply = await ((IInternalFtpClient)this).GetReplyInternal(token, command, false, -1); // no exhaustNoop, but non-blocking
					if (!reply.Success) {
						throw new FtpCommandException(reply);
					}
					throw;
				}
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when producing an empty response
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
						Log(FtpTraceLevel.Verbose, "Warning:  Retry ExecuteMultiline once more due to control connection disconnect");
						return await ExecuteDownloadTextInternal(command, false, token);
					}
					else {
						throw;
					}
				}

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.ContainsAnyCI(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					Log(FtpTraceLevel.Verbose, "Warning:  Retry ExecuteMultiline once more due to unexpected EOF");
					return await ExecuteDownloadTextInternal(command, false, token);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}
	}
}