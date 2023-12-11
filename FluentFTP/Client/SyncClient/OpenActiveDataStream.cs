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
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		protected FtpDataStream OpenActiveDataStream(FtpDataConnectionType type, string command, long restart) {
			LogFunction(nameof(OpenActiveDataStream), new object[] { type, command, restart });

			var stream = new FtpDataStream(this);
			FtpReply reply;

			if (m_stream == null) {
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");
			}

			StartListeningOnPort(stream);
#if NETSTANDARD || NET5_0_OR_GREATER
			var args = stream.BeginAccept();
#endif
#if NETFRAMEWORK
			var ar = stream.BeginAccept(null, null);
#endif

			if (type is FtpDataConnectionType.EPRT or FtpDataConnectionType.AutoActive) {
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
				throw new FtpCommandException(reply);
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

#if NETSTANDARD || NET5_0_OR_GREATER
			stream.EndAccept(args, Config.DataConnectionConnectTimeout);
#endif
#if NETFRAMEWORK
			ar.AsyncWaitHandle.WaitOne(Config.DataConnectionConnectTimeout);
			if (Type.GetType("Mono.Runtime") == null) {
				ar.AsyncWaitHandle.Close(); // See issue #648 this needs to be commented out for MONO
			}
			if (!ar.IsCompleted) {
				stream.Close();
				throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
			}

			stream.EndAccept(ar);
#endif

			if (Config.DataConnectionEncryption && Config.EncryptionMode != FtpEncryptionMode.None && !Status.ConnectionFTPSFailure) {
				stream.ActivateEncryption(m_host,
					Config.ClientCertificates.Count > 0 ? Config.ClientCertificates : null,
					Config.SslProtocols);
			}

			stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, Config.SocketKeepAlive);
			stream.ReadTimeout = Config.DataConnectionReadTimeout;

			return stream;
		}

	}
}