using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A data stream ready to be used</returns>
		protected async Task<FtpDataStream> OpenPassiveDataStreamAsync(FtpDataConnectionType type, string command, long restart, CancellationToken token = default(CancellationToken)) {
			LogFunction(nameof(OpenPassiveDataStreamAsync), new object[] { type, command, restart });

			FtpDataStream stream = null;
			FtpReply reply;
			string host = null;
			var port = 0;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");
			}


			for (int a = 0; a <= Config.PassiveMaxAttempts;) {

				if (type is FtpDataConnectionType.EPSV or FtpDataConnectionType.AutoPassive && !Status.EPSVNotSupported) {
					// execute EPSV to try enhanced-passive mode
					if (!(reply = await Execute("EPSV", token)).Success) {
						// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
						if (reply.Type is FtpResponseType.TransientNegativeCompletion or FtpResponseType.PermanentNegativeCompletion
							&& type == FtpDataConnectionType.AutoPassive
							&& m_stream != null
							&& m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {
							// mark EPSV not supported so we do not try EPSV again during this connection
							Status.EPSVNotSupported = true;
							return await OpenPassiveDataStreamAsync(FtpDataConnectionType.PASV, command, restart, token);
						}

						// throw this unknown error
						throw new FtpCommandException(reply);
					}

					// read the connection port from the EPSV response
					GetEnhancedPassivePort(reply, out host, out port);

				}
				else {
					if (m_stream.LocalEndPoint.AddressFamily != AddressFamily.InterNetwork) {
						throw new FtpException("Only IPv4 is supported by the PASV command. Use EPSV instead.");
					}

					// execute PRET before passive if server requires it
					if (HasFeature(FtpCapability.PRET)) {
						reply = await Execute("PRET " + command, token);
					}

					// execute PASV to try passive mode
					if (!(reply = await Execute("PASV", token)).Success) {
						throw new FtpCommandException(reply);
					}

					// get the passive port taking proxy config into account (if any)
					GetPassivePort(type, reply, out host, out port);

				}


				// break if too many tries
				a++;
				if (a >= Config.PassiveMaxAttempts) {
					throw new FtpException("Could not find a suitable port for PASV/EPSV Data Connection after trying " + Config.PassiveMaxAttempts + " times.");
				}

				// accept first port if not configured
				if (Config.PassiveBlockedPorts.IsBlank()) {
					break;
				}
				else {

					// check port against blacklist if configured
					if (!Config.PassiveBlockedPorts.Contains(port)) {

						// blacklist does not port, accept it
						break;
					}
					else {

						// blacklist contains port, try again
						continue;
					}
				}
			}

			stream = new FtpDataStream(this);
			stream.ConnectTimeout = Config.DataConnectionConnectTimeout;
			stream.ReadTimeout = Config.DataConnectionReadTimeout;
			await ConnectAsync(stream, host, port, Config.InternetProtocolVersions, token);
			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, Config.SocketKeepAlive);

			if (restart > 0) {
				// Fix for #887: When downloading through SOCKS proxy, the restart param is incorrect and needs to be ignored.
				// Restart is set to the length of the already downloaded file (i.e. if the file is 1000 bytes, it restarts with restart parameter 1000 or 1001 after file is successfully downloaded)
				if (IsProxy()) {
					var length = await GetFileSize(LastStreamPath, -1L, token);
					if (restart < length) {
						reply = await Execute("REST " + restart, token);
						if (!reply.Success) {
							throw new FtpCommandException(reply);
						}
					}
				}
				else {
					// Note: If this implementation causes an issue with non-proxy downloads too then we need to use the above implementation for all clients.
					if (!(reply = await Execute("REST " + restart, token)).Success) {
						throw new FtpCommandException(reply);
					}
				}
			}

			if (!(reply = await Execute(command, token)).Success) {
				await stream.CloseAsync(token);
				throw new FtpCommandException(reply);
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

			// this needs to take place after the command is executed
			if (Config.DataConnectionEncryption && Config.EncryptionMode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				await stream.ActivateEncryptionAsync(m_host,
					Config.ClientCertificates.Count > 0 ? Config.ClientCertificates : null,
					Config.SslProtocols,
					token: token);
			}

			return stream;
		}
	}
}