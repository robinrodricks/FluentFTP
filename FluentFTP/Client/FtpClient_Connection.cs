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
using FluentFTP.Servers;
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
			Host = host ?? throw new ArgumentNullException("Host must be provided");
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials) {
			Host = host ?? throw new ArgumentNullException("Host must be provided");
			Credentials = credentials ?? throw new ArgumentNullException("Credentials must be provided");
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(string host, int port, NetworkCredential credentials) {
			Host = host ?? throw new ArgumentNullException("Host must be provided");
			Port = port;
			Credentials = credentials ?? throw new ArgumentNullException("Credentials must be provided");
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
		/// Creates a new instance of an FTP Client, with the given host.
		/// </summary>
		public FtpClient(Uri host) {
			Host = ValidateHost(host);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, NetworkCredential credentials) {
			Host = ValidateHost(host);
			Credentials = credentials;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, string user, string pass) {
			Host = ValidateHost(host);
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(Uri host, int port, string user, string pass) {
			Host = ValidateHost(host);
			Port = port;
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Check if the host parameter is valid
		/// </summary>
		/// <param name="host"></param>
		private static string ValidateHost(Uri host) {
			if (host == null) {
				throw new ArgumentNullException("Host is required");
			}
#if !CORE
			if (host.Scheme != Uri.UriSchemeFtp) {
				throw new ArgumentException("Host is not a valid FTP path");
			}
#endif
			return host.ToString();
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
				if (IsDisposed) {
					return;
				}

				// Fix: Hard catch and suppress all exceptions during disposing as there are constant issues with this method
				try {
					LogFunc(nameof(Dispose));
					LogStatus(FtpTraceLevel.Verbose, "Disposing FtpClient object...");
				}
				catch (Exception ex) {
				}

				try {
					if (IsConnected) {
						Disconnect();
					}
				}
				catch (Exception ex) {
				}

				if (m_stream != null) {
					try {
						m_stream.Dispose();
					}
					catch (Exception ex) {
					}

					m_stream = null;
				}

				try {
					m_credentials = null;
					m_textEncoding = null;
					m_host = null;
					m_asyncmethods.Clear();
				}
				catch (Exception ex) {
				}

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
			var conn = Create();

			conn.m_isClone = true;

			// configure new connection as clone of self
			conn.InternetProtocolVersions = InternetProtocolVersions;
			conn.SocketPollInterval = SocketPollInterval;
			conn.StaleDataCheck = StaleDataCheck;
			conn.EnableThreadSafeDataConnections = EnableThreadSafeDataConnections;
			conn.NoopInterval = NoopInterval;
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
			conn.m_capabilities = m_capabilities;
			conn.EncryptionMode = EncryptionMode;
			conn.DataConnectionEncryption = DataConnectionEncryption;
			conn.SslProtocols = SslProtocols;
			conn.SslBuffering = SslBuffering;
			conn.TransferChunkSize = TransferChunkSize;
			conn.LocalFileBufferSize = LocalFileBufferSize;
			conn.ListingDataType = ListingDataType;
			conn.ListingParser = ListingParser;
			conn.ListingCulture = ListingCulture;
			conn.TimeOffset = TimeOffset;
			conn.RetryAttempts = RetryAttempts;
			conn.UploadRateLimit = UploadRateLimit;
			conn.DownloadZeroByteFiles = DownloadZeroByteFiles;
			conn.DownloadRateLimit = DownloadRateLimit;
			conn.DownloadDataType = DownloadDataType;
			conn.UploadDataType = UploadDataType;
			conn.ActivePorts = ActivePorts;
			conn.SendHost = SendHost;
			conn.SendHostDomain = SendHostDomain;
			conn.FXPDataType = FXPDataType;
			conn.FXPProgressInterval = FXPProgressInterval;
			conn.ServerHandler = ServerHandler;
			conn.UploadDirectoryDeleteExcluded = UploadDirectoryDeleteExcluded;
			conn.DownloadDirectoryDeleteExcluded = DownloadDirectoryDeleteExcluded;


			// fix for #428: OpenRead with EnableThreadSafeDataConnections always uses ASCII
			conn.CurrentDataType = CurrentDataType;
			conn.ForceSetDataType = true;

#if !CORE
			conn.PlainTextEncryption = PlainTextEncryption;
#endif

			// always accept certificate no matter what because if code execution ever
			// gets here it means the certificate on the control connection object being
			// cloned was already accepted.
			conn.ValidateCertificate += new FtpSslValidation(
				delegate (FtpClient obj, FtpSslValidationEventArgs e) { e.Accept = true; });

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

				LogFunc(nameof(Connect));

				if (IsDisposed) {
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
				}

				if (m_stream == null) {
					m_stream = new FtpSocketStream(m_SslProtocols);
					m_stream.Client = this;
					m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
				}
				else {
					if (IsConnected) {
						Disconnect();
					}
				}

				if (Host == null) {
					throw new FtpException("No host has been specified");
				}

				if (!IsClone) {
					m_capabilities = new List<FtpCapability>();
				}

				ResetStateFlags();

				m_hashAlgorithms = FtpHashAlgorithm.NONE;
				m_stream.ConnectTimeout = m_connectTimeout;
				m_stream.SocketPollInterval = m_socketPollInterval;
				Connect(m_stream);

				m_stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);

#if !NO_SSL
				if (EncryptionMode == FtpEncryptionMode.Implicit) {
					m_stream.ActivateEncryption(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
				}
#endif

				Handshake();
				m_serverType = FtpServerSpecificHandler.DetectFtpServer(this, HandshakeReply);

				if (SendHost) {
					if (!(reply = Execute("HOST " + (SendHostDomain != null ? SendHostDomain : Host))).Success) {
						throw new FtpException("HOST command failed.");
					}
				}

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
				bool assumeCaps = false;
				if (!IsClone && m_checkCapabilities) {
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

				LogStatus(FtpTraceLevel.Info, "Text encoding: " + m_textEncoding.ToString());

				if (m_textEncoding == Encoding.UTF8) {
					// If the server supports UTF8 it should already be enabled and this
					// command should not matter however there are conflicting drafts
					// about this so we'll just execute it to be safe. 
					Execute("OPTS UTF8 ON");
				}

				// Get the system type - Needed to auto-detect file listing parser
				if ((reply = Execute("SYST")).Success) {
					m_systemType = reply.Message;
					m_serverType = FtpServerSpecificHandler.DetectFtpServerBySyst(this);
					m_serverOS = FtpServerSpecificHandler.DetectFtpOSBySyst(this);
				}

				// Set a FTP server handler if a custom handler has not already been set
				if (ServerHandler != null) {
					ServerHandler = FtpServerSpecificHandler.GetServerHandler(m_serverType);
				}

				// Assume the system's capabilities if FEAT command not supported by the server
				if (assumeCaps) {
					FtpServerSpecificHandler.AssumeCapabilities(this, ServerHandler, m_capabilities, ref m_hashAlgorithms);
				}

#if !NO_SSL && !CORE
				if (m_stream.IsEncrypted && PlainTextEncryption) {
					if (!(reply = Execute("CCC")).Success) {
						throw new FtpSecurityNotAvailableException("Failed to disable encryption with CCC command. Perhaps your server does not support it or is not configured to allow it.");
					}
					else {
						// close the SslStream and send close_notify command to server
						m_stream.DeactivateEncryption();

						// read stale data (server's reply?)
						ReadStaleData(false, true, false);
					}
				}
#endif

				// Create the parser even if the auto-OS detection failed
				var autoParser = ServerHandler != null ? ServerHandler.GetParser() : FtpParser.Unix;
				m_listParser.Init(m_serverOS, autoParser);

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
		public virtual async Task ConnectAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			LogFunc(nameof(ConnectAsync));

			if (IsDisposed) {
				throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
			}

			if (m_stream == null) {
				m_stream = new FtpSocketStream(m_SslProtocols);
				m_stream.Client = this;
				m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
			}
			else {
				if (IsConnected) {
					Disconnect();
				}
			}

			if (Host == null) {
				throw new FtpException("No host has been specified");
			}

			if (!IsClone) {
				m_capabilities = new List<FtpCapability>();
			}

			m_hashAlgorithms = FtpHashAlgorithm.NONE;
			m_stream.ConnectTimeout = m_connectTimeout;
			m_stream.SocketPollInterval = m_socketPollInterval;
			await ConnectAsync(m_stream, token);

			m_stream.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, m_keepAlive);

#if !NO_SSL
			if (EncryptionMode == FtpEncryptionMode.Implicit) {
				await m_stream.ActivateEncryptionAsync(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
			}
#endif

			await HandshakeAsync(token);
			m_serverType = FtpServerSpecificHandler.DetectFtpServer(this, HandshakeReply);

			if (SendHost) {
				if (!(reply = await ExecuteAsync("HOST " + (SendHostDomain != null ? SendHostDomain : Host), token)).Success) {
					throw new FtpException("HOST command failed.");
				}
			}

#if !NO_SSL
			if (EncryptionMode == FtpEncryptionMode.Explicit) {
				if (!(reply = await ExecuteAsync("AUTH TLS", token)).Success) {
					throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
				}

				await m_stream.ActivateEncryptionAsync(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
			}
#endif

			if (m_credentials != null) {
				await AuthenticateAsync(token);
			}

			if (m_stream.IsEncrypted && DataConnectionEncryption) {
				if (!(reply = await ExecuteAsync("PBSZ 0", token)).Success) {
					throw new FtpCommandException(reply);
				}

				if (!(reply = await ExecuteAsync("PROT P", token)).Success) {
					throw new FtpCommandException(reply);
				}
			}

			// if this is a clone these values should have already been loaded
			// so save some bandwidth and CPU time and skip executing this again.
			bool assumeCaps = false;
			if (!IsClone && m_checkCapabilities) {
				if ((reply = await ExecuteAsync("FEAT", token)).Success && reply.InfoMessages != null) {
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

			LogStatus(FtpTraceLevel.Info, "Text encoding: " + m_textEncoding.ToString());

			if (m_textEncoding == Encoding.UTF8) {
				// If the server supports UTF8 it should already be enabled and this
				// command should not matter however there are conflicting drafts
				// about this so we'll just execute it to be safe. 
				await ExecuteAsync("OPTS UTF8 ON", token);
			}

			// Get the system type - Needed to auto-detect file listing parser
			if ((reply = await ExecuteAsync("SYST", token)).Success) {
				m_systemType = reply.Message;
				m_serverType = FtpServerSpecificHandler.DetectFtpServerBySyst(this);
				m_serverOS = FtpServerSpecificHandler.DetectFtpOSBySyst(this);
			}

			// Assume the system's capabilities if FEAT command not supported by the server
			if (assumeCaps) {
				FtpServerSpecificHandler.AssumeCapabilities(this, ServerHandler, m_capabilities, ref m_hashAlgorithms);
			}

#if !NO_SSL && !CORE
			if (m_stream.IsEncrypted && PlainTextEncryption) {
				if (!(reply = await ExecuteAsync("CCC", token)).Success) {
					throw new FtpSecurityNotAvailableException("Failed to disable encryption with CCC command. Perhaps your server does not support it or is not configured to allow it.");
				}
				else {
					// close the SslStream and send close_notify command to server
					m_stream.DeactivateEncryption();

					// read stale data (server's reply?)
					await ReadStaleDataAsync(false, true, false, token);
				}
			}
#endif

			// Create the parser after OS auto-detection
			var autoParser = ServerHandler != null ? ServerHandler.GetParser() : FtpParser.Unix;
			m_listParser.Init(m_serverOS, autoParser);
		}
#endif

		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		protected virtual void Connect(FtpSocketStream stream) {
			stream.Connect(Host, Port, InternetProtocolVersions);
		}

#if ASYNC
		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		protected virtual async Task ConnectAsync(FtpSocketStream stream, CancellationToken token) {
			await stream.ConnectAsync(Host, Port, InternetProtocolVersions, token);
		}
#endif

		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		protected virtual void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			stream.Connect(host, port, ipVersions);
		}

#if ASYNC
		/// <summary>
		/// Connect to the FTP server. Overridden in proxy classes.
		/// </summary>
		protected virtual Task ConnectAsync(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions, CancellationToken token) {
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
				}
				else {
					throw new FtpCommandException(reply);
				}
			}

			HandshakeReply = reply;
		}

#if ASYNC
		/// <summary>
		/// Called during <see cref="ConnectAsync()"/>. Typically extended by FTP proxies.
		/// </summary>
		protected virtual async Task HandshakeAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			if (!(reply = await GetReplyAsync(token)).Success) {
				if (reply.Code == null) {
					throw new IOException("The connection was terminated before a greeting could be read.");
				}
				else {
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
			FtpServerSpecificHandler.GetFeatures(this, m_capabilities, ref m_hashAlgorithms, reply.InfoMessages.Split('\n'));
		}

#if !ASYNC
		private delegate void AsyncConnect();

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
				ar = (func = Connect).BeginInvoke(callback, state);
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
		protected virtual async Task AuthenticateAsync(CancellationToken token) {
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
		protected virtual async Task AuthenticateAsync(string userName, string password, CancellationToken token) {
			FtpReply reply;

			if (!(reply = await ExecuteAsync("USER " + userName, token)).Success) {
				throw new FtpAuthenticationException(reply);
			}

			if (reply.Type == FtpResponseType.PositiveIntermediate
				&& !(reply = await ExecuteAsync("PASS " + password, token)).Success) {
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
					}
					catch (Exception ex) {
						LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): Exception caught and discarded while closing control connection: " + ex.ToString());
					}
					finally {
						m_stream.Close();
					}
				}

#if !CORE14
			}

#endif
		}

#if !ASYNC
		private delegate void AsyncDisconnect();

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
				ar = (func = Disconnect).BeginInvoke(callback, state);
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
			if (m_stream != null && m_stream.IsConnected) {
				try {
					if (!UngracefullDisconnection) {
						await ExecuteAsync("QUIT", token);
					}
				}
				catch (Exception ex) {
					LogStatus(FtpTraceLevel.Warn, "FtpClient.Disconnect(): Exception caught and discarded while closing control connection: " + ex.ToString());
				}
				finally {
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
		private void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
			OnValidateCertficate(e);
		}

		/// <summary>
		/// Fires the SSL validation event
		/// </summary>
		/// <param name="e">Event Args</param>
		private void OnValidateCertficate(FtpSslValidationEventArgs e) {

			// automatically validate if ValidateAnyCertificate is set
			if (ValidateAnyCertificate) {
				e.Accept = true;
				return;
			}

			// fallback to manual validation using the ValidateCertificate event
			m_ValidateCertificate?.Invoke(this, e);

		}

		#endregion
	}
}