using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Servers;
using FluentFTP.Helpers;
using System.Net.Sockets;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		/// <summary>
		/// Current FTP client status flags used for improving performance and caching data.
		/// </summary>
		private readonly FtpClientState m_status = new FtpClientState();

		/// <summary>
		/// Returns the current FTP client status flags. For advanced use only.
		/// </summary>
		public FtpClientState Status { get => m_status; }

#if !CORE14
		/// <summary>
		/// Used for internally synchronizing access to this
		/// object from multiple threads
		/// </summary>
		private readonly object m_lock = new object();

		/// <summary>
		/// For usage by FTP proxies only
		/// </summary>
		protected object Lock => m_lock;
#endif

		/// <summary>
		/// A list of asynchronous methods that are in progress
		/// </summary>
		private readonly Dictionary<IAsyncResult, object> m_asyncmethods = new Dictionary<IAsyncResult, object>();

		/// <summary>
		/// Control connection socket stream
		/// </summary>
		private FtpSocketStream m_stream = null;

		private bool m_isDisposed = false;

		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed {
			get => m_isDisposed;
			private set => m_isDisposed = value;
		}

		/// <summary>
		/// Gets the base stream for talking to the server via
		/// the control connection.
		/// </summary>
		protected Stream BaseStream => m_stream;

		private FtpIpVersion m_ipVersions = FtpIpVersion.ANY;

		/// <summary>
		/// Flags specifying which versions of the internet protocol (IPV4 or IPV6) to
		/// support when making a connection. All addresses returned during
		/// name resolution are tried until a successful connection is made.
		/// You can fine tune which versions of the internet protocol to use
		/// by adding or removing flags here. I.e., setting this property
		/// to FtpIpVersion.IPv4 will cause the connection process to
		/// ignore IPv6 addresses. The default value is ANY version.
		/// </summary>
		public FtpIpVersion InternetProtocolVersions {
			get => m_ipVersions;
			set => m_ipVersions = value;
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

		private int m_socketPollInterval = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds
		/// that must pass since the last socket activity
		/// before calling <see cref="System.Net.Sockets.Socket.Poll"/> 
		/// on the socket to test for connectivity. 
		/// Setting this interval too low will
		/// have a negative impact on performance. Setting this
		/// interval to 0 disables Polling all together.
		/// The default value is 15 seconds.
		/// </summary>
		public int SocketPollInterval {
			get => m_socketPollInterval;
			set {
				m_socketPollInterval = value;
				if (m_stream != null) {
					m_stream.SocketPollInterval = value;
				}
			}
		}

		private bool m_staleDataTest = true;

		/// <summary>
		/// Gets or sets a value indicating whether a test should be performed to
		/// see if there is stale (unrequested data) sitting on the socket. In some
		/// cases the control connection may time out but before the server closes
		/// the connection it might send a 4xx response that was unexpected and
		/// can cause synchronization errors with transactions. To avoid this
		/// problem the <see cref="o:Execute"/> method checks to see if there is any data
		/// available on the socket before executing a command. On Azure hosting
		/// platforms this check can cause an exception to be thrown. In order
		/// to work around the exception you can set this property to false
		/// which will skip the test entirely however doing so eliminates the
		/// best effort attempt of detecting such scenarios. See this thread
		/// for more details about the Azure problem:
		/// https://netftp.codeplex.com/discussions/535879
		/// </summary>
		public bool StaleDataCheck {
			get => m_staleDataTest;
			set => m_staleDataTest = value;
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

		private bool m_IsAuthenticated = false;

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

		private bool m_threadSafeDataChannels = false;

		/// <summary>
		/// When this value is set to true (default) the control connection
		/// is cloned and a new connection the server is established for the
		/// data channel operation. This is a thread safe approach to make
		/// asynchronous operations on a single control connection transparent
		/// to the developer.
		/// </summary>
		public bool EnableThreadSafeDataConnections {
			get => m_threadSafeDataChannels;
			set => m_threadSafeDataChannels = value;
		}

		private int m_noopInterval = 0;

		/// <summary>
		/// Gets or sets the length of time in milliseconds after last command
		/// (NOOP or other) that a NOOP command is sent by <see cref="Noop"/>.
		/// This is called during downloading/uploading if
		/// <see cref="EnableThreadSafeDataConnections"/> is false. Setting this
		/// interval to 0 disables <see cref="Noop"/> all together.
		/// The default value is 0 (disabled).
		/// </summary>
		public int NoopInterval {
			get => m_noopInterval;
			set => m_noopInterval = value;
		}

		private bool m_checkCapabilities = true;

		/// <summary>
		/// When this value is set to true (default) the control connection
		/// will set which features are available by executing the FEAT command
		/// when the connect method is called.
		/// </summary>
		public bool CheckCapabilities {
			get => m_checkCapabilities;
			set => m_checkCapabilities = value;
		}

		private bool m_isClone = false;

		/// <summary>
		/// Gets a value indicating if this control connection is a clone. This property
		/// is used with data streams to determine if the connection should be closed
		/// when the stream is closed. Servers typically only allow 1 data connection
		/// per control connection. If you try to open multiple data connections this
		/// object will be cloned for 2 or more resulting in N new connections to the
		/// server.
		/// </summary>
		internal bool IsClone {
			get => m_isClone;
			private set => m_isClone = value;
		}

		private Encoding m_textEncoding = Encoding.ASCII;
		private bool m_textEncodingAutoUTF = true;

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
#if !CORE14
				lock (m_lock) {
#endif
					m_textEncoding = value;
					m_textEncodingAutoUTF = false;
#if !CORE14
				}

#endif
			}
		}

		private string m_host = null;

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

		private int m_port = 0;

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
					if (EncryptionMode == FtpEncryptionMode.Implicit) {
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

		private NetworkCredential m_credentials = new NetworkCredential("anonymous", "anonymous");

		/// <summary>
		/// Credentials used for authentication
		/// </summary>
		public NetworkCredential Credentials {
			get => m_credentials;
			set => m_credentials = value;
		}

		private int m_maxDerefCount = 20;

		/// <summary>
		/// Gets or sets a value that controls the maximum depth
		/// of recursion that <see cref="o:DereferenceLink"/> will follow symbolic
		/// links before giving up. You can also specify the value
		/// to be used as one of the overloaded parameters to the
		/// <see cref="o:DereferenceLink"/> method. The default value is 20. Specifying
		/// -1 here means indefinitely try to resolve a link. This is
		/// not recommended for obvious reasons (stack overflow).
		/// </summary>
		public int MaximumDereferenceCount {
			get => m_maxDerefCount;
			set => m_maxDerefCount = value;
		}

		private X509CertificateCollection m_clientCerts = new X509CertificateCollection();

		/// <summary>
		/// Client certificates to be used in SSL authentication process
		/// </summary>
		public X509CertificateCollection ClientCertificates {
			get => m_clientCerts;
			protected set => m_clientCerts = value;
		}

		// Holds the cached resolved address
		private string m_Address;

		private Func<string> m_AddressResolver;

		/// <summary>
		/// Delegate used for resolving local address, used for active data connections
		/// This can be used in case you're behind a router, but port forwarding is configured to forward the
		/// ports from your router to your internal IP. In that case, we need to send the router's IP instead of our internal IP.
		/// See example: FtpClient.GetPublicIP -> This uses Ipify api to find external IP
		/// </summary>
		public Func<string> AddressResolver {
			get => m_AddressResolver;
			set => m_AddressResolver = value;
		}

		private IEnumerable<int> m_ActivePorts;

		/// <summary>
		/// Ports used for Active Data Connection.
		/// Useful when your FTP server has certain ports that are blocked or used for other purposes.
		/// </summary>
		public IEnumerable<int> ActivePorts {
			get => m_ActivePorts;
			set => m_ActivePorts = value;
		}

		private IEnumerable<int> m_PassiveBlockedPorts;

		/// <summary>
		/// Ports blocked for Passive Data Connection (PASV and EPSV).
		/// Useful when your FTP server has certain ports that are blocked or used for other purposes.
		/// </summary>
		public IEnumerable<int> PassiveBlockedPorts {
			get => m_PassiveBlockedPorts;
			set => m_PassiveBlockedPorts = value;
		}
		
		private int m_PassiveMaxAttempts = 100;

		/// <summary>
		/// Maximum number of passive connections made in order to find a working port for Passive Data Connection (PASV and EPSV).
		/// Only used if PassiveBlockedPorts is non-null.
		/// </summary>
		public int PassiveMaxAttempts {
			get => m_PassiveMaxAttempts;
			set => m_PassiveMaxAttempts = value;
		}

		private FtpDataConnectionType m_dataConnectionType = FtpDataConnectionType.AutoPassive;

		/// <summary>
		/// Data connection type, default is AutoPassive which tries
		/// a connection with EPSV first and if it fails then tries
		/// PASV before giving up. If you know exactly which kind of
		/// connection you need you can slightly increase performance
		/// by defining a specific type of passive or active data
		/// connection here.
		/// </summary>
		public FtpDataConnectionType DataConnectionType {
			get => m_dataConnectionType;
			set => m_dataConnectionType = value;
		}

		private bool m_DisconnectWithQuit = true;

		/// <summary>
		/// Disconnect from the server without sending QUIT. This helps
		/// work around IOExceptions caused by buggy connection resets
		/// when closing the control connection.
		/// </summary>
		public bool DisconnectWithQuit {
			get => m_DisconnectWithQuit;
			set => m_DisconnectWithQuit = value;
		}

		private bool m_DisconnectWithShutdown = false;

		/// <summary>
		/// Before we disconnect from the server, send the Shutdown signal on the socket stream.
		/// </summary>
		public bool DisconnectWithShutdown {
			get => m_DisconnectWithShutdown;
			set => m_DisconnectWithShutdown = value;
		}

		private int m_connectTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds to wait for a connection 
		/// attempt to succeed before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int ConnectTimeout {
			get => m_connectTimeout;
			set => m_connectTimeout = value;
		}

		private int m_readTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time wait in milliseconds for data to be
		/// read from the underlying stream. The default value is 15000 (15 seconds).
		/// </summary>
		public int ReadTimeout {
			get => m_readTimeout;
			set => m_readTimeout = value;
		}

		private int m_dataConnectionConnectTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds for a data connection
		/// to be established before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int DataConnectionConnectTimeout {
			get => m_dataConnectionConnectTimeout;
			set => m_dataConnectionConnectTimeout = value;
		}

		private int m_dataConnectionReadTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds the data channel
		/// should wait for the server to send data. Default value is 
		/// 15000 (15 seconds).
		/// </summary>
		public int DataConnectionReadTimeout {
			get => m_dataConnectionReadTimeout;
			set => m_dataConnectionReadTimeout = value;
		}

		private bool m_keepAlive = false;

		/// <summary>
		/// Gets or sets a value indicating if <see cref="System.Net.Sockets.SocketOptionName.KeepAlive"/> should be set on 
		/// the underlying stream's socket. If the connection is alive, the option is
		/// adjusted in real-time. The value is stored and the KeepAlive option is set
		/// accordingly upon any new connections. The value set here is also applied to
		/// all future data streams. It has no affect on cloned control connections or
		/// data connections already in progress. The default value is false.
		/// </summary>
		public bool SocketKeepAlive {
			get => m_keepAlive;
			set {
				m_keepAlive = value;
				if (m_stream != null) {
					m_stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, value);
				}
			}
		}

		private List<FtpCapability> m_capabilities = null;

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

		private FtpHashAlgorithm m_hashAlgorithms = FtpHashAlgorithm.NONE;

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
			private set => m_hashAlgorithms = value;
		}

		private FtpEncryptionMode m_encryptionmode = FtpEncryptionMode.None;

		/// <summary>
		/// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
		/// </summary>
		public FtpEncryptionMode EncryptionMode {
			get => m_encryptionmode;
			set => m_encryptionmode = value;
		}

		private bool m_dataConnectionEncryption = true;

		/// <summary>
		/// Indicates if data channel transfers should be encrypted. Only valid if <see cref="EncryptionMode"/>
		/// property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// </summary>
		public bool DataConnectionEncryption {
			get => m_dataConnectionEncryption;
			set => m_dataConnectionEncryption = value;
		}

#if !CORE
		private bool m_plainTextEncryption = false;

		/// <summary>
		/// Indicates if the encryption should be disabled immediately after connecting using a CCC command.
		/// This is useful when you have a FTP firewall that requires plaintext FTP, but your server mandates FTPS connections.
		/// </summary>
		public bool PlainTextEncryption {
			get => m_plainTextEncryption;
			set => m_plainTextEncryption = value;
		}
#endif

#if CORE || NET45
		private SslProtocols m_SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#else
		private SslProtocols m_SslProtocols = SslProtocols.Default;
#endif
		/// <summary>
		/// Encryption protocols to use. Only valid if EncryptionMode property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// Default value is .NET Framework defaults from the <see cref="System.Net.Security.SslStream"/> class.
		/// </summary>
		public SslProtocols SslProtocols {
			get => m_SslProtocols;
			set => m_SslProtocols = value;
		}

		private FtpsBuffering m_SslBuffering = FtpsBuffering.Auto;

		/// <summary>
		/// Whether to use SSL Buffering to speed up data transfer during FTP operations.
		/// SSL Buffering is always disabled on .NET 5.0 and later due to platform issues (see issue 682 in FluentFTP issue tracker).
		/// </summary>
		public FtpsBuffering SslBuffering {
			get => m_SslBuffering;
			set => m_SslBuffering = value;
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

		private FtpSslValidation m_ValidateCertificate = null;

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

		private bool m_ValidateAnyCertificate = false;

		/// <summary>
		/// Accept any SSL certificate received from the server and skip performing
		/// the validation using the ValidateCertificate callback.
		/// Useful for Powershell users.
		/// </summary>
		public bool ValidateAnyCertificate {
			get => m_ValidateAnyCertificate;
			set => m_ValidateAnyCertificate = value;
		}

		private bool m_ValidateCertificateRevocation = false;

		/// <summary>
		/// Indicates if the certificate revocation list is checked during authentication.
		/// Useful when you need to maintain the certificate chain validation,
		/// but skip the certificate revocation check.
		/// WARNING: Enabling this can cause memory leaks in some conditions (see issue #710 for details).
		/// </summary>
		public bool ValidateCertificateRevocation {
			get => m_ValidateCertificateRevocation;
			set => m_ValidateCertificateRevocation = value;
		}

		private string m_systemType = "UNKNOWN";

		/// <summary>
		/// Gets the type of system/server that we're connected to. Typically begins with "WINDOWS" or "UNIX".
		/// </summary>
		public string SystemType => m_systemType;

		private FtpServer m_serverType = FtpServer.Unknown;

		/// <summary>
		/// Gets the type of the FTP server software that we're connected to.
		/// </summary>
		public FtpServer ServerType => m_serverType;

		private FtpBaseServer m_serverHandler;

		/// <summary>
		/// Gets the type of the FTP server handler.
		/// This is automatically set based on the detected FTP server, if it is detected. 
		/// You can manually set this property to implement handling for a custom FTP server.
		/// </summary>
		public FtpBaseServer ServerHandler {
			get => m_serverHandler;
			set => m_serverHandler = value;
		}

		private FtpOperatingSystem m_serverOS = FtpOperatingSystem.Unknown;

		/// <summary>
		/// Gets the operating system of the FTP server that we're connected to.
		/// </summary>
		public FtpOperatingSystem ServerOS => m_serverOS;

		private string m_connectionType = "Default";

		/// <summary> Gets the connection type </summary>
		public string ConnectionType {
			get => m_connectionType;
			protected set => m_connectionType = value;
		}

		private FtpReply m_lastReply;

		/// <summary> Gets the last reply received from the server</summary>
		public FtpReply LastReply {
			get => m_lastReply;
			protected set => m_lastReply = value;
		}


		private FtpDataType m_ListingDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the file listings are downloaded in Binary or ASCII mode.
		/// </summary>
		public FtpDataType ListingDataType {
			get => m_ListingDataType;
			set => m_ListingDataType = value;
		}

		private FtpParser m_parser = FtpParser.Auto;

		/// <summary>
		/// File listing parser to be used. 
		/// Automatically calculated based on the type of the server at the time of connection.
		/// If you want to override this property, make sure to do it after calling Connect.
		/// </summary>
		public FtpParser ListingParser {
			get => m_parser;
			set {
				m_parser = value;

				// configure parser
				m_listParser.CurrentParser = value;
				m_listParser.ParserConfirmed = false;
			}
		}

		private CultureInfo m_parserCulture = CultureInfo.InvariantCulture;

		/// <summary>
		/// Culture used to parse file listings
		/// </summary>
		public CultureInfo ListingCulture {
			get => m_parserCulture;
			set => m_parserCulture = value;
		}

		private CustomParser m_customParser = null;

		/// <summary>
		/// Custom file listing parser to be used.
		/// </summary>
		public CustomParser ListingCustomParser {
			get => m_customParser;
			set {
				m_customParser = value;

				// modify the ListingParser to note that a custom parser is set
				if (value != null) {
					ListingParser = FtpParser.Custom;
				}
				else {
					ListingParser = FtpParser.Auto;
				}
			}
		}

		/// <summary>
		/// Callback format to implement your custom FTP listing line parser.
		/// </summary>
		/// <param name="line">The line from the listing</param>
		/// <param name="capabilities">The server capabilities</param>
		/// <param name="client">The FTP client</param>
		/// <returns>Return an FtpListItem object if the line can be parsed, else return null</returns>
		public delegate FtpListItem CustomParser(string line, List<FtpCapability> capabilities, FtpClient client);

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


		private double m_serverTimeZone = 0;
		private TimeSpan m_serverTimeOffset = new TimeSpan();

		/// <summary>
		/// The timezone of the FTP server. If the server is in Tokyo with UTC+9 then set this to 9.
		/// If the server returns timestamps in UTC then keep this 0.
		/// </summary>
		public double TimeZone {
			get => m_serverTimeZone;
			set {
				if (value < -14 || value > 14) {
					throw new ArgumentOutOfRangeException(nameof(value), "TimeZone must be within -14 to +14 to represent UTC-14 to UTC+14");
				}
				m_serverTimeZone = value;

				// configure parser
				if (value == 0) {
					m_serverTimeOffset = TimeSpan.Zero;
				}
				else {
					var hours = (int)Math.Floor(m_serverTimeZone);
					var mins = (int)Math.Floor((m_serverTimeZone - Math.Floor(m_serverTimeZone)) * 60);
					m_serverTimeOffset = new TimeSpan(hours, mins, 0);
				}
			}
		}


#if CORE
		private double m_localTimeZone = 0;
		private TimeSpan m_localTimeOffset = new TimeSpan();

		/// <summary>
		/// The timezone of your machine. If your machine is in Tokyo with UTC+9 then set this to 9.
		/// If your machine is synchronized with UTC then keep this 0.
		/// </summary>
		public double LocalTimeZone {
			get => m_localTimeZone;
			set {
				if (value < -14 || value > 14) {
					throw new ArgumentOutOfRangeException(nameof(value), "LocalTimeZone must be within -14 to +14 to represent UTC-14 to UTC+14");
				}
				m_localTimeZone = value;

				// configure parser
				if (value == 0) {
					m_localTimeOffset = TimeSpan.Zero;
				}
				else {
					var hours = (int)Math.Floor(m_localTimeZone);
					var mins = (int)Math.Floor((m_localTimeZone - Math.Floor(m_localTimeZone)) * 60);
					m_localTimeOffset = new TimeSpan(hours, mins, 0);
				}
			}
		}
#endif

		private FtpDate m_timeConversion = FtpDate.ServerTime;

		/// <summary>
		/// Server timestamps are converted into the given timezone.
		/// ServerTime will return the original timestamp.
		/// LocalTime will convert the timestamp into your local machine's timezone.
		/// UTC will convert the timestamp into UTC format (GMT+0).
		/// You need to set TimeZone and LocalTimeZone (.NET core only) for these to work.
		/// </summary>
		public FtpDate TimeConversion {
			get => m_timeConversion;
			set {
				m_timeConversion = value;
			}
		}

		private bool m_bulkListing = true;

		/// <summary>
		/// If true, increases performance of GetListing by reading multiple lines
		/// of the file listing at once. If false then GetListing will read file
		/// listings line-by-line. If GetListing is having issues with your server,
		/// set it to false.
		/// 
		/// The number of bytes read is based upon <see cref="BulkListingLength"/>.
		/// </summary>
		public bool BulkListing {
			get => m_bulkListing;
			set => m_bulkListing = value;
		}

		private int m_bulkListingLength = 128;

		/// <summary>
		/// Bytes to read during GetListing. Only honored if <see cref="BulkListing"/> is true.
		/// </summary>
		public int BulkListingLength {
			get => m_bulkListingLength;
			set => m_bulkListingLength = value;
		}

		private int? m_transferChunkSize;

		/// <summary>
		/// Gets or sets the number of bytes transferred in a single chunk (a single FTP command).
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// to transfer large files in multiple chunks.
		/// </summary>
		public int TransferChunkSize {
			get => m_transferChunkSize ?? 65536;
			set => m_transferChunkSize = value;
		}

		private int? m_localFileBufferSize;

		/// <summary>
		/// Gets or sets the size of the file buffer when reading and writing files on the local file system.
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// and all the other file and directory transfer methods.
		/// </summary>
		public int LocalFileBufferSize {
			get => m_localFileBufferSize ?? 4096;
			set => m_localFileBufferSize = value;
		}

		private int m_quickTransferSize = (10 * 1024 * 1024);

		/// <summary>
		/// Files within this size are read and written in a single call to the disk, thereby greatly increasing transfer performance. Measured in bytes.
		/// Reduce this if you notice large memory consumption by FluentFTP. Set this to 0 to disable quick transfer.
		/// </summary>
		internal int QuickTransferLimit {
			get => m_quickTransferSize;
			set => m_quickTransferSize = value;
		}

		private FtpDataType CurrentDataType;

		private int m_retryAttempts = 3;

		/// <summary>
		/// Gets or sets the retry attempts allowed when a verification failure occurs during download or upload.
		/// This value must be set to 1 or more.
		/// </summary>
		public int RetryAttempts {
			get => m_retryAttempts;
			set => m_retryAttempts = value > 0 ? value : 1;
		}

		private uint m_uploadRateLimit = 0;

		/// <summary>
		/// Rate limit for uploads in kbyte/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint UploadRateLimit {
			get => m_uploadRateLimit;
			set => m_uploadRateLimit = value;
		}

		private uint m_downloadRateLimit = 0;

		/// <summary>
		/// Rate limit for downloads in kbytes/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint DownloadRateLimit {
			get => m_downloadRateLimit;
			set => m_downloadRateLimit = value;
		}

		private bool m_DownloadZeroByteFiles = true;

		/// <summary>
		/// Controls if zero-byte files should be downloaded or skipped.
		/// If false, then no file is created/overwritten into the filesystem.
		/// </summary>
		public bool DownloadZeroByteFiles {
			get => m_DownloadZeroByteFiles;
			set => m_DownloadZeroByteFiles = value;
		}

		private FtpDataType m_UploadDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the high-level API uploads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType UploadDataType {
			get => m_UploadDataType;
			set => m_UploadDataType = value;
		}

		private FtpDataType m_DownloadDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the high-level API downloads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType DownloadDataType {
			get => m_DownloadDataType;
			set => m_DownloadDataType = value;
		}

		private bool m_UploadDirectoryDeleteExcluded = true;

		/// <summary>
		/// Controls if the UploadDirectory API deletes the excluded files when uploading in Mirror mode.
		/// If true, then any files that are excluded will be deleted from the FTP server if they are
		/// excluded from the local system. This is done to keep the server in sync with the local system.
		/// But if it is false, the excluded files are not touched on the server, and simply ignored.
		/// </summary>
		public bool UploadDirectoryDeleteExcluded {
			get => m_UploadDirectoryDeleteExcluded;
			set => m_UploadDirectoryDeleteExcluded = value;
		}

		private bool m_DownloadDirectoryDeleteExcluded = true;

		/// <summary>
		/// Controls if the DownloadDirectory API deletes the excluded files when downloading in Mirror mode.
		/// If true, then any files that are excluded will be deleted from the local filesystem if they are
		/// excluded from the FTP server. This is done to keep the local filesystem in sync with the FTP server.
		/// But if it is false, the excluded files are not touched on the local filesystem, and simply ignored.
		/// </summary>
		public bool DownloadDirectoryDeleteExcluded {
			get => m_DownloadDirectoryDeleteExcluded;
			set => m_DownloadDirectoryDeleteExcluded = value;
		}

		private FtpDataType m_FXPDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the FXP server-to-server file transfer API uses Binary or ASCII mode.
		/// </summary>
		public FtpDataType FXPDataType {
			get => m_FXPDataType;
			set => m_FXPDataType = value;
		}

		private int m_FXPProgressInterval = 1000;

		/// <summary>
		/// Controls how often the progress reports are sent during an FXP file transfer.
		/// The default value is 1000 (1 second).
		/// </summary>
		public int FXPProgressInterval {
			get => m_FXPProgressInterval;
			set => m_FXPProgressInterval = value;
		}

		private bool m_SendHost;
		/// <summary>
		/// Controls if the HOST command is sent immediately after the handshake.
		/// Useful when you are using shared hosting and you need to inform the
		/// FTP server which domain you want to connect to.
		/// </summary>
		public bool SendHost {
			get => m_SendHost;
			set => m_SendHost = value;
		}

		private string m_SendHostDomain = null;
		/// <summary>
		/// Controls which domain is sent with the HOST command.
		/// If this is null, then the Host parameter of the FTP client is sent.
		/// </summary>
		public string SendHostDomain {
			get => m_SendHostDomain; 
			set => m_SendHostDomain = value;
		}

#if ASYNC && !CORE14 && !CORE16
		private IPAddress m_SocketLocalIp;
		/// <summary>
		/// The local socket will be bound to the given local IP/interface.
		/// This is useful if you have several usable public IP addresses and want to use a particular one.
		/// </summary>
		public IPAddress SocketLocalIp
		{
			get => m_SocketLocalIp;
			set => m_SocketLocalIp = value;
		}
#endif

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

		private FtpZOSListRealm m_zOSListingRealm;

		/// <summary>
		/// During and after a z/OS GetListing(), this value shows the
		/// z/OS filesystem realm that was encountered.
		/// </summary>
		public FtpZOSListRealm zOSListingRealm
		{
			get => m_zOSListingRealm;
			set => m_zOSListingRealm = value;
		}

		private ushort m_zOSListingLRECL;

		/// <summary>
		/// During and after a z/OS GetListing(), this value shows the
		/// the LRECL that was encountered (for a realm = Member only).
		/// The value is used internally to calculate member sizes
		/// </summary>
		public ushort zOSListingLRECL
		{
			get => m_zOSListingLRECL;
			set => m_zOSListingLRECL = value;
		}

		// ADD PROPERTIES THAT NEED TO BE CLONED INTO
		// FtpClient.CloneConnection()
	}
}
