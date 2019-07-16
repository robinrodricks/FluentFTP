using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;

namespace FluentFTP {
	
	public partial class FtpClient : IDisposable {
		
#if !CORE14
		/// <summary>
		/// Used for internally synchronizing access to this
		/// object from multiple threads
		/// </summary>
		readonly Object m_lock = new Object();

		/// <summary>
		/// For usage by FTP proxies only
		/// </summary>
		protected Object Lock {
			get {
				return m_lock;
			}
		}
#endif

		/// <summary>
		/// A list of asynchronous methods that are in progress
		/// </summary>
		readonly Dictionary<IAsyncResult, object> m_asyncmethods = new Dictionary<IAsyncResult, object>();

		/// <summary>
		/// Control connection socket stream
		/// </summary>
		FtpSocketStream m_stream = null;

		bool m_isDisposed = false;
		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed {
			get {
				return m_isDisposed;
			}
			private set {
				m_isDisposed = value;
			}
		}

		/// <summary>
		/// Gets the base stream for talking to the server via
		/// the control connection.
		/// </summary>
		protected Stream BaseStream {
			get {
				return m_stream;
			}
		}

		FtpIpVersion m_ipVersions = FtpIpVersion.ANY;
		/// <summary>
		/// Flags specifying which versions of the internet protocol to
		/// support when making a connection. All addresses returned during
		/// name resolution are tried until a successful connection is made.
		/// You can fine tune which versions of the internet protocol to use
		/// by adding or removing flags here. I.e., setting this property
		/// to FtpIpVersion.IPv4 will cause the connection process to
		/// ignore IPv6 addresses. The default value is ANY version.
		/// </summary>
		public FtpIpVersion InternetProtocolVersions {
			get {
				return m_ipVersions;
			}
			set {
				m_ipVersions = value;
			}
		}

		int m_socketPollInterval = 15000;
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
			get { return m_socketPollInterval; }
			set {
				m_socketPollInterval = value;
				if (m_stream != null)
					m_stream.SocketPollInterval = value;
			}
		}

		bool m_staleDataTest = true;
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
			get { return m_staleDataTest; }
			set { m_staleDataTest = value; }
		}

		/// <summary>
		/// Gets a value indicating if the connection is alive
		/// </summary>
		public bool IsConnected {
			get {
				if (m_stream != null)
					return m_stream.IsConnected;
				return false;
			}
		}

		bool m_threadSafeDataChannels = false;
		/// <summary>
		/// When this value is set to true (default) the control connection
		/// is cloned and a new connection the server is established for the
		/// data channel operation. This is a thread safe approach to make
		/// asynchronous operations on a single control connection transparent
		/// to the developer.
		/// </summary>
		public bool EnableThreadSafeDataConnections {
			get {
				return m_threadSafeDataChannels;
			}
			set {
				m_threadSafeDataChannels = value;
			}
		}

		bool m_checkCapabilities = true;
		/// <summary>
		/// When this value is set to true (default) the control connection
		/// will set which features are avaiable by executing the FEAT command
		/// when the connect method is called.
		/// </summary>
		public bool CheckCapabilities {
			get {
				return m_checkCapabilities;
			}
			set {
				m_checkCapabilities = value;
			}
		}

		bool m_isClone = false;
		/// <summary>
		/// Gets a value indicating if this control connection is a clone. This property
		/// is used with data streams to determine if the connection should be closed
		/// when the stream is closed. Servers typically only allow 1 data connection
		/// per control connection. If you try to open multiple data connections this
		/// object will be cloned for 2 or more resulting in N new connections to the
		/// server.
		/// </summary>
		internal bool IsClone {
			get {
				return m_isClone;
			}
			private set {
				m_isClone = value;
			}
		}

		Encoding m_textEncoding = Encoding.ASCII;
		bool m_textEncodingAutoUTF = true;
		/// <summary>
		/// Gets or sets the text encoding being used when talking with the server. The default
		/// value is <see cref="System.Text.Encoding.ASCII"/> however upon connection, the client checks
		/// for UTF8 support and if it's there this property is switched over to
		/// <see cref="System.Text.Encoding.UTF8"/>. Manually setting this value overrides automatic detection
		/// based on the FEAT list; if you change this value it's always used
		/// regardless of what the server advertises, if anything.
		/// </summary>
		public Encoding Encoding {
			get {
				return m_textEncoding;
			}
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

		string m_host = null;
		/// <summary>
		/// The server to connect to
		/// </summary>
		public string Host {
			get {
				return m_host;
			}
			set {

				// remove unwanted prefix/postfix
				if (value.StartsWith("ftp://")) {
					value = value.Substring(value.IndexOf("ftp://") + ("ftp://").Length);
				}
				if (value.EndsWith("/")) {
					value = value.Replace("/", "");
				}

				m_host = value;
			}
		}

		int m_port = 0;
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
					switch (EncryptionMode) {
						case FtpEncryptionMode.None:
						case FtpEncryptionMode.Explicit:
							return 21;
						case FtpEncryptionMode.Implicit:
							return 990;
					}
				}

				return m_port;
			}
			set {
				m_port = value;
			}
		}

		NetworkCredential m_credentials = new NetworkCredential("anonymous", "anonymous");
		/// <summary>
		/// Credentials used for authentication
		/// </summary>
		public NetworkCredential Credentials {
			get {
				return m_credentials;
			}
			set {
				m_credentials = value;
			}
		}

		int m_maxDerefCount = 20;
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
			get {
				return m_maxDerefCount;
			}
			set {
				m_maxDerefCount = value;
			}
		}

		X509CertificateCollection m_clientCerts = new X509CertificateCollection();
		/// <summary>
		/// Client certificates to be used in SSL authentication process
		/// </summary>
		public X509CertificateCollection ClientCertificates {
			get {
				return m_clientCerts;
			}
			protected set {
				m_clientCerts = value;
			}
		}

		// Holds the cached resolved address
		string m_Address;

		Func<string> m_AddressResolver;

		/// <summary>
		/// Delegate used for resolving local address, used for active data connections
		/// This can be used in case you're behind a router, but port forwarding is configured to forward the
		/// ports from your router to your internal IP. In that case, we need to send the router's IP instead of our internal IP.
		/// See example: FtpClient.GetPublicIP -> This uses Ipify api to find external IP
		/// </summary>
		public Func<string> AddressResolver {
			get { return m_AddressResolver; }
			set { m_AddressResolver = value; }
		}

		IEnumerable<int> m_ActivePorts;

		/// <summary>
		/// Ports used for Active Data Connection
		/// </summary>
		public IEnumerable<int> ActivePorts {
			get { return m_ActivePorts; }
			set { m_ActivePorts = value; }
		}

		FtpDataConnectionType m_dataConnectionType = FtpDataConnectionType.AutoPassive;
		/// <summary>
		/// Data connection type, default is AutoPassive which tries
		/// a connection with EPSV first and if it fails then tries
		/// PASV before giving up. If you know exactly which kind of
		/// connection you need you can slightly increase performance
		/// by defining a specific type of passive or active data
		/// connection here.
		/// </summary>
		public FtpDataConnectionType DataConnectionType {
			get {
				return m_dataConnectionType;
			}
			set {
				m_dataConnectionType = value;
			}
		}

		bool m_ungracefullDisconnect = false;
		/// <summary>
		/// Disconnect from the server without sending QUIT. This helps
		/// work around IOExceptions caused by buggy connection resets
		/// when closing the control connection.
		/// </summary>
		public bool UngracefullDisconnection {
			get {
				return m_ungracefullDisconnect;
			}
			set {
				m_ungracefullDisconnect = value;
			}
		}

		int m_connectTimeout = 15000;
		/// <summary>
		/// Gets or sets the length of time in milliseconds to wait for a connection 
		/// attempt to succeed before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int ConnectTimeout {
			get {
				return m_connectTimeout;
			}
			set {
				m_connectTimeout = value;
			}
		}

		int m_readTimeout = 15000;
		/// <summary>
		/// Gets or sets the length of time wait in milliseconds for data to be
		/// read from the underlying stream. The default value is 15000 (15 seconds).
		/// </summary>
		public int ReadTimeout {
			get {
				return m_readTimeout;
			}
			set {
				m_readTimeout = value;
			}
		}

		int m_dataConnectionConnectTimeout = 15000;
		/// <summary>
		/// Gets or sets the length of time in milliseconds for a data connection
		/// to be established before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int DataConnectionConnectTimeout {
			get {
				return m_dataConnectionConnectTimeout;
			}
			set {
				m_dataConnectionConnectTimeout = value;
			}
		}

		int m_dataConnectionReadTimeout = 15000;
		/// <summary>
		/// Gets or sets the length of time in milliseconds the data channel
		/// should wait for the server to send data. Default value is 
		/// 15000 (15 seconds).
		/// </summary>
		public int DataConnectionReadTimeout {
			get {
				return m_dataConnectionReadTimeout;
			}
			set {
				m_dataConnectionReadTimeout = value;
			}
		}

		bool m_keepAlive = false;
		/// <summary>
		/// Gets or sets a value indicating if <see cref="System.Net.Sockets.SocketOptionName.KeepAlive"/> should be set on 
		/// the underlying stream's socket. If the connection is alive, the option is
		/// adjusted in real-time. The value is stored and the KeepAlive option is set
		/// accordingly upon any new connections. The value set here is also applied to
		/// all future data streams. It has no affect on cloned control connections or
		/// data connections already in progress. The default value is false.
		/// </summary>
		public bool SocketKeepAlive {
			get {
				return m_keepAlive;
			}
			set {
				m_keepAlive = value;
				if (m_stream != null)
					m_stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, value);
			}
		}

		FtpCapability m_capabilities = FtpCapability.NONE;
		/// <summary>
		/// Gets the server capabilities represented by flags
		/// </summary>
		public FtpCapability Capabilities {
			get {
				if (m_stream == null || !m_stream.IsConnected) {
					Connect();
				}

				return m_capabilities;
			}
			protected set {
				m_capabilities = value;
			}
		}

		FtpHashAlgorithm m_hashAlgorithms = FtpHashAlgorithm.NONE;
		/// <summary>
		/// Get the hash types supported by the server, if any. This
		/// is a recent extension to the protocol that is not fully
		/// standardized and is not guaranteed to work. See here for
		/// more details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </summary>
		public FtpHashAlgorithm HashAlgorithms {
			get {
				if (m_stream == null || !m_stream.IsConnected) {
					Connect();
				}

				return m_hashAlgorithms;
			}
			private set {
				m_hashAlgorithms = value;
			}
		}

		FtpEncryptionMode m_encryptionmode = FtpEncryptionMode.None;
		/// <summary>
		/// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
		/// </summary>
		public FtpEncryptionMode EncryptionMode {
			get {
				return m_encryptionmode;
			}
			set {
				m_encryptionmode = value;
			}
		}

		bool m_dataConnectionEncryption = true;
		/// <summary>
		/// Indicates if data channel transfers should be encrypted. Only valid if <see cref="EncryptionMode"/>
		/// property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// </summary>
		public bool DataConnectionEncryption {
			get {
				return m_dataConnectionEncryption;
			}
			set {
				m_dataConnectionEncryption = value;
			}
		}

#if !CORE
		bool m_plainTextEncryption = false;
		/// <summary>
		/// Indicates if the encryption should be disabled immediately after connecting using a CCC command.
		/// This is useful when you have a FTP firewall that requires plaintext FTP, but your server mandates FTPS connections.
		/// </summary>
		public bool PlainTextEncryption {
			get {
				return m_plainTextEncryption;
			}
			set {
				m_plainTextEncryption = value;
			}
		}
#endif

#if CORE
		private SslProtocols m_SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#else
		private SslProtocols m_SslProtocols = SslProtocols.Default;
#endif
		/// <summary>
		/// Encryption protocols to use. Only valid if EncryptionMode property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// Default value is .NET Framework defaults from the <see cref="System.Net.Security.SslStream"/> class.
		/// </summary>
		public SslProtocols SslProtocols {
			get {
				return m_SslProtocols;
			}
			set {
				m_SslProtocols = value;
			}
		}

		private FtpsBuffering m_SslBuffering = FtpsBuffering.Auto;
		/// <summary>
		/// Whether to use SSL Buffering to speed up data transfer during FTP operations
		/// </summary>
		public FtpsBuffering SslBuffering {
			get {
				return m_SslBuffering;
			}
			set {
				m_SslBuffering = value;
			}
		}

		FtpSslValidation m_sslvalidate = null;
		/// <summary>
		/// Event is fired to validate SSL certificates. If this event is
		/// not handled and there are errors validating the certificate
		/// the connection will be aborted.
		/// </summary>
		/// <example><code source="..\Examples\ValidateCertificate.cs" lang="cs" /></example>
		public event FtpSslValidation ValidateCertificate {
			add {
				m_sslvalidate += value;
			}
			remove {
				m_sslvalidate -= value;
			}
		}


		private string m_systemType = "UNKNOWN";
		/// <summary>
		/// Gets the type of system/server that we're connected to. Typically begins with "WINDOWS" or "UNIX".
		/// </summary>
		public string SystemType {
			get {
				return m_systemType;
			}
		}

		private FtpServer m_serverType = FtpServer.Unknown;
		/// <summary>
		/// Gets the type of the FTP server software that we're connected to.
		/// </summary>
		public FtpServer ServerType {
			get {
				return m_serverType;
			}
		}

		private FtpOperatingSystem m_serverOS = FtpOperatingSystem.Unknown;
		/// <summary>
		/// Gets the operating system of the FTP server that we're connected to.
		/// </summary>
		public FtpOperatingSystem ServerOS {
			get {
				return m_serverOS;
			}
		}

		private string m_connectionType = "Default";
		/// <summary> Gets the connection type </summary>
		public string ConnectionType {
			get { return m_connectionType; }
			protected set { m_connectionType = value; }
		}

		private FtpReply m_lastReply;
		/// <summary> Gets the last reply recieved from the server</summary>
		public FtpReply LastReply {
			get { return m_lastReply; }
			protected set { m_lastReply = value; }
		}


		private FtpParser m_parser = FtpParser.Auto;
		/// <summary>
		/// File listing parser to be used. 
		/// Automatically calculated based on the type of the server, unless changed.
		/// </summary>
		public FtpParser ListingParser {
			get { return m_parser; }
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
			get { return m_parserCulture; }
			set {
				m_parserCulture = value;
			}
		}

		private double m_timeDiff = 0;
		/// <summary>
		/// Time difference between server and client, in hours.
		/// If the server is located in New York and you are in London then the time difference is -5 hours.
		/// </summary>
		public double TimeOffset {
			get { return m_timeDiff; }
			set {
				m_timeDiff = value;

				// configure parser
				int hours = (int)Math.Floor(m_timeDiff);
				int mins = (int)Math.Floor((m_timeDiff - Math.Floor(m_timeDiff)) * 60);
				m_listParser.TimeOffset = new TimeSpan(hours, mins, 0);
				m_listParser.HasTimeOffset = m_timeDiff != 0;
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
			get {
				return m_bulkListing;
			}
			set {
				m_bulkListing = value;
			}
		}

		private int m_bulkListingLength = 128;

		/// <summary>
		/// Bytes to read during GetListing. Only honored if <see cref="BulkListing"/> is true.
		/// </summary>
		public int BulkListingLength {
			get {
				return m_bulkListingLength;
			}
			set {
				m_bulkListingLength = value;
			}
		}


		private int m_transferChunkSize = 65536;
		/// <summary>
		/// Gets or sets the number of bytes transferred in a single chunk (a single FTP command).
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// to transfer large files in multiple chunks.
		/// </summary>
		public int TransferChunkSize {
			get {
				return m_transferChunkSize;
			}
			set {
				m_transferChunkSize = value;
			}
		}

		private FtpDataType CurrentDataType;

		private int m_retryAttempts = 3;
		/// <summary>
		/// Gets or sets the retry attempts allowed when a verification failure occurs during download or upload.
		/// This value must be set to 1 or more.
		/// </summary>
		public int RetryAttempts {
			get { return m_retryAttempts; }
			set { m_retryAttempts = value > 0 ? value : 1; }
		}

		uint m_uploadRateLimit = 0;

		/// <summary>
		/// Rate limit for uploads in kbyte/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint UploadRateLimit {
			get { return m_uploadRateLimit; }
			set { m_uploadRateLimit = value; }
		}

		uint m_downloadRateLimit = 0;

		/// <summary>
		/// Rate limit for downloads in kbytes/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint DownloadRateLimit {
			get { return m_downloadRateLimit; }
			set { m_downloadRateLimit = value; }
		}

		public FtpDataType m_UploadDataType = FtpDataType.Binary;
		/// <summary>
		/// Controls if the high-level API uploads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType UploadDataType {
			get { return m_UploadDataType; }
			set { m_UploadDataType = value; }
		}

		public FtpDataType m_DownloadDataType = FtpDataType.Binary;
		/// <summary>
		/// Controls if the high-level API downloads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType DownloadDataType {
			get { return m_DownloadDataType; }
			set { m_DownloadDataType = value; }
		}


		// ADD PROPERTIES THAT NEED TO BE CLONED INTO
		// FtpClient.CloneConnection()

	}
}