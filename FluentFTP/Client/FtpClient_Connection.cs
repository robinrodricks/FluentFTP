using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP {

	/// <summary>
	/// A connection to a single FTP server. Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. See the FAQ on our Github project page for more info.
	/// </summary>
	public partial class FtpClient : IDisposable {
		
		#region Constructor / Destructor

		/// <summary>
		/// Creates a new instance of an FTP Client.
		/// </summary>
		public FtpClient() {
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host.
		/// </summary>
		public FtpClient(string host) {
			Host = host;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials) {
			Host = host;
			Credentials = credentials;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(string host, int port, NetworkCredential credentials) {
			Host = host;
			Port = port;
			Credentials = credentials;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, username and password.
		/// </summary>
		public FtpClient(string host, string user, string pass) {
			Host = host;
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port, username and password.
		/// </summary>
		public FtpClient(string host, int port, string user, string pass) {
			Host = host;
			Port = port;
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		/// <returns></returns>
		protected virtual FtpClient Create() {
			return new FtpClient();
		}

		/// <summary>
		/// Disconnects from the server, releases resources held by this
		/// object.
		/// </summary>
		public virtual void Dispose() {
#if !CORE14
			lock (m_lock) {
#endif
				if (IsDisposed)
					return;

				this.LogFunc("Dispose");
				this.LogStatus(FtpTraceLevel.Verbose, "Disposing FtpClient object...");

				try {
					if (IsConnected) {
						Disconnect();
					}
				} catch (Exception ex) {
					this.LogLine(FtpTraceLevel.Warn, "FtpClient.Dispose(): Caught and discarded an exception while disconnecting from host: " + ex.ToString());
				}

				if (m_stream != null) {
					try {
						m_stream.Dispose();
					} catch (Exception ex) {
						this.LogLine(FtpTraceLevel.Warn, "FtpClient.Dispose(): Caught and discarded an exception while disposing FtpStream object: " + ex.ToString());
					} finally {
						m_stream = null;
					}
				}

				m_credentials = null;
				m_textEncoding = null;
				m_host = null;
				m_asyncmethods.Clear();
				IsDisposed = true;
				GC.SuppressFinalize(this);
#if !CORE14
			}
#endif
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~FtpClient() {
			Dispose();
		}

		#endregion

		#region Clone

		/// <summary>
		/// Clones the control connection for opening multiple data streams
		/// </summary>
		/// <returns>A new control connection with the same property settings as this one</returns>
		/// <example><code source="..\Examples\CloneConnection.cs" lang="cs" /></example>
		protected FtpClient CloneConnection() {
			FtpClient conn = Create();

			conn.m_isClone = true;

			// configure new connection as clone of self
			conn.InternetProtocolVersions = InternetProtocolVersions;
			conn.SocketPollInterval = SocketPollInterval;
			conn.StaleDataCheck = StaleDataCheck;
			conn.EnableThreadSafeDataConnections = EnableThreadSafeDataConnections;
			conn.Encoding = Encoding;
			conn.Host = Host;
			conn.Port = Port;
			conn.Credentials = Credentials;
			conn.MaximumDereferenceCount = MaximumDereferenceCount;
			conn.ClientCertificates = ClientCertificates;
			conn.DataConnectionType = DataConnectionType;
			conn.UngracefullDisconnection = UngracefullDisconnection;
			conn.ConnectTimeout = ConnectTimeout;
			conn.ReadTimeout = ReadTimeout;
			conn.DataConnectionConnectTimeout = DataConnectionConnectTimeout;
			conn.DataConnectionReadTimeout = DataConnectionReadTimeout;
			conn.SocketKeepAlive = SocketKeepAlive;
			conn.Capabilities = Capabilities;
			conn.EncryptionMode = EncryptionMode;
			conn.DataConnectionEncryption = DataConnectionEncryption;
			conn.SslProtocols = SslProtocols;
			conn.SslBuffering = SslBuffering;
			conn.TransferChunkSize = TransferChunkSize;
			conn.ListingParser = ListingParser;
			conn.ListingCulture = ListingCulture;
			conn.TimeOffset = TimeOffset;
			conn.RetryAttempts = RetryAttempts;
			conn.UploadRateLimit = UploadRateLimit;
			conn.DownloadRateLimit = DownloadRateLimit;
			conn.DownloadDataType = DownloadDataType;
			conn.UploadDataType = UploadDataType;
			conn.ActivePorts = ActivePorts;

			// fix for #428: OpenRead with EnableThreadSafeDataConnections always uses ASCII
			conn.CurrentDataType = CurrentDataType;

#if !CORE
			conn.PlainTextEncryption = PlainTextEncryption;
#endif

			// copy props using attributes (slower, not .NET core compatible)
			/*foreach (PropertyInfo prop in GetType().GetProperties()) {
				object[] attributes = prop.GetCustomAttributes(typeof(FtpControlConnectionClone), true);

				if (attributes.Length > 0) {
					prop.SetValue(conn, prop.GetValue(this, null), null);
				}
			}*/

			// always accept certificate no matter what because if code execution ever
			// gets here it means the certificate on the control connection object being
			// cloned was already accepted.
			conn.ValidateCertificate += new FtpSslValidation(
				delegate (FtpClient obj, FtpSslValidationEventArgs e) {
					e.Accept = true;
				});

			return conn;
		}

		#endregion

		#region Connect

		private FtpListParser m_listParser;

		/// <summary>
		/// Connect to the server
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
		/// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
		public virtual void Connect() {
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif

				this.LogFunc("Connect");

				if (IsDisposed)
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");

				if (m_stream == null) {
					m_stream = new FtpSocketStream(m_SslProtocols);
					m_stream.Client = this;
					m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
				} else {
					if (IsConnected) {
						Disconnect();
					}
				}

				if (Host == null) {
					throw new FtpException("No host has been specified");
				}

				if (!IsClone) {
					m_caps = FtpCapability.NONE;
				}

				m_hashAlgorithms = FtpHashAlgorithm.NONE;
				m_stream.ConnectTimeout = m_connectTimeout;
				m_stream.SocketPollInterval = m_socketPollInterval;
				Connect(m_stream);

				m_stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

#if !NO_SSL
				if (EncryptionMode == FtpEncryptionMode.Implicit) {
					m_stream.ActivateEncryption(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
				}
#endif

				Handshake();
				DetectFtpServer();

#if !NO_SSL
				if (EncryptionMode == FtpEncryptionMode.Explicit) {
					if (!(reply = Execute("AUTH TLS")).Success) {
						throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
					}
					m_stream.ActivateEncryption(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
				}
#endif

				if (m_credentials != null) {
					Authenticate();
				}

				if (m_stream.IsEncrypted && DataConnectionEncryption) {
					if (!(reply = Execute("PBSZ 0")).Success) {
						throw new FtpCommandException(reply);
					}
					if (!(reply = Execute("PROT P")).Success) {
						throw new FtpCommandException(reply);
					}
				}

				// if this is a clone these values should have already been loaded
				// so save some bandwidth and CPU time and skip executing this again.
				if (!IsClone && m_checkCapabilities) {
					if ((reply = Execute("FEAT")).Success && reply.InfoMessages != null) {
						GetFeatures(reply);
					} else {
						AssumeCapabilities();
					}
				}

				// Enable UTF8 if the encoding is ASCII and UTF8 is supported
				if (m_textEncodingAutoUTF && m_textEncoding == Encoding.ASCII && HasFeature(FtpCapability.UTF8)) {
					m_textEncoding = Encoding.UTF8;
				}

				this.LogStatus(FtpTraceLevel.Info, "Text encoding: " + m_textEncoding.ToString());

				if (m_textEncoding == Encoding.UTF8) {
					// If the server supports UTF8 it should already be enabled and this
					// command should not matter however there are conflicting drafts
					// about this so we'll just execute it to be safe. 
					Execute("OPTS UTF8 ON");
				}

				// Get the system type - Needed to auto-detect file listing parser
				if ((reply = Execute("SYST")).Success) {
					m_systemType = reply.Message;
					DetectFtpServerBySyst();
				}

#if !NO_SSL && !CORE
				if (m_stream.IsEncrypted && PlainTextEncryption) {
					if (!(reply = Execute("CCC")).Success) {
						throw new FtpSecurityNotAvailableException("Failed to disable encryption with CCC command. Perhaps your server does not support it or is not configured to allow it.");
					} else {

						// close the SslStream and send close_notify command to server
						m_stream.DeactivateEncryption();

						// read stale data (server's reply?)
						ReadStaleData(false, true, false);
					}
				}
#endif

				// Create the parser even if the auto-OS detection failed
				m_listParser.Init(m_systemType);

				// FIX : #318 always set the type when we create a new connection
				ForceSetDataType = true;

#if !CORE14
			}
#endif
		}

#if ASYNC
        // TODO: add example
        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
        /// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
        public virtual async Task ConnectAsync(CancellationToken token = default(CancellationToken))
        {
            FtpReply reply;

            this.LogFunc(nameof(ConnectAsync));

            if (IsDisposed)
                throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");

            if (m_stream == null)
            {
                m_stream = new FtpSocketStream(m_SslProtocols);
				m_stream.Client = this;
                m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
            }
            else
            {
                if (IsConnected)
                {
                    Disconnect();
                }
            }

            if (Host == null)
            {
                throw new FtpException("No host has been specified");
            }

            if (!IsClone)
            {
                m_caps = FtpCapability.NONE;
            }

            m_hashAlgorithms = FtpHashAlgorithm.NONE;
            m_stream.ConnectTimeout = m_connectTimeout;
            m_stream.SocketPollInterval = m_socketPollInterval;
            await ConnectAsync(m_stream, token);

            m_stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

#if !NO_SSL
            if (EncryptionMode == FtpEncryptionMode.Implicit) {
                await m_stream.ActivateEncryptionAsync(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
            }
#endif

            await HandshakeAsync(token);
			DetectFtpServer();

#if !NO_SSL
            if (EncryptionMode == FtpEncryptionMode.Explicit) {
                if (!(reply = await ExecuteAsync("AUTH TLS", token)).Success) {
                    throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
                }
                await m_stream.ActivateEncryptionAsync(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
            }
#endif

			if (m_credentials != null)
            {
                await AuthenticateAsync(token);
            }

            if (m_stream.IsEncrypted && DataConnectionEncryption)
            {
                if (!(reply = await ExecuteAsync("PBSZ 0", token)).Success){
                    throw new FtpCommandException(reply);
				}
                if (!(reply = await ExecuteAsync("PROT P", token)).Success){
                    throw new FtpCommandException(reply);
				}
            }

			// if this is a clone these values should have already been loaded
			// so save some bandwidth and CPU time and skip executing this again.
			if (!IsClone && m_checkCapabilities) {
				if ((reply = await ExecuteAsync("FEAT", token)).Success && reply.InfoMessages != null)
                {
                    GetFeatures(reply);
                }else {
						AssumeCapabilities();
				}
            }

            // Enable UTF8 if the encoding is ASCII and UTF8 is supported
            if (m_textEncodingAutoUTF && m_textEncoding == Encoding.ASCII && HasFeature(FtpCapability.UTF8))
            {
                m_textEncoding = Encoding.UTF8;
            }

            this.LogStatus(FtpTraceLevel.Info, "Text encoding: " + m_textEncoding.ToString());

            if (m_textEncoding == Encoding.UTF8)
            {
                // If the server supports UTF8 it should already be enabled and this
                // command should not matter however there are conflicting drafts
                // about this so we'll just execute it to be safe. 
                await ExecuteAsync("OPTS UTF8 ON", token);
            }

            // Get the system type - Needed to auto-detect file listing parser
            if ((reply = await ExecuteAsync("SYST", token)).Success)
            {
                m_systemType = reply.Message;
				DetectFtpServerBySyst();
            }

#if !NO_SSL && !CORE
            if (m_stream.IsEncrypted && PlainTextEncryption) {
                if (!(reply = await ExecuteAsync("CCC", token)).Success)
                {
                    throw new FtpSecurityNotAvailableException("Failed to disable encryption with CCC command. Perhaps your server does not support it or is not configured to allow it.");
                } else {

                    // close the SslStream and send close_notify command to server
                    m_stream.DeactivateEncryption();

                    // read stale data (server's reply?)
                    await ReadStaleDataAsync(false, true, false, token);
                }
            }
#endif

			// Create the parser even if the auto-OS detection failed
			m_listParser.Init(m_systemType);
        }
#endif

		/// <summary>
		/// Connect to the FTP server. Overwritten in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		protected virtual void Connect(FtpSocketStream stream) {
			stream.Connect(Host, Port, InternetProtocolVersions);
		}

#if ASYNC
		/// <summary>
		/// Connect to the FTP server. Overwritten in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		protected virtual async Task ConnectAsync(FtpSocketStream stream, CancellationToken token)
        {
            await stream.ConnectAsync(Host, Port, InternetProtocolVersions, token);
        }
#endif

		/// <summary>
		/// Connect to the FTP server. Overwritten in proxy classes.
		/// </summary>
		protected virtual void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			stream.Connect(host, port, ipVersions);
		}

#if ASYNC
        /// <summary>
        /// Connect to the FTP server. Overwritten in proxy classes.
        /// </summary>
        protected virtual Task ConnectAsync(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions, CancellationToken token)
        {
            return stream.ConnectAsync(host, port, ipVersions, token);
        }
#endif

		protected FtpReply HandshakeReply;

		/// <summary>
		/// Called during Connect(). Typically extended by FTP proxies.
		/// </summary>
		protected virtual void Handshake() {
			FtpReply reply;
			if (!(reply = GetReply()).Success) {
				if (reply.Code == null) {
					throw new IOException("The connection was terminated before a greeting could be read.");
				} else {
					throw new FtpCommandException(reply);
				}
			}
			HandshakeReply = reply;
		}

#if ASYNC
        /// <summary>
        /// Called during <see cref="ConnectAsync()"/>. Typically extended by FTP proxies.
        /// </summary>
        protected virtual async Task HandshakeAsync(CancellationToken token = default(CancellationToken))
        {
            FtpReply reply;
            if (!(reply = await GetReplyAsync(token)).Success)
            {
                if (reply.Code == null)
                {
                    throw new IOException("The connection was terminated before a greeting could be read.");
                }
                else
                {
                    throw new FtpCommandException(reply);
                }
            }
			HandshakeReply = reply;
        }
#endif

		/// <summary>
		/// Populates the capabilities flags based on capabilities
		/// supported by this server. This method is overridable
		/// so that new features can be supported
		/// </summary>
		/// <param name="reply">The reply object from the FEAT command. The InfoMessages property will
		/// contain a list of the features the server supported delimited by a new line '\n' character.</param>
		protected virtual void GetFeatures(FtpReply reply) {
			GetFeatures(reply.InfoMessages.Split('\n'));
		}

#if !CORE
		delegate void AsyncConnect();

		/// <summary>
		/// Initiates a connection to the server
		/// </summary>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
		public IAsyncResult BeginConnect(AsyncCallback callback, object state) {
			AsyncConnect func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = new AsyncConnect(Connect)).BeginInvoke(callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous connection attempt to the server from <see cref="BeginConnect"/>
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="BeginConnect"/></param>
		/// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
		public void EndConnect(IAsyncResult ar) {
			GetAsyncDelegate<AsyncConnect>(ar).EndInvoke(ar);
		}
#endif

		#endregion

		#region Auto Connect

		private static List<FtpEncryptionMode> autoConnectEncryption = new List<FtpEncryptionMode> {
			FtpEncryptionMode.None, FtpEncryptionMode.Implicit, FtpEncryptionMode.Explicit
		};
		private static List<SysSslProtocols> autoConnectProtocols = new List<SysSslProtocols> {
			SysSslProtocols.None, SysSslProtocols.Ssl2, SysSslProtocols.Ssl3, SysSslProtocols.Tls,
#if !CORE
			SysSslProtocols.Default, 
#endif
#if ASYNC
			SysSslProtocols.Tls11, SysSslProtocols.Tls12,
#endif
		};
		private static List<FtpDataConnectionType> autoConnectData = new List<FtpDataConnectionType> {
			FtpDataConnectionType.EPRT,FtpDataConnectionType.EPSV, FtpDataConnectionType.PASV, FtpDataConnectionType.PASVEX, FtpDataConnectionType.PORT
		};
		private static List<Encoding> autoConnectEncoding = new List<Encoding> {
			Encoding.UTF8, Encoding.ASCII
		};


		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public void Connect(FtpProfile profile) {

			// copy over the profile properties to this instance
			this.Host = profile.Host;
			this.Credentials = profile.Credentials;
			this.EncryptionMode = profile.Encryption;
			this.SslProtocols = profile.Protocols;
			this.DataConnectionType = profile.DataConnection;
			this.Encoding = profile.Encoding;

			// begin connection
			this.Connect();
		}

#if ASYNC
		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public async Task ConnectAsync(FtpProfile profile) {

			// copy over the profile properties to this instance
			this.Host = profile.Host;
			this.Credentials = profile.Credentials;
			this.EncryptionMode = profile.Encryption;
			this.SslProtocols = profile.Protocols;
			this.DataConnectionType = profile.DataConnection;
			this.Encoding = profile.Encoding;

			// begin connection
			await this.ConnectAsync();
		}
#endif

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)?</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(bool firstOnly = true) {
			var results = new List<FtpProfile>();

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("AutoDetect", new object[] { firstOnly });

				if (IsDisposed)
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");

				if (Host == null) {
					throw new FtpException("No host has been specified. Please set the 'Host' property before trying to auto connect.");
				}
				if (Credentials == null) {
					throw new FtpException("No username and password has been specified. Please set the 'Credentials' property before trying to auto connect.");
				}

				// try each encoding
				encoding: foreach (var encoding in autoConnectEncoding) {

					// try each encryption mode
					encryption: foreach (var encryption in autoConnectEncryption) {

						// try each SSL protocol
						protocol: foreach (var protocol in autoConnectProtocols) {

							// skip secure protocols if testing plain FTP
							if (encryption == FtpEncryptionMode.None && protocol != SysSslProtocols.None) {
								continue;
							}

							// try each data connection type
							dataType: foreach (var dataType in autoConnectData) {

								// clone this connection
								var conn = this.CloneConnection();

								// set basic props
								conn.Host = this.Host;
								conn.Port = this.Port;
								conn.Credentials = this.Credentials;

								// set rolled props
								conn.EncryptionMode = encryption;
								conn.SslProtocols = protocol;
								conn.DataConnectionType = dataType;
								conn.Encoding = encoding;

								// try to connect
								var connected = false;
								try {
									conn.Connect();
									connected = true;
								} catch (Exception ex) {

								}

								// if it worked, add the profile
								if (connected) {
									results.Add(new FtpProfile {
										Host = this.Host,
										Credentials = this.Credentials,
										Encryption = encryption,
										Protocols = protocol,
										DataConnection = dataType,
										Encoding = encoding
									});

									// stop if only 1 wanted
									if (firstOnly) {
										return results;
									}
								}


							}
						}
					}
				}


#if !CORE14
			}
#endif

			return results;
		}

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// </summary>
		public FtpProfile AutoConnect() {

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("AutoConnect");

				// detect the first available connection profile
				var results = this.AutoDetect();
				if (results.Count > 1) {
					var profile = results[0];

					// if we are using SSL, set a basic server acceptance function
					if (profile.Encryption != FtpEncryptionMode.None) {
						this.ValidateCertificate += new FtpSslValidation(delegate (FtpClient c, FtpSslValidationEventArgs e) {
							if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
								e.Accept = false;
							} else {
								e.Accept = true;
							}
						});
					}

					// connect to the first found profile
					this.Connect(profile);

					// return the working profile
					return profile;
				}
#if !CORE14
			}
#endif

			return null;
		}

#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// </summary>
		public async Task<FtpProfile> AutoConnect() {

#if !CORE14
			lock (m_lock) {
#endif
				this.LogFunc("AutoConnect");

				// detect the first available connection profile
				var results = this.AutoDetect();
				if (results.Count > 1) {
					var profile = results[0];

					// if we are using SSL, set a basic server acceptance function
					if (profile.Encryption != FtpEncryptionMode.None) {
						this.ValidateCertificate += new FtpSslValidation(delegate (FtpClient c, FtpSslValidationEventArgs e) {
							if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
								e.Accept = false;
							} else {
								e.Accept = true;
							}
						});
					}

					// connect to the first found profile
					await this.ConnectAsync(profile);

					// return the working profile
					return profile;
				}
#if !CORE14
			}
#endif

			return null;
		}
#endif

#endregion

#region Login

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual void Authenticate() {
			Authenticate(Credentials.UserName, Credentials.Password);
		}

#if ASYNC
        /// <summary>
        /// Performs a login on the server. This method is overridable so
        /// that the login procedure can be changed to support, for example,
        /// a FTP proxy.
        /// </summary>
        protected virtual async Task AuthenticateAsync(CancellationToken token)
        {
            await AuthenticateAsync(Credentials.UserName, Credentials.Password, token);
        }
#endif

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		/// <exception cref="FtpAuthenticationException">On authentication failures</exception>
		/// <remarks>
		/// To handle authentication failures without retries, catch FtpAuthenticationException.
		/// </remarks>
		protected virtual void Authenticate(string userName, string password) {
			FtpReply reply;

			if (!(reply = Execute("USER " + userName)).Success) {
				throw new FtpAuthenticationException(reply);
			}

			if (reply.Type == FtpResponseType.PositiveIntermediate &&
				!(reply = Execute("PASS " + password)).Success) {
				throw new FtpAuthenticationException(reply);
			}
		}

#if ASYNC
        /// <summary>
        /// Performs a login on the server. This method is overridable so
        /// that the login procedure can be changed to support, for example,
        /// a FTP proxy.
        /// </summary>
		/// <exception cref="FtpAuthenticationException">On authentication failures</exception>
		/// <remarks>
		/// To handle authentication failures without retries, catch FtpAuthenticationException.
		/// </remarks>
        protected virtual async Task AuthenticateAsync(string userName, string password, CancellationToken token)
        {
            FtpReply reply;

            if (!(reply = await ExecuteAsync("USER " + userName, token)).Success){
                throw new FtpAuthenticationException(reply);
			}

            if (reply.Type == FtpResponseType.PositiveIntermediate
                && !(reply = await ExecuteAsync("PASS " + password, token)).Success){
                throw new FtpAuthenticationException(reply);
			}
        }
#endif

#endregion

#region Disconnect

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		public virtual void Disconnect() {
#if !CORE14
			lock (m_lock) {
#endif
				if (m_stream != null && m_stream.IsConnected) {
					try {
						if (!UngracefullDisconnection) {
							Execute("QUIT");
						}
					} catch (SocketException sockex) {
						this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): SocketException caught and discarded while closing control connection: " + sockex.ToString());
					} catch (IOException ioex) {
						this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): IOException caught and discarded while closing control connection: " + ioex.ToString());
					} catch (FtpCommandException cmdex) {
						this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): FtpCommandException caught and discarded while closing control connection: " + cmdex.ToString());
					} catch (FtpException ftpex) {
						this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): FtpException caught and discarded while closing control connection: " + ftpex.ToString());
					} finally {
						m_stream.Close();
					}
				}
#if !CORE14
			}
#endif
		}

#if !CORE
		delegate void AsyncDisconnect();

		/// <summary>
		/// Initiates a disconnection on the server
		/// </summary>
		/// <param name="callback"><see cref="AsyncCallback"/> method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDisconnect.cs" lang="cs" /></example>
		public IAsyncResult BeginDisconnect(AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDisconnect func;

			lock (m_asyncmethods) {
				ar = (func = new AsyncDisconnect(Disconnect)).BeginInvoke(callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginDisconnect"/>
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="BeginDisconnect"/></param>
		/// <example><code source="..\Examples\BeginConnect.cs" lang="cs" /></example>
		public void EndDisconnect(IAsyncResult ar) {
			GetAsyncDelegate<AsyncDisconnect>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
		public async Task DisconnectAsync(CancellationToken token = default(CancellationToken)) {
			if (m_stream != null && m_stream.IsConnected)
			{
				try
				{
					if (!UngracefullDisconnection)
					{
						await ExecuteAsync("QUIT", token);
					}
				}
				catch (SocketException sockex)
				{
					this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): SocketException caught and discarded while closing control connection: " + sockex.ToString());
				}
				catch (IOException ioex)
				{
					this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): IOException caught and discarded while closing control connection: " + ioex.ToString());
				}
				catch (FtpCommandException cmdex)
				{
					this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): FtpCommandException caught and discarded while closing control connection: " + cmdex.ToString());
				}
				catch (FtpException ftpex)
				{
					this.LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): FtpException caught and discarded while closing control connection: " + ftpex.ToString());
				}
				finally
				{
					m_stream.Close();
				}
			}
		}
#endif

#endregion

#region FTPS

		/// <summary>
		/// Catches the socket stream ssl validation event and fires the event handlers
		/// attached to this object for validating SSL certificates
		/// </summary>
		/// <param name="stream">The stream that fired the event</param>
		/// <param name="e">The event args used to validate the certificate</param>
		void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
			OnValidateCertficate(e);
		}

		/// <summary>
		/// Fires the SSL validation event
		/// </summary>
		/// <param name="e">Event Args</param>
		void OnValidateCertficate(FtpSslValidationEventArgs e) {
			FtpSslValidation evt;

			evt = m_sslvalidate;
			if (evt != null)
				evt(this, e);
		}

#endregion

#region Utils

		/// <summary>
		/// Performs a bitwise and to check if the specified
		/// flag is set on the <see cref="Capabilities"/>  property.
		/// </summary>
		/// <param name="cap">The <see cref="FtpCapability"/> to check for</param>
		/// <returns>True if the feature was found, false otherwise</returns>
		public bool HasFeature(FtpCapability cap) {
			return ((this.Capabilities & cap) == cap);
		}

		/// <summary>
		/// Retrieves the delegate for the specified IAsyncResult and removes
		/// it from the m_asyncmethods collection if the operation is successful
		/// </summary>
		/// <typeparam name="T">Type of delegate to retrieve</typeparam>
		/// <param name="ar">The IAsyncResult to retrieve the delegate for</param>
		/// <returns>The delegate that generated the specified IAsyncResult</returns>
		protected T GetAsyncDelegate<T>(IAsyncResult ar) {
			T func;

			lock (m_asyncmethods) {
				if (m_isDisposed) {
					throw new ObjectDisposedException("This connection object has already been disposed.");
				}

				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult could not be located.");

				if (!(m_asyncmethods[ar] is T)) {
#if CORE
					throw new InvalidCastException("The AsyncResult cannot be matched to the specified delegate. ");
#else
					StackTrace st = new StackTrace(1);

					throw new InvalidCastException("The AsyncResult cannot be matched to the specified delegate. " +
						("Are you sure you meant to call " + st.GetFrame(0).GetMethod().Name + " and not another method?")
					);
#endif
				}

				func = (T)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func;
		}

		/// <summary>
		/// Ensure a relative path is absolute by appending the working dir
		/// </summary>
		private string GetAbsolutePath(string path) {
			if (path == null || path.Trim().Length == 0) {

				// if path not given, then use working dir
				string pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0)
					path = pwd;
				else
					path = "./";

				// FIX : #153 ensure this check works with unix & windows
			} else if (!path.StartsWith("/") && path.Substring(1, 1) != ":") {
				
				// if its a server-specific absolute path then don't add base dir
				if (IsAbsolutePath(path)) {
					return path;
				}

				// if relative path given then add working dir to calc full path
				string pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0) {
					if (path.StartsWith("./"))
						path = path.Remove(0, 2);
					path = (pwd + "/" + path).GetFtpPath();
				}
			}
			return path;
		}

#if ASYNC
        /// <summary>
        /// Ensure a relative path is absolute by appending the working dir
        /// </summary>
        private async Task<string> GetAbsolutePathAsync(string path, CancellationToken token)
        {
            if (path == null || path.Trim().Length == 0)
            {

                // if path not given, then use working dir
                string pwd = await GetWorkingDirectoryAsync(token);
                if (pwd != null && pwd.Trim().Length > 0)
                    path = pwd;
                else
                    path = "./";

            }
            else if (!path.StartsWith("/"))
            {

                // if relative path given then add working dir to calc full path
                string pwd = await GetWorkingDirectoryAsync(token);
                if (pwd != null && pwd.Trim().Length > 0)
                {
                    if (path.StartsWith("./"))
                        path = path.Remove(0, 2);
                    path = (pwd + "/" + path).GetFtpPath();
                }
            }
            return path;
        }
#endif

		private static string DecodeUrl(string url) {
#if CORE
			return WebUtility.UrlDecode(url);
#else
			return HttpUtility.UrlDecode(url);
#endif
		}

		/// <summary>
		/// Disables UTF8 support and changes the Encoding property
		/// back to ASCII. If the server returns an error when trying
		/// to turn UTF8 off a FtpCommandException will be thrown.
		/// </summary>
		public void DisableUTF8() {
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("OPTS UTF8 OFF")).Success) {
					throw new FtpCommandException(reply);
				}

				m_textEncoding = Encoding.ASCII;
				m_textEncodingAutoUTF = false;
#if !CORE14
			}
#endif
		}

		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably
		/// means we've been disconnected. Read and discard
		/// whatever is there and close the connection (optional).
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		private void ReadStaleData(bool closeStream, bool evenEncrypted, bool traceData) {
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (traceData) {
					this.LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
				}
				if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted)) {
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					m_stream.RawSocketRead(buf);
					if (traceData) {
						this.LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + Encoding.GetString(buf).TrimEnd('\r', '\n'));
					}
				}

				if (closeStream) {
					m_stream.Close();
				}
			}
		}

#if ASYNC
		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably
		/// means we've been disconnected. Read and discard
		/// whatever is there and close the connection (optional).
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		/// <param name="token">Cancellation Token</param>

		private async Task ReadStaleDataAsync(bool closeStream, bool evenEncrypted, bool traceData, CancellationToken token)
        {
            if (m_stream != null && m_stream.SocketDataAvailable > 0)
            {
                if (traceData)
                {
                    this.LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
                }
                if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted))
                {
                    byte[] buf = new byte[m_stream.SocketDataAvailable];
                    await m_stream.RawSocketReadAsync(buf, token);
                    if (traceData)
                    {
                        this.LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + Encoding.GetString(buf).TrimEnd('\r', '\n'));
                    }
                }

                if (closeStream)
                {
                    m_stream.Close();
                }
            }
        }
#endif

		/// <summary>
		/// Checks if this FTP/FTPS connection is made through a proxy.
		/// </summary>
		public bool IsProxy() {
			return (this is FtpClientProxy);
		}


#endregion

#region Logging

		/// <summary>
		/// Add a custom listener here to get events every time a message is logged.
		/// </summary>
		public Action<FtpTraceLevel, string> OnLogEvent;

		/// <summary>
		/// Log a function call with relavent arguments
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		public void LogFunc(string function, object[] args = null) {

			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(FtpTraceLevel.Verbose, ">         " + function + "(" + args.ItemsToString().Join(", ") + ")");
			}

			// log to system
			FtpTrace.WriteFunc(function, args);
		}
		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		public void LogLine(FtpTraceLevel eventType, string message) {

			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(eventType, message);
			}

			// log to system
			FtpTrace.WriteLine(eventType, message);
		}

		/// <summary>
		/// Log a message, adding an automatic prefix to the message based on the `eventType`
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		public void LogStatus(FtpTraceLevel eventType, string message) {

			// add prefix
			message = TraceLevelPrefix(eventType) + message;

			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(eventType, message);
			}

			// log to system
			FtpTrace.WriteLine(eventType, message);
		}
		private static string TraceLevelPrefix(FtpTraceLevel level) {
			switch (level) {
				case FtpTraceLevel.Verbose:
					return "Status:   ";
				case FtpTraceLevel.Info:
					return "Status:   ";
				case FtpTraceLevel.Warn:
					return "Warning:  ";
				case FtpTraceLevel.Error:
					return "Error:    ";
			}
			return "Status:   ";
		}
#endregion

	}
}