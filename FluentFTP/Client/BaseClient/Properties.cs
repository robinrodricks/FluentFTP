using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using FluentFTP.Servers;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

	public partial class BaseFtpClient {

		private IFtpLogger m_logger = null;

		/// <summary>
		/// Should the function calls be logged in Verbose mode?
		/// </summary>
		public IFtpLogger Logger {
			get => m_logger;
			set => m_logger = value;
		}

		private Action<FtpTraceLevel, string> m_legacyLogger = null;


		/// <summary>
		/// Add a custom listener here to get events every time a message is logged.
		/// This is the older system, prefer using the ILogger based `Logger` property.
		/// This system will never be removed, you can safely use it in your applications.
		/// </summary>
		public Action<FtpTraceLevel, string> LegacyLogger {
			get => m_legacyLogger;
			set => m_legacyLogger = value;
		}

		private FtpConfig m_config = null;

		/// <summary>
		/// All the configuration settings for this FTP client.
		/// </summary>
		public FtpConfig Config {
			get => m_config;
			set {
				m_config = value;
				m_config.BindTo(this);
			}
		}

		protected FtpBaseServer _serverHandler;

		/// <summary>
		/// Gets the type of the FTP server handler.
		/// This is automatically set based on the detected FTP server, if it is detected. 
		/// You can manually set this property to implement handling for a custom FTP server.
		/// </summary>
		public FtpBaseServer ServerHandler {
			get => _serverHandler;
			set => _serverHandler = value;
		}

		protected Encoding m_textEncoding = Encoding.ASCII;
		protected bool m_textEncodingAutoUTF = true;

		/// <summary>
		/// Gets or sets the text encoding being used when talking with the server. The default
		/// value is <see cref="System.Text.Encoding.ASCII"/> however upon connection, the client checks
		/// for UTF8 support and if it's there this property is switched over to
		/// <see cref="System.Text.Encoding.UTF8"/>. Manually setting this value overrides automatic detection
		/// based on the FEAT list; if you change this value it's always used
		/// regardless of what the server advertises, if anything.
		/// </summary>
		public Encoding Encoding {
			get => m_textEncoding;
			set {
				m_textEncoding = value;
				m_textEncodingAutoUTF = false;
			}
		}

		/// <summary>
		/// When last command was sent (NOOP or other)/>.
		/// </summary>
		protected DateTime LastCommandTimestamp;

		/// <summary>
		/// To help in logging / debugging
		/// </summary>
		protected string LastCommandExecuted;

		protected FtpReply HandshakeReply;

		protected string LastStreamPath;

		protected FtpListParser CurrentListParser;

		/// <summary>
		/// A thread for background tasks
		/// </summary>
		protected Task m_task;

		// Holds the cached resolved address
		protected string m_Address;

		// Sync or Async Ftp Client
		public string ClientType => this.GetType().ToString().Split('.')[1];

		/// <summary>
		/// Current FTP client status flags used for improving performance and caching data.
		/// </summary>
		protected readonly FtpClientState m_status = new FtpClientState();

		/// <summary>
		/// Returns the current FTP client status flags. For advanced use only.
		/// </summary>
		public FtpClientState Status { get => m_status; }

		/// <summary>
		/// Used for internally synchronizing access to this
		/// object from multiple threads in SYNC code
		/// </summary>
		protected SemaphoreSlim m_sema = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Control connection socket stream
		/// </summary>
		protected FtpSocketStream m_stream = null;

		/// <summary>
		/// Gets the base stream for talking to the server via
		/// the control connection.
		/// </summary>
		FtpSocketStream IInternalFtpClient.GetBaseStream() {
			return m_stream;
		}
		void IInternalFtpClient.SetListingParser(FtpParser parser) {
			CurrentListParser.CurrentParser = parser;
			CurrentListParser.ParserConfirmed = false;
		}

		protected bool m_isDisposed = false;

		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed {
			get => m_isDisposed;
			protected set => m_isDisposed = value;
		}

		/// <summary>
		/// Gets the current internet protocol (IPV4 or IPV6) used by the socket connection.
		/// Returns FtpIpVersion.Unknown before connection.
		/// </summary>
		public FtpIpVersion? InternetProtocol {
			get {
				if (m_stream != null && m_stream.LocalEndPoint != null) {
					if (m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {
						return FtpIpVersion.IPv4;
					}
					if (m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6) {
						return FtpIpVersion.IPv6;
					}
				}
				return FtpIpVersion.Unknown;
			}
		}

		/// <summary>
		/// Returns true if the connection to the FTP server is open.
		/// WARNING: Returns true even if our credentials are incorrect but connection to the server is open.
		/// See the IsAuthenticated property if you want to check if we are correctly logged in.
		/// </summary>
		public bool IsConnected {
			get {
				if (m_stream != null) {
					return m_stream.IsConnected;
				}

				return false;
			}
		}

		protected bool m_IsAuthenticated = false;

		/// <summary>
		/// Returns true if the connection to the FTP server is open and if the FTP server accepted our credentials.
		/// </summary>
		public bool IsAuthenticated {
			get {
				if (m_stream != null) {
					return m_stream.IsConnected && m_IsAuthenticated;
				}

				return false;
			}
		}

		protected bool m_isClone = false;

		/// <summary>
		/// Gets a value indicating if this control connection is a clone. This property
		/// is used with data streams to determine if the connection should be closed
		/// when the stream is closed. Servers typically only allow 1 data connection
		/// per control connection. If you try to open multiple data connections this
		/// object will be cloned for 2 or more resulting in N new connections to the
		/// server.
		/// </summary>
		protected bool IsClone {
			get => m_isClone;
		}

		protected string m_host = null;

		/// <summary>
		/// The server to connect to
		/// </summary>
		public string Host {
			get => m_host;
			set {
				// remove unwanted prefix/postfix
				if (value.StartsWith("ftp://")) {
					value = value.Substring(value.IndexOf("ftp://") + "ftp://".Length);
				}

				if (value.EndsWith("/")) {
					value = value.Replace("/", "");
				}

				m_host = value;
			}
		}

		protected int m_port = 0;

		/// <summary>
		/// The port to connect to. If this value is set to 0 (Default) the port used
		/// will be determined by the type of SSL used or if no SSL is to be used it 
		/// will automatically connect to port 21.
		/// </summary>
		public int Port {
			get {
				// automatically determine port
				// when m_port is 0.
				if (m_port == 0) {
					if (Config.EncryptionMode == FtpEncryptionMode.Implicit) {
						return 990;
					}
					else {
						return 21;
					}
				}

				return m_port;
			}
			set => m_port = value;
		}

		protected NetworkCredential m_credentials = new NetworkCredential("anonymous", "anonymous");

		/// <summary>
		/// Credentials used for authentication
		/// </summary>
		public NetworkCredential Credentials {
			get => m_credentials;
			set => m_credentials = value;
		}

		protected List<FtpCapability> m_capabilities = null;

		/// <summary>
		/// Gets the server capabilities represented by an array of capability flags
		/// </summary>
		public List<FtpCapability> Capabilities {
			get {

				// FIX #683: if capabilities are already loaded, don't check if connected and return straight away
				if (m_capabilities != null && m_capabilities.Count > 0) {
					return m_capabilities;
				}

				// FIX #683: while using async operations, it is possible that the stream is not
				// connected, so don't connect using synchronous connection logic
				if (m_stream == null) {
					throw new FtpException("Please call Connect() before trying to read the Capabilities!");
				}

				return m_capabilities;
			}
			protected set => m_capabilities = value;
		}

		protected FtpHashAlgorithm m_hashAlgorithms = FtpHashAlgorithm.NONE;

		/// <summary>
		/// Get the hash types supported by the server for use with the HASH Command.
		/// This is a recent extension to the protocol that is not fully
		/// standardized and is not guaranteed to work. See here for
		/// more details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </summary>
		public FtpHashAlgorithm HashAlgorithms {
			get {

				// FIX #683: if hash types are already loaded, don't check if connected and return straight away
				if (m_hashAlgorithms != FtpHashAlgorithm.NONE) {
					return m_hashAlgorithms;
				}

				// FIX #683: while using async operations, it is possible that the stream is not
				// connected, so don't connect using synchronous connection logic
				if (m_stream == null || !m_stream.IsConnected) {
					throw new FtpException("Please call Connect() before trying to read the HashAlgorithms!");
				}

				return m_hashAlgorithms;
			}
			protected set => m_hashAlgorithms = value;
		}

		/// <summary>
		/// The negotiated SSL/TLS protocol version.
		/// Will return a valid value after connection is complete.
		/// Before connection it will return `SslProtocols.None`.
		/// </summary>
		public SslProtocols SslProtocolActive {
			get { return m_stream != null ? m_stream.SslProtocolActive : SslProtocols.None; }
		}

		/// <summary>
		/// Checks if FTPS/SSL encryption is currently active.
		/// Useful to see if your server supports FTPS, when using FtpEncryptionMode.Auto. 
		/// </summary>
		public bool IsEncrypted {
			get => m_stream != null && m_stream.IsEncrypted;
		}

		protected FtpSslValidation m_ValidateCertificate = null;

		/// <summary>
		/// Easiest way to check if a handler has been attached.
		/// </summary>
		public bool ValidateCertificateHandlerExists {
			get { return m_ValidateCertificate != null; }
		}

		/// <summary>
		/// Event is fired to validate SSL certificates. If this event is
		/// not handled and there are errors validating the certificate
		/// the connection will be aborted.
		/// Not fired if ValidateAnyCertificate is set to true.
		/// </summary>
		public event FtpSslValidation ValidateCertificate {
			add => m_ValidateCertificate += value;
			remove => m_ValidateCertificate -= value;
		}

		protected string m_systemType = "UNKNOWN";

		/// <summary>
		/// Gets the type of system/server that we're connected to. Typically begins with "WINDOWS" or "UNIX".
		/// </summary>
		public string SystemType => m_systemType;

		protected FtpServer m_serverType = FtpServer.Unknown;

		/// <summary>
		/// Gets the type of the FTP server software that we're connected to.
		/// </summary>
		public FtpServer ServerType => m_serverType;

		protected FtpOperatingSystem m_serverOS = FtpOperatingSystem.Unknown;

		/// <summary>
		/// Gets the operating system of the FTP server that we're connected to.
		/// </summary>
		public FtpOperatingSystem ServerOS => m_serverOS;

		protected string m_connectionType = "Default";

		/// <summary> Gets the connection type </summary>
		public string ConnectionType {
			get => m_connectionType;
			protected set => m_connectionType = value;
		}

		/// <summary> Gets the last reply received from the server</summary>
		public FtpReply LastReply {
			get {
				return LastReplies == null ? new FtpReply() : LastReplies[0];
			}
		}

		/// <summary> Gets the last replies received from the server</summary>
		public List<FtpReply> LastReplies { get; set; }

		/// <summary>
		/// Callback format to implement your custom FTP listing line parser.
		/// </summary>
		/// <param name="line">The line from the listing</param>
		/// <param name="capabilities">The server capabilities</param>
		/// <param name="client">The FTP client</param>
		/// <returns>Return an FtpListItem object if the line can be parsed, else return null</returns>
		public delegate FtpListItem CustomParser(string line, List<FtpCapability> capabilities, BaseFtpClient client);

		/// <summary>
		/// Detect if your FTP server supports the recursive LIST command (LIST -R).
		/// If you know for sure that this is supported, return true here.
		/// </summary>
		public bool RecursiveList {
			get {

				// If the user has confirmed support on his server, return true
				if (Status.RecursiveListSupported) {
					return true;
				}

				// ask the server handler if it supports recursive listing
				if (ServerHandler != null && ServerHandler.RecursiveList()) {
					return true;
				}
				return false;

			}
			set {
				// You can always set this property if you are sure about
				// your server's support for recursive listing
				Status.RecursiveListSupported = value;
			}
		}

		/// <summary>
		/// Returns the local end point of the FTP socket, if it is available.
		/// </summary>
		public IPEndPoint SocketLocalEndPoint {
			get => m_stream?.LocalEndPoint;
		}

		/// <summary>
		/// Returns the remote end point of the FTP socket, if it is available.
		/// </summary>
		public IPEndPoint SocketRemoteEndPoint {
			get => m_stream?.RemoteEndPoint;
		}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}
