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
	public partial class FtpClient {

		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		protected FtpDataStream OpenPassiveDataStream(FtpDataConnectionType type, string command, long restart) {
			LogFunction(nameof(OpenPassiveDataStream), new object[] { type, command, restart });

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
					if (!(reply = Execute("EPSV")).Success) {

						// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
						if (reply.Type is FtpResponseType.TransientNegativeCompletion or FtpResponseType.PermanentNegativeCompletion
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
			Connect(stream, host, port, Config.InternetProtocolVersions);
			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, Config.SocketKeepAlive);

			if (restart > 0) {
				// Fix for #887: When downloading through SOCKS proxy, the restart param is incorrect and needs to be ignored.
				// Restart is set to the length of the already downloaded file (i.e. if the file is 1000 bytes, it restarts with restart parameter 1000 or 1001 after file is successfully downloaded)
				if (IsProxy()) {
					var length = GetFileSize(LastStreamPath);
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
			if (Config.DataConnectionEncryption && Config.EncryptionMode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				stream.ActivateEncryption(m_host,
					Config.ClientCertificates.Count > 0 ? Config.ClientCertificates : null,
					Config.SslProtocols);
			}

			return stream;
		}


	}
}