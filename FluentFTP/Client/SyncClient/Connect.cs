using System.Collections.Generic;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Connect
		/// </summary>
		public virtual void Connect() {
			Connect(false);
		}

		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public void Connect(FtpProfile profile) {

			// copy over the profile properties to this instance
			LoadProfile(profile);

			// begin connection
			Connect(false);
		}

		/// <summary>
		/// Connect to the server
		/// </summary>
		/// <param name="reConnect"> true indicates that we want a 
		/// reconnect to take place.</param>
		/// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
		public virtual void Connect(bool reConnect) {
			FtpReply reply;

			lock (m_lock) {

				// If we have never been connected before...
				if (this.Status.CachedHostIpads.Count == 0) {
					reConnect = false;
				}

				if (!reConnect) {

					LogFunction(nameof(Connect));
				}
				else {
					LogFunction("Re" + nameof(Connect));
				}

				LogVersion();

				if (IsDisposed) {
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
				}

				if (m_stream == null) {
					m_stream = new FtpSocketStream(this);
					m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
				}
				else {
					if (IsConnected) {
						((IInternalFtpClient)this).DisconnectInternal();
					}
				}

				if (Host == null) {
					throw new FtpException("No host has been specified");
				}

				if (m_capabilities == null) {
					m_capabilities = new List<FtpCapability>();
				}

				Status.Reset(reConnect);

				m_hashAlgorithms = FtpHashAlgorithm.NONE;
				m_stream.ConnectTimeout = Config.ConnectTimeout;
				m_stream.SocketPollInterval = Config.SocketPollInterval;
				Connect(m_stream);

				m_stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, Config.SocketKeepAlive);

				if (Config.EncryptionMode == FtpEncryptionMode.Implicit) {
					m_stream.ActivateEncryption(Host,
						Config.ClientCertificates.Count > 0 ? Config.ClientCertificates : null,
						Config.SslProtocols);
				}

				Handshake();
				m_serverType = ServerModule.DetectFtpServer(this, HandshakeReply);

				if (Config.SendHost) {
					if (!(reply = Execute("HOST " + (Config.SendHostDomain ?? Host))).Success) {
						throw new FtpException("HOST command failed.");
					}
				}

				// try to upgrade this connection to SSL if supported by the server
				if (Config.EncryptionMode is FtpEncryptionMode.Explicit or FtpEncryptionMode.Auto) {
					reply = Execute("AUTH TLS");
					if (!reply.Success) {
						Status.ConnectionFTPSFailure = true;
						if (Config.EncryptionMode == FtpEncryptionMode.Explicit) {
							throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
						}
					}
					else {
						m_stream.ActivateEncryption(Host,
							Config.ClientCertificates.Count > 0 ? Config.ClientCertificates : null,
							Config.SslProtocols);
					}
				}

				if (m_credentials != null) {
					Authenticate();
				}

				// configure the default FTPS settings
				if (IsEncrypted && Config.DataConnectionEncryption) {
					if (!(reply = Execute("PBSZ 0")).Success) {
						throw new FtpCommandException(reply);
					}

					if (!(reply = Execute("PROT P")).Success) {
						throw new FtpCommandException(reply);
					}
				}

				// if this is a clone these values should have already been loaded
				// so save some bandwidth and CPU time and skip executing this again.
				// otherwise clear the capabilities in case connection is reused to 
				// a different server 
				if (!reConnect && !m_isClone && Config.CheckCapabilities) {
					m_capabilities.Clear();
				}
				bool assumeCaps = false;
				if (m_capabilities.IsBlank() && Config.CheckCapabilities) {
					if ((reply = Execute("FEAT")).Success && reply.InfoMessages != null) {
						GetFeatures(reply);
					}
					else {
						assumeCaps = true;
					}
				}

				// Enable UTF8 if the encoding is ASCII and UTF8 is supported
				if (m_textEncodingAutoUTF && m_textEncoding == Encoding.ASCII && HasFeature(FtpCapability.UTF8)) {
					m_textEncoding = Encoding.UTF8;
				}

				LogWithPrefix(FtpTraceLevel.Info, "Text encoding: " + m_textEncoding.ToString());

				if (m_textEncoding == Encoding.UTF8) {
					// If the server supports UTF8 it should already be enabled and this
					// command should not matter however there are conflicting drafts
					// about this so we'll just execute it to be safe. 
					if ((reply = Execute("OPTS UTF8 ON")).Success) {
						Status.ConnectionUTF8Success = true;
					}
				}

				// Get the system type - Needed to auto-detect file listing parser
				if ((reply = Execute("SYST")).Success) {
					m_systemType = reply.Message;
					m_serverType = ServerModule.DetectFtpServerBySyst(this);
					m_serverOS = ServerModule.DetectFtpOSBySyst(this);
				}

				// Set a FTP server handler if a custom handler has not already been set
				if (ServerHandler == null) {
					ServerHandler = ServerModule.GetServerHandler(m_serverType);
				}

				// Assume the system's capabilities if FEAT command not supported by the server
				if (assumeCaps) {
					ServerFeatureModule.Assume(ServerHandler, m_capabilities, ref m_hashAlgorithms);
				}

				// Unless a custom list parser has been set,
				// Detect the listing parser and prefer machine listings over any other type
				// FIX : #739 prefer using machine listings to fix issues with GetListing and DeleteDirectory
				if (Config.ListingParser != FtpParser.Custom) {
					Config.ListingParser = ServerHandler != null ? ServerHandler.GetParser() : FtpParser.Auto;
					if (HasFeature(FtpCapability.MLSD)) {
						Config.ListingParser = FtpParser.Machine;
					}
				}

				// Create the parser even if the auto-OS detection failed
				CurrentListParser.Init(m_serverOS, Config.ListingParser);

				// FIX #318 always set the type when we create a new connection
				Status.CurrentDataType = FtpDataType.Unknown;

				// Execute server-specific post-connection event
				ServerHandler?.AfterConnected(this);

				if (reConnect) {
					// go back to previous CWD
					if (Status.LastWorkingDir != null) {
						SetWorkingDirectory(Status.LastWorkingDir);
					}
				}
				else {
					_ = GetWorkingDirectory();
				}

				// FIX #922: disable checking for stale data during connection
				Status.AllowCheckStaleData = true;
			}
		}

		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		protected virtual void Connect(FtpSocketStream stream) {
			stream.Connect(Host, Port, Config.InternetProtocolVersions);
		}

		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		protected virtual void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			stream.Connect(host, port, ipVersions);
		}

	}
}
