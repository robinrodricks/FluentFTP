using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		protected FtpDataStream OpenPassiveDataStream(FtpDataConnectionType type, string command, long restart) {
			LogFunc(nameof(OpenPassiveDataStream), new object[] { type, command, restart });

			FtpDataStream stream = null;
			FtpReply reply;
			string host = null;
			var port = 0;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");
			}

			for (int a = 0; a <= m_PassiveMaxAttempts;) {

				if ((type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive) && !Status.EPSVNotSupported) {

					// execute EPSV to try enhanced-passive mode
					if (!(reply = Execute("EPSV")).Success) {

						// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
						if ((reply.Type == FtpResponseType.TransientNegativeCompletion || reply.Type == FtpResponseType.PermanentNegativeCompletion)
							&& type == FtpDataConnectionType.AutoPassive
							&& m_stream != null
							&& m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {
							// mark EPSV not supported so we do not try EPSV again during this connection
							Status.EPSVNotSupported = true;
							return OpenPassiveDataStream(FtpDataConnectionType.PASV, command, restart);
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
						reply = Execute("PRET " + command);
					}

					// execute PASV to try passive mode
					if (!(reply = Execute("PASV")).Success) {
						throw new FtpCommandException(reply);
					}

					// get the passive port taking proxy config into account (if any)
					GetPassivePort(type, reply, out host, out port);

				}



				// break if too many tries
				a++;
				if (a >= m_PassiveMaxAttempts) {
					throw new FtpException("Could not find a suitable port for PASV/EPSV Data Connection after trying " + m_PassiveMaxAttempts + " times.");
				}

				// accept first port if not configured
				if (m_PassiveBlockedPorts.IsBlank()) {
					break;
				}
				else {

					// check port against blacklist if configured
					if (!m_PassiveBlockedPorts.Contains(port)) {

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
			stream.ConnectTimeout = DataConnectionConnectTimeout;
			stream.ReadTimeout = DataConnectionReadTimeout;
			Connect(stream, host, port, InternetProtocolVersions);
			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);

			if (restart > 0) {
				// Fix for #887: When downloading through SOCKS proxy, the restart param is incorrect and needs to be ignored.
				// Restart is set to the length of the already downloaded file (i.e. if the file is 1000 bytes, it restarts with restart parameter 1000 or 1001 after file is successfully downloaded)
				if (IsProxy()) {
					var length = GetFileSize(m_path);
					if (restart < length) {
						reply = Execute("REST " + restart);
						if (!reply.Success) {
							throw new FtpCommandException(reply);
						}
					}
				}
				else {
					// Note: If this implementation causes an issue with non-proxy downloads too then we need to use the above implementation for all clients.
					if (!(reply = Execute("REST " + restart)).Success) {
						throw new FtpCommandException(reply);
					}
				}
			}

			if (!(reply = Execute(command)).Success) {
				stream.Close();
				if (command.StartsWith("NLST ") && reply.Code == "550" && reply.Message == "No files found.") {
					//workaround for ftpd which responses "550 No files found." when folder exists but is empty
				}
				else {
					throw new FtpCommandException(reply);
				}
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

			// this needs to take place after the command is executed
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				stream.ActivateEncryption(m_host,
					ClientCertificates.Count > 0 ? ClientCertificates : null,
					m_SslProtocols);
			}

			return stream;
		}

#if ASYNC
		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		protected async Task<FtpDataStream> OpenPassiveDataStreamAsync(FtpDataConnectionType type, string command, long restart, CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(OpenPassiveDataStreamAsync), new object[] { type, command, restart });

			FtpDataStream stream = null;
			FtpReply reply;
			string host = null;
			var port = 0;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");
			}


			for (int a = 0; a <= m_PassiveMaxAttempts;) {

				if ((type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive) && !Status.EPSVNotSupported) {
					// execute EPSV to try enhanced-passive mode
					if (!(reply = await ExecuteAsync("EPSV", token)).Success) {
						// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
						if ((reply.Type == FtpResponseType.TransientNegativeCompletion || reply.Type == FtpResponseType.PermanentNegativeCompletion)
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
						reply = await ExecuteAsync("PRET " + command, token);
					}

					// execute PASV to try passive mode
					if (!(reply = await ExecuteAsync("PASV", token)).Success) {
						throw new FtpCommandException(reply);
					}

					// get the passive port taking proxy config into account (if any)
					GetPassivePort(type, reply, out host, out port);

				}


				// break if too many tries
				a++;
				if (a >= m_PassiveMaxAttempts) {
					throw new FtpException("Could not find a suitable port for PASV/EPSV Data Connection after trying " + m_PassiveMaxAttempts + " times.");
				}

				// accept first port if not configured
				if (m_PassiveBlockedPorts.IsBlank()) {
					break;
				}
				else {

					// check port against blacklist if configured
					if (!m_PassiveBlockedPorts.Contains(port)) {

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
			stream.ConnectTimeout = DataConnectionConnectTimeout;
			stream.ReadTimeout = DataConnectionReadTimeout;
			await ConnectAsync(stream, host, port, InternetProtocolVersions, token);
			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);

			if (restart > 0) {
				// Fix for #887: When downloading through SOCKS proxy, the restart param is incorrect and needs to be ignored.
				// Restart is set to the length of the already downloaded file (i.e. if the file is 1000 bytes, it restarts with restart parameter 1000 or 1001 after file is successfully downloaded)
				if (IsProxy()) {
					var length = await GetFileSizeAsync(m_path, -1L, token);
					if (restart < length) {
						reply = await ExecuteAsync("REST " + restart, token);
						if (!reply.Success) {
							throw new FtpCommandException(reply);
						}
					}
				}
				else {
					// Note: If this implementation causes an issue with non-proxy downloads too then we need to use the above implementation for all clients.
					if (!(reply = await ExecuteAsync("REST " + restart, token)).Success) {
						throw new FtpCommandException(reply);
					}
				}
			}

			if (!(reply = await ExecuteAsync(command, token)).Success) {
				stream.Close();
				throw new FtpCommandException(reply);
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

			// this needs to take place after the command is executed
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				await stream.ActivateEncryptionAsync(m_host,
					ClientCertificates.Count > 0 ? ClientCertificates : null,
					m_SslProtocols);
			}

			return stream;
		}
#endif

		/// <summary>
		/// Parse the host and port number from an EPSV response
		/// </summary>
		protected void GetEnhancedPassivePort(FtpReply reply, out string host, out int port) {
			var m = Regex.Match(reply.Message, @"\(\|\|\|(?<port>\d+)\|\)");
			if (!m.Success) {
				// In the case that ESPV is responded with a regular "Entering Passive Mode" instead, we'll try that parsing before we raise the exception
				/* Example:
				Command: EPSV
				Response: 227 Entering Passive Mode(XX, XX, XX, XX, 143, 225).
				*/

				try {
					GetPassivePort(FtpDataConnectionType.AutoPassive, reply, out host, out port);
					return;
				}
				catch {
					throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
				}
			}
			// If ESPV is responded with Entering Extended Passive. The IP must remain the same.
			/* Example:
			Command: EPSV
			Response: 229 Entering Extended Passive Mode(|||10016|)

			If we set the host to ftp.host.com and ftp.host.com has multiple ip's we may end up with the wrong ip.
			Making sure that we use the same IP.
			host = m_host; 
			*/
			host = SocketRemoteEndPoint.Address.ToString();
			port = int.Parse(m.Groups["port"].Value);
		}

		/// <summary>
		/// Parse the host and port number from an PASV or PASVEX response
		/// </summary>
		protected void GetPassivePort(FtpDataConnectionType type, FtpReply reply, out string host, out int port) {
			var m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// PASVEX mode ignores the host supplied in the PASV response
			if (type == FtpDataConnectionType.PASVEX) {
				host = m_host;
			}
			else {
				host = m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value;
			}

			port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

			// Fix #409 for BlueCoat proxy connections. This code replaces the name of the proxy with the name of the FTP server and then nothing works.
			if (!IsProxy()) {
				//use host ip if server advertises a non-routable IP
				m = Regex.Match(host, @"(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");

				if (m.Success) {
					host = m_host;
				}
			}
		}

		/// <summary>
		/// Returns the IP address to be sent to the server for the active connection.
		/// </summary>
		/// <param name="ip"></param>
		/// <returns></returns>
		protected string GetLocalAddress(IPAddress ip) {

			// Use resolver
			if (m_AddressResolver != null) {
				return m_Address ?? (m_Address = m_AddressResolver());
			}

			// Use supplied IP
			return ip.ToString();
		}

	}
}