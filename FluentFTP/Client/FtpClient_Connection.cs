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
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Client.Modules;
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
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials) {
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials), "Credentials must be provided");
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(string host, int port, NetworkCredential credentials) {
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			Port = port;
			Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials), "Credentials must be provided");
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
		/// Creates a new instance of an FTP Client, with the given host, username, password and account
		/// </summary>
		public FtpClient(string host, string user, string pass, string account) {
			Host = host;
			Credentials = new NetworkCredential(user, pass, account);
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
		/// Creates a new instance of an FTP Client, with the given host, port, username, password and account
		/// </summary>
		public FtpClient(string host, int port, string user, string pass, string account) {
			Host = host;
			Port = port;
			Credentials = new NetworkCredential(user, pass, account);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host.
		/// </summary>
		public FtpClient(Uri host) {
			Host = ValidateHost(host);
			Port = host.Port;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, NetworkCredential credentials) {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = credentials;
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, string user, string pass) {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, string user, string pass, string account) {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = new NetworkCredential(user, pass, account);
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
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(Uri host, int port, string user, string pass, string account) {
			Host = ValidateHost(host);
			Port = port;
			Credentials = new NetworkCredential(user, pass, account);
			m_listParser = new FtpListParser(this);
		}

		/// <summary>
		/// Check if the host parameter is valid
		/// </summary>
		/// <param name="host"></param>
		private static string ValidateHost(Uri host) {
			if (host == null) {
				throw new ArgumentNullException(nameof(host), "Host is required");
			}
#if !CORE
			if (host.Scheme != Uri.UriSchemeFtp) {
				throw new ArgumentException("Host is not a valid FTP path");
			}
#endif
			return host.Host;
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
		/// Clones the FTP client control connection. Used for opening multiple data streams.
		/// You will need to manually connect after cloning.
		/// </summary>
		/// <returns>A new FTP client connection with the same property settings as this one.</returns>
		public FtpClient Clone() {
			var write = Create();

			write.m_isClone = true;

			CloneModule.Clone(this, write);

			// fix for #428: OpenRead with EnableThreadSafeDataConnections always uses ASCII
			write.CurrentDataType = CurrentDataType;
			write.ForceSetDataType = true;

			return write;
		}


		#endregion

		#region Connect

		private FtpListParser m_listParser;

		/// <summary>
		/// Connect to the server
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
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
					m_stream = new FtpSocketStream(this);
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

				if (m_capabilities == null) {
					m_capabilities = new List<FtpCapability>();
				}

				Status.Reset();

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
				m_serverType = ServerModule.DetectFtpServer(this, HandshakeReply);

				if (SendHost) {
					if (!(reply = Execute("HOST " + (SendHostDomain != null ? SendHostDomain : Host))).Success) {
						throw new FtpException("HOST command failed.");
					}
				}

#if !NO_SSL
				// try to upgrade this connection to SSL if supported by the server
				if (EncryptionMode == FtpEncryptionMode.Explicit || EncryptionMode == FtpEncryptionMode.Auto) {
					reply = Execute("AUTH TLS");
					if (!reply.Success){
						Status.ConnectionFTPSFailure = true;
						if (EncryptionMode == FtpEncryptionMode.Explicit) {
							throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
						}
					}
					else if (reply.Success) {
						m_stream.ActivateEncryption(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
					}
				}
#endif

				if (m_credentials != null) {
					Authenticate();
				}

				// configure the default FTPS settings
				if (IsEncrypted && DataConnectionEncryption) {
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
				if (!m_isClone && m_checkCapabilities) {
					m_capabilities.Clear();
				}
				bool assumeCaps = false;
				if (m_capabilities.IsBlank() && m_checkCapabilities) {
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

#if !NO_SSL && !CORE
				if (IsEncrypted && PlainTextEncryption) {
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

				// Unless a custom list parser has been set,
				// Detect the listing parser and prefer machine listings over any other type
				// FIX : #739 prefer using machine listings to fix issues with GetListing and DeleteDirectory
				if (ListingParser != FtpParser.Custom) {
					ListingParser = ServerHandler != null ? ServerHandler.GetParser() : FtpParser.Auto;
					if (HasFeature(FtpCapability.MLSD)) {
						ListingParser = FtpParser.Machine;
					}
				}

				// Create the parser even if the auto-OS detection failed
				m_listParser.Init(m_serverOS, ListingParser);

				// FIX #318 always set the type when we create a new connection
				ForceSetDataType = true;

				// Execute server-specific post-connection event
				if (ServerHandler != null) {
					ServerHandler.AfterConnected(this);
				}

				// FIX #922: disable checking for stale data during connection
				Status.AllowCheckStaleData = true;

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
		public virtual async Task ConnectAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			LogFunc(nameof(ConnectAsync));

			if (IsDisposed) {
				throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
			}

			if (m_stream == null) {
				m_stream = new FtpSocketStream(this);
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

			if (m_capabilities == null) {
				m_capabilities = new List<FtpCapability>();
			}

			Status.Reset();

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
			m_serverType = ServerModule.DetectFtpServer(this, HandshakeReply);

			if (SendHost) {
				if (!(reply = await ExecuteAsync("HOST " + (SendHostDomain != null ? SendHostDomain : Host), token)).Success) {
					throw new FtpException("HOST command failed.");
				}
			}

#if !NO_SSL
			// try to upgrade this connection to SSL if supported by the server
			if (EncryptionMode == FtpEncryptionMode.Explicit || EncryptionMode == FtpEncryptionMode.Auto) {
				reply = await ExecuteAsync("AUTH TLS", token);
				if (!reply.Success) {
					Status.ConnectionFTPSFailure = true;
					if (EncryptionMode == FtpEncryptionMode.Explicit) {
						throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
					}
				}
				else if (reply.Success) {
					await m_stream.ActivateEncryptionAsync(Host, m_clientCerts.Count > 0 ? m_clientCerts : null, m_SslProtocols);
				}
			}
#endif


			if (m_credentials != null) {
				await AuthenticateAsync(token);
			}

			// configure the default FTPS settings
			if (IsEncrypted && DataConnectionEncryption) {
				if (!(reply = await ExecuteAsync("PBSZ 0", token)).Success) {
					throw new FtpCommandException(reply);
				}

				if (!(reply = await ExecuteAsync("PROT P", token)).Success) {
					throw new FtpCommandException(reply);
				}
			}

			// if this is a clone these values should have already been loaded
			// so save some bandwidth and CPU time and skip executing this again.
			// otherwise clear the capabilities in case connection is reused to 
			// a different server 
			if (!m_isClone && m_checkCapabilities) {
				m_capabilities.Clear();
			}
			bool assumeCaps = false;
			if (m_capabilities.IsBlank() && m_checkCapabilities) {
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
				if ((reply = await ExecuteAsync("OPTS UTF8 ON", token)).Success) {
					Status.ConnectionUTF8Success = true;
				}
			}

			// Get the system type - Needed to auto-detect file listing parser
			if ((reply = await ExecuteAsync("SYST", token)).Success) {
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

#if !NO_SSL && !CORE
			if (IsEncrypted && PlainTextEncryption) {
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

			// Unless a custom list parser has been set,
			// Detect the listing parser and prefer machine listings over any other type
			// FIX : #739 prefer using machine listings to fix issues with GetListing and DeleteDirectory
			if (ListingParser != FtpParser.Custom) {
				ListingParser = ServerHandler != null ? ServerHandler.GetParser() : FtpParser.Auto;
				if (HasFeature(FtpCapability.MLSD)) {
					ListingParser = FtpParser.Machine;
				}
			}

			// Create the parser even if the auto-OS detection failed
			m_listParser.Init(m_serverOS, ListingParser);

			// FIX : #318 always set the type when we create a new connection
			ForceSetDataType = true;

			// Execute server-specific post-connection event
			if (ServerHandler != null) {
				await ServerHandler.AfterConnectedAsync(this, token);
			}

			// FIX #922: disable checking for stale data during connection
			Status.AllowCheckStaleData = true;
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
			ServerFeatureModule.Detect(m_capabilities, ref m_hashAlgorithms, reply.InfoMessages.Split('\n'));
		}

#if !ASYNC
		private delegate void AsyncConnect();

		/// <summary>
		/// Initiates a connection to the server
		/// </summary>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
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
			Authenticate(Credentials.UserName, Credentials.Password, Credentials.Domain);
		}

#if ASYNC
		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual async Task AuthenticateAsync(CancellationToken token) {
			await AuthenticateAsync(Credentials.UserName, Credentials.Password, Credentials.Domain, token);
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
		protected virtual void Authenticate(string userName, string password, string account) {

			// mark that we are not authenticated
			m_IsAuthenticated = false;

			// send the USER command along with the FTP username
			FtpReply reply = Execute("USER " + userName);

			// check the reply to the USER command
			if (!reply.Success) {
				throw new FtpAuthenticationException(reply);
			}

			// if it was accepted
			else if (reply.Type == FtpResponseType.PositiveIntermediate) {

				// send the PASS command along with the FTP password
				reply = Execute("PASS " + password);

				// fix for #620: some servers send multiple responses that must be read and decoded,
				// otherwise the connection is aborted and remade and it goes into an infinite loop
				var staleData = ReadStaleData(false, true, true);
				if (staleData != null) {
					var staleReply = new FtpReply();
					if (DecodeStringToReply(staleData, ref staleReply) && !staleReply.Success) {
						throw new FtpAuthenticationException(staleReply);
					}
				}

				// check the first reply to the PASS command
				if (!reply.Success) {
					throw new FtpAuthenticationException(reply);
				}

				// only possible 3** here is `332 Need account for login`
				if (reply.Type == FtpResponseType.PositiveIntermediate) {
					reply = Execute("ACCT " + account);

					if (!reply.Success) {
						throw new FtpAuthenticationException(reply);
					}
				}

				// mark that we are authenticated
				m_IsAuthenticated = true;

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
		protected virtual async Task AuthenticateAsync(string userName, string password, string account, CancellationToken token) {
			
			// send the USER command along with the FTP username
			FtpReply reply = await ExecuteAsync("USER " + userName, token);

			// check the reply to the USER command
			if (!reply.Success) {
				throw new FtpAuthenticationException(reply);
			}

			// if it was accepted
			else if (reply.Type == FtpResponseType.PositiveIntermediate) {

				// send the PASS command along with the FTP password
				reply = await ExecuteAsync("PASS " + password, token);

				// fix for #620: some servers send multiple responses that must be read and decoded,
				// otherwise the connection is aborted and remade and it goes into an infinite loop
				var staleData = await ReadStaleDataAsync(false, true, true, token);
				if (staleData != null) {
					var staleReply = new FtpReply();
					if (DecodeStringToReply(staleData, ref staleReply) && !staleReply.Success) {
						throw new FtpAuthenticationException(staleReply);
					}
				}

				// check the first reply to the PASS command
				if (!reply.Success) {
					throw new FtpAuthenticationException(reply);
				}

				// only possible 3** here is `332 Need account for login`
				if (reply.Type == FtpResponseType.PositiveIntermediate) {
					reply = await ExecuteAsync("ACCT " + account, token);

					if (!reply.Success) {
						throw new FtpAuthenticationException(reply);
					}
					else
					{
						m_IsAuthenticated = true;
					}
				}
				else if (reply.Type == FtpResponseType.PositiveCompletion)
				{
					m_IsAuthenticated = true;
				}
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
						if (DisconnectWithQuit) {
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
					if (DisconnectWithQuit) {
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