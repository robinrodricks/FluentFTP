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
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		protected FtpDataStream OpenActiveDataStream(FtpDataConnectionType type, string command, long restart) {
			LogFunc(nameof(OpenActiveDataStream), new object[] { type, command, restart });

			var stream = new FtpDataStream(this);
			FtpReply reply;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");
			}

			StartListeningOnPort(stream);
			var args = stream.BeginAccept();

			if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive) {
				var ipver = 0;

				switch (stream.LocalEndPoint.AddressFamily) {
					case AddressFamily.InterNetwork:
						ipver = 1; // IPv4
						break;

					case AddressFamily.InterNetworkV6:
						ipver = 2; // IPv6
						break;

					default:
						throw new InvalidOperationException("The IP protocol being used is not supported.");
				}

				if (!(reply = Execute("EPRT |" + ipver + "|" + GetLocalAddress(stream.LocalEndPoint.Address) + "|" + stream.LocalEndPoint.Port + "|")).Success) {
					// if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
					if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {
						stream.ControlConnection = null; // we don't want this failed EPRT attempt to close our control connection when the stream is closed so clear out the reference.
						stream.Close();
						return OpenActiveDataStream(FtpDataConnectionType.PORT, command, restart);
					}
					else {
						stream.Close();
						throw new FtpCommandException(reply);
					}
				}
			}
			else {
				if (m_stream.LocalEndPoint.AddressFamily != AddressFamily.InterNetwork) {
					throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");
				}

				if (!(reply = Execute("PORT " +
									  GetLocalAddress(stream.LocalEndPoint.Address).Replace('.', ',') + "," +
									  stream.LocalEndPoint.Port / 256 + "," +
									  stream.LocalEndPoint.Port % 256)).Success) {
					stream.Close();
					throw new FtpCommandException(reply);
				}
			}

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
				throw new FtpCommandException(reply);
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

			stream.EndAccept(args, m_dataConnectionConnectTimeout);

			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				stream.ActivateEncryption(m_host,
					ClientCertificates.Count > 0 ? ClientCertificates : null,
					m_SslProtocols);
			}

			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);
			stream.ReadTimeout = m_dataConnectionReadTimeout;

			return stream;
		}

#if ASYNC
		/// <summary>
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A data stream ready to be used</returns>
		protected async Task<FtpDataStream> OpenActiveDataStreamAsync(FtpDataConnectionType type, string command, long restart, CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(OpenActiveDataStreamAsync), new object[] { type, command, restart });

			var stream = new FtpDataStream(this);
			FtpReply reply;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");
			}

			StartListeningOnPort(stream);

			var args = stream.BeginAccept();

			if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive) {
				var ipver = 0;

				switch (stream.LocalEndPoint.AddressFamily) {
					case AddressFamily.InterNetwork:
						ipver = 1; // IPv4
						break;

					case AddressFamily.InterNetworkV6:
						ipver = 2; // IPv6
						break;

					default:
						throw new InvalidOperationException("The IP protocol being used is not supported.");
				}

				if (!(reply = await ExecuteAsync("EPRT |" + ipver + "|" + GetLocalAddress(stream.LocalEndPoint.Address) + "|" + stream.LocalEndPoint.Port + "|", token)).Success) {
					// if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
					if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {
						stream.ControlConnection = null; // we don't want this failed EPRT attempt to close our control connection when the stream is closed so clear out the reference.
						stream.Close();
						return await OpenActiveDataStreamAsync(FtpDataConnectionType.PORT, command, restart, token);
					}
					else {
						stream.Close();
						throw new FtpCommandException(reply);
					}
				}
			}
			else {
				if (m_stream.LocalEndPoint.AddressFamily != AddressFamily.InterNetwork) {
					throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");
				}

				if (!(reply = await ExecuteAsync("PORT " +
												 GetLocalAddress(stream.LocalEndPoint.Address).Replace('.', ',') + "," +
												 stream.LocalEndPoint.Port / 256 + "," +
												 stream.LocalEndPoint.Port % 256, token)).Success) {
					stream.Close();
					throw new FtpCommandException(reply);
				}
			}

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

			stream.EndAccept(args, m_dataConnectionConnectTimeout);

			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				await stream.ActivateEncryptionAsync(m_host,
					ClientCertificates.Count > 0 ? ClientCertificates : null,
					m_SslProtocols);
			}

			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);
			stream.ReadTimeout = m_dataConnectionReadTimeout;

			return stream;
		}
#endif

	}
}