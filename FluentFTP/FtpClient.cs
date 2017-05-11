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
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX45)
using System.Threading;
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	/// <summary>
	/// Event is fired when a SSL certificate needs to be validated
	/// </summary>
	/// <param name="control">The control connection that triggered the event</param>
	/// <param name="e">Event args</param>
	public delegate void FtpSslValidation(FtpClient control, FtpSslValidationEventArgs e);

	/// <summary>
	/// FTP Control Connection. Speaks the FTP protocol with the server and
	/// provides facilities for performing transactions.
	/// 
	/// Debugging problems with FTP transactions is much easier to do when
	/// you can see exactly what is sent to the server and the reply 
	/// FluentFTP gets in return. Please review the Debug example
    /// below for information on how to add <see cref="System.Diagnostics.TraceListener"/>s for capturing
	/// the conversation between FluentFTP and the server.
	/// </summary>
	/// <example>The following example illustrates how to assist in debugging
	/// FluentFTP by getting a transaction log from the server.
	/// <code source="..\Examples\Debug.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates adding a custom file
	/// listing parser in the event that you encounter a list format
	/// not already supported.
	/// <code source="..\Examples\CustomParser.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to validate
	/// a SSL certificate when using SSL/TLS.
	/// <code source="..\Examples\ValidateCertificate.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file.
	/// <code source="..\Examples\OpenRead.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file
	/// using a URI object.
	/// <code source="..\Examples\OpenReadURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file.
	/// <code source="..\Examples\OpenWrite.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file
	/// using a URI object.
	/// <code source="..\Examples\OpenWriteURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file.
	/// <code source="..\Examples\OpenAppend.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file
	/// using a URI object.
	/// <code source="..\Examples\OpenAppendURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to get a file
	/// listing from the server.
	/// <code source="..\Examples\GetListing.cs" lang="cs" />
	/// </example>
	public class FtpClient : IDisposable {

		#region Properties

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
				lock (m_lock) {
					m_textEncoding = value;
					m_textEncodingAutoUTF = false;
				}
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
        public Func<string> AddressResolver
	    {
	        get { return m_AddressResolver; }
            set { m_AddressResolver = value; }
	    }
        
        IEnumerable<int> m_ActivePorts;

        /// <summary>
        /// Ports used for Active Data Connection
        /// </summary>
	    public IEnumerable<int> ActivePorts
	    {
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

		FtpCapability m_caps = FtpCapability.NONE;
		/// <summary>
		/// Gets the server capabilities represented by flags
		/// </summary>
		public FtpCapability Capabilities {
			get {
				if (m_stream == null || !m_stream.IsConnected) {
					Connect();
				}

				return m_caps;
			}
			protected set {
				m_caps = value;
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

#if CORE
		private SslProtocols m_SslProtocols = SslProtocols.Tls11 | SslProtocols.Ssl3;
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

		/// <summary>
		/// Gets the type of system/server that we're
		/// connected to.
		/// </summary>
		private string m_systemType = "UNKNOWN";
		public string SystemType {
			get {
				return m_systemType;
			}
		}

		private string m_connectionType = "Default";
		/// <summary> Gets the connection type </summary>
		public string ConnectionType {
			get { return m_connectionType; }
			protected set { m_connectionType = value; }
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
				m_listParser.parser = value;
				m_listParser.parserConfirmed = false;
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

				// configure parser
				m_listParser.parserCulture = value;
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
				m_listParser.timeOffset = new TimeSpan(hours, mins, 0);
				m_listParser.hasTimeOffset = m_timeDiff != 0;
			}
		}

		// ADD PROPERTIES THAT NEED TO BE CLONED INTO
		// FtpClient.CloneConnection()

		#endregion

		#region Constructor / Connection

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
		/// Creates a new instance of an FTP Client, with the given host, username and password.
		/// </summary>
		public FtpClient(string host, string user, string pass) {
			Host = host;
			Credentials = new NetworkCredential(user, pass);
			m_listParser = new FtpListParser(this);
		}

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
		/// Fires the SSL validation event
		/// </summary>
		/// <param name="e">Event Args</param>
		void OnValidateCertficate(FtpSslValidationEventArgs e) {
			FtpSslValidation evt;

			evt = m_sslvalidate;
			if (evt != null)
				evt(this, e);
		}

		/// <summary>
		/// Retretieves the delegate for the specified IAsyncResult and removes
		/// it from the m_asyncmethods collection if the operation is successfull
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
			conn.TransferChunkSize = TransferChunkSize;
			conn.ListingParser = ListingParser;
			conn.ListingCulture = ListingCulture;
			conn.TimeOffset = TimeOffset;

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
				delegate(FtpClient obj, FtpSslValidationEventArgs e) {
					e.Accept = true;
				});

			return conn;
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
		public void Dispose() {
			lock (m_lock) {
				if (IsDisposed)
					return;

				FtpTrace.WriteLine("Disposing FtpClient object...");

				try {
					if (IsConnected) {
						Disconnect();
					}
				} catch (Exception ex) {
					FtpTrace.WriteLine("FtpClient.Dispose(): Caught and discarded an exception while disconnecting from host: " + ex.ToString());
				}

				if (m_stream != null) {
					try {
						m_stream.Dispose();
					} catch (Exception ex) {
						FtpTrace.WriteLine("FtpClient.Dispose(): Caught and discarded an exception while disposing FtpStream object: " + ex.ToString());
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
			}
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~FtpClient() {
			Dispose();
		}

		#endregion

		#region Execute Command

		/// <summary>
		/// Retrieves a reply from the server. Do not execute this method
		/// unless you are sure that a reply has been sent, i.e., you
		/// executed a command. Doing so will cause the code to hang
		/// indefinitely waiting for a server reply that is never coming.
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		/// <example><code source="..\Examples\BeginGetReply.cs" lang="cs" /></example>
		public FtpReply GetReply() {
			FtpReply reply = new FtpReply();
			string buf;

			lock (m_lock) {
				if (!IsConnected)
					throw new InvalidOperationException("No connection to the server has been established.");

				m_stream.ReadTimeout = m_readTimeout;
				while ((buf = m_stream.ReadLine(Encoding)) != null) {
					Match m;

					FtpTrace.WriteLine(buf);

					if ((m = Regex.Match(buf, "^(?<code>[0-9]{3}) (?<message>.*)$")).Success) {
						reply.Code = m.Groups["code"].Value;
						reply.Message = m.Groups["message"].Value;
						break;
					}

					reply.InfoMessages += (buf + "\n");
				}
			}

			return reply;
		}

		/// <summary>
		/// Executes a command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <returns>The servers reply to the command</returns>
		/// <example><code source="..\Examples\Execute.cs" lang="cs" /></example>
		public FtpReply Execute(string command) {
			FtpReply reply;

			lock (m_lock) {
				if (StaleDataCheck) {
					if (m_stream != null && m_stream.SocketDataAvailable > 0) {
						// Data shouldn't be on the socket, if it is it probably
						// means we've been disconnected. Read and discard
						// whatever is there and close the connection.

						FtpTrace.WriteLine("There is stale data on the socket, maybe our connection timed out. Re-connecting.");
						if (m_stream.IsConnected && !m_stream.IsEncrypted) {
							byte[] buf = new byte[m_stream.SocketDataAvailable];
							m_stream.RawSocketRead(buf);
							FtpTrace.Write("The data was: ");
							FtpTrace.WriteLine(Encoding.GetString(buf).TrimEnd('\r', '\n'));
						}

						m_stream.Close();
					}
				}

				if (!IsConnected) {
					if (command == "QUIT") {
						FtpTrace.WriteLine("Not sending QUIT because the connection has already been closed.");
						return new FtpReply() {
							Code = "200",
							Message = "Connection already closed."
						};
					}

					Connect();
				}

				FtpTrace.WriteLine(command.StartsWith("PASS") ? "PASS <omitted>" : command);
				m_stream.WriteLine(m_textEncoding, command);
				reply = GetReply();
			}

			return reply;
		}

		delegate FtpReply AsyncExecute(string command);

		/// <summary>
        /// Performs execution of the specified command asynchronously
		/// </summary>
		/// <param name="command">The command to execute</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
		public IAsyncResult BeginExecute(string command, AsyncCallback callback, object state) {
			AsyncExecute func;
			IAsyncResult ar;

			ar = (func = new AsyncExecute(Execute)).BeginInvoke(command, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous command
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginExecute</param>
		/// <returns>FtpReply object (never null).</returns>
		/// <example><code source="..\Examples\BeginExecute.cs" lang="cs" /></example>
		public FtpReply EndExecute(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncExecute>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Performs an asynchronous execution of the specified command
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The servers reply to the command</returns>
	    public async Task<FtpReply> ExecuteAsync(string command) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, FtpReply>(
	            (c, ac, state) => BeginExecute(c, ac, state),
	            ar => EndExecute(ar),
	            command, null);
	    }
#endif

		#endregion

		#region Connection

		private FtpListParser m_listParser;

		/// <summary>
		/// Connect to the server
		/// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this object has been disposed.</exception>
		/// <example><code source="..\Examples\Connect.cs" lang="cs" /></example>
		public virtual void Connect() {
			FtpReply reply;

			lock (m_lock) {
				if (IsDisposed)
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");

				if (m_stream == null) {
					m_stream = new FtpSocketStream();
					m_stream.ValidateCertificate += new FtpSocketStreamSslValidation(FireValidateCertficate);
				} else
					if (IsConnected)
						Disconnect();

				if (Host == null)
					throw new FtpException("No host has been specified");

				if (!IsClone)
					m_caps = FtpCapability.NONE;

				m_hashAlgorithms = FtpHashAlgorithm.NONE;
				m_stream.ConnectTimeout = m_connectTimeout;
				m_stream.SocketPollInterval = m_socketPollInterval;
				Connect(m_stream);

				m_stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
					System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

#if !NO_SSL
				if (EncryptionMode == FtpEncryptionMode.Implicit)
					m_stream.ActivateEncryption(Host,
						m_clientCerts.Count > 0 ? m_clientCerts : null,
						m_SslProtocols);
#endif

				Handshake();

#if !NO_SSL
				if (EncryptionMode == FtpEncryptionMode.Explicit) {
					if (!(reply = Execute("AUTH TLS")).Success)
						throw new FtpSecurityNotAvailableException("AUTH TLS command failed.");
					m_stream.ActivateEncryption(Host,
						m_clientCerts.Count > 0 ? m_clientCerts : null,
						m_SslProtocols);
				}
#endif

				if (m_credentials != null) {
					Authenticate();
				}

				if (m_stream.IsEncrypted && DataConnectionEncryption) {
					if (!(reply = Execute("PBSZ 0")).Success)
						throw new FtpCommandException(reply);
					if (!(reply = Execute("PROT P")).Success)
						throw new FtpCommandException(reply);
				}

				// if this is a clone these values
				// should have already been loaded
				// so save some bandwidth and CPU
				// time and skip executing this again.
				if (!IsClone) {
					if ((reply = Execute("FEAT")).Success && reply.InfoMessages != null) {
						GetFeatures(reply);
					}
				}

				// Enable UTF8 if the encoding is ASCII and UTF8 is supported
				if (m_textEncodingAutoUTF && m_textEncoding == Encoding.ASCII && HasFeature(FtpCapability.UTF8)) {
					m_textEncoding = Encoding.UTF8;
				}

				FtpTrace.WriteLine("Text encoding: " + m_textEncoding.ToString());

				if (m_textEncoding == Encoding.UTF8) {
					// If the server supports UTF8 it should already be enabled and this
					// command should not matter however there are conflicting drafts
					// about this so we'll just execute it to be safe. 
					Execute("OPTS UTF8 ON");
				}

				// Get the system type - Needed to auto-detect file listing parser
				if ((reply = Execute("SYST")).Success) {
					m_systemType = reply.Message;
				}

				// Create the parser even if the auto-OS detection failed
				m_listParser.Init(m_systemType);

			}
		}

		/// <summary>
		/// Connect to the FTP server. Overwritten in proxy classes.
		/// </summary>
		/// <param name="stream"></param>
		protected virtual void Connect(FtpSocketStream stream) {
			stream.Connect(Host, Port, InternetProtocolVersions);
		}

		/// <summary>
		/// Connect to the FTP server. Overwritten in proxy classes.
		/// </summary>
		protected virtual void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			stream.Connect(host, port, ipVersions);
		}


		protected virtual void Handshake() {
			FtpReply reply;
			if (!(reply = GetReply()).Success) {
				if (reply.Code == null) {
					throw new IOException("The connection was terminated before a greeting could be read.");
				} else {
					throw new FtpCommandException(reply);
				}
			}
		}

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual void Authenticate() {
			Authenticate(Credentials.UserName, Credentials.Password);
		}

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual void Authenticate(string userName, string password) {
			FtpReply reply;

			if (!(reply = Execute("USER " + userName)).Success)
				throw new FtpCommandException(reply);

			if (reply.Type == FtpResponseType.PositiveIntermediate
				&& !(reply = Execute("PASS " + password)).Success)
				throw new FtpCommandException(reply);
		}

		/// <summary>
		/// Populates the capabilities flags based on capabilities
		/// supported by this server. This method is overridable
		/// so that new features can be supported
		/// </summary>
		/// <param name="reply">The reply object from the FEAT command. The InfoMessages property will
		/// contain a list of the features the server supported delimited by a new line '\n' character.</param>
		protected virtual void GetFeatures(FtpReply reply) {
			foreach (string feat in reply.InfoMessages.Split('\n')) {
				if (feat.ToUpper().Trim().StartsWith("MLST") || feat.ToUpper().Trim().StartsWith("MLSD"))
					m_caps |= FtpCapability.MLSD;
				else if (feat.ToUpper().Trim().StartsWith("MDTM"))
					m_caps |= FtpCapability.MDTM;
				else if (feat.ToUpper().Trim().StartsWith("REST STREAM"))
					m_caps |= FtpCapability.REST;
				else if (feat.ToUpper().Trim().StartsWith("SIZE"))
					m_caps |= FtpCapability.SIZE;
				else if (feat.ToUpper().Trim().StartsWith("UTF8"))
					m_caps |= FtpCapability.UTF8;
				else if (feat.ToUpper().Trim().StartsWith("PRET"))
					m_caps |= FtpCapability.PRET;
				else if (feat.ToUpper().Trim().StartsWith("MFMT"))
					m_caps |= FtpCapability.MFMT;
				else if (feat.ToUpper().Trim().StartsWith("MFCT"))
					m_caps |= FtpCapability.MFCT;
				else if (feat.ToUpper().Trim().StartsWith("MFF"))
					m_caps |= FtpCapability.MFF;
				else if (feat.ToUpper().Trim().StartsWith("MD5"))
					m_caps |= FtpCapability.MD5;
				else if (feat.ToUpper().Trim().StartsWith("XMD5"))
					m_caps |= FtpCapability.XMD5;
				else if (feat.ToUpper().Trim().StartsWith("XCRC"))
					m_caps |= FtpCapability.XCRC;
				else if (feat.ToUpper().Trim().StartsWith("XSHA1"))
					m_caps |= FtpCapability.XSHA1;
				else if (feat.ToUpper().Trim().StartsWith("XSHA256"))
					m_caps |= FtpCapability.XSHA256;
				else if (feat.ToUpper().Trim().StartsWith("XSHA512"))
					m_caps |= FtpCapability.XSHA512;
				else if (feat.ToUpper().Trim().StartsWith("HASH")) {
					Match m;

					m_caps |= FtpCapability.HASH;

					if ((m = Regex.Match(feat.ToUpper().Trim(), @"^HASH\s+(?<types>.*)$")).Success) {
						foreach (string type in m.Groups["types"].Value.Split(';')) {
							switch (type.ToUpper().Trim()) {
								case "SHA-1":
								case "SHA-1*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA1;
									break;
								case "SHA-256":
								case "SHA-256*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA256;
									break;
								case "SHA-512":
								case "SHA-512*":
									m_hashAlgorithms |= FtpHashAlgorithm.SHA512;
									break;
								case "MD5":
								case "MD5*":
									m_hashAlgorithms |= FtpHashAlgorithm.MD5;
									break;
								case "CRC":
								case "CRC*":
									m_hashAlgorithms |= FtpHashAlgorithm.CRC;
									break;
							}
						}
					}
				}
			}
		}

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

			ar = (func = new AsyncConnect(Connect)).BeginInvoke(callback, state);

			lock (m_asyncmethods) {
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

#if (CORE || NETFX45)
        /// <summary>
        /// Connects to the server asynchronously
        /// </summary>
	    public async Task ConnectAsync() {
            //TODO:  Rewrite as true async method with cancellation support
            await Task.Factory.FromAsync(
	            (c, s) => BeginConnect(c, s),
	            ar => EndConnect(ar),
	            null);
	    }
#endif

		/// <summary>
		/// Catches the socket stream ssl validation event and fires the event handlers
		/// attached to this object for validating SSL certificates
		/// </summary>
		/// <param name="stream">The stream that fired the event</param>
		/// <param name="e">The event args used to validate the certficate</param>
		void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
			OnValidateCertficate(e);
		}

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		public virtual void Disconnect() {
			lock (m_lock) {
				if (m_stream != null && m_stream.IsConnected) {
					try {
						if (!UngracefullDisconnection) {
							Execute("QUIT");
						}
					} catch (SocketException sockex) {
						FtpTrace.WriteLine("FtpClient.Disconnect(): SocketException caught and discarded while closing control connection: "+ sockex.ToString());
					} catch (IOException ioex) {
						FtpTrace.WriteLine("FtpClient.Disconnect(): IOException caught and discarded while closing control connection: "+ ioex.ToString());
					} catch (FtpCommandException cmdex) {
						FtpTrace.WriteLine("FtpClient.Disconnect(): FtpCommandException caught and discarded while closing control connection: "+ cmdex.ToString());
					} catch (FtpException ftpex) {
						FtpTrace.WriteLine("FtpClient.Disconnect(): FtpException caught and discarded while closing control connection: "+ ftpex.ToString());
					} finally {
						m_stream.Close();
					}
				}
			}
		}

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

			ar = (func = new AsyncDisconnect(Disconnect)).BeginInvoke(callback, state);
			lock (m_asyncmethods) {
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
        
#if (CORE || NETFX45)
        /// <summary>
        /// Disconnects from the server asynchronously
        /// </summary>
	    public async Task DisconnectAsync() {
            //TODO:  Rewrite as true async method with cancellation support
            await Task.Factory.FromAsync(
	            (c, s) => BeginDisconnect(c, s),
	            ar => EndDisconnect(ar),
	            null);
	    }
#endif

		#endregion

		#region Active/Passive Streams

		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		FtpDataStream OpenPassiveDataStream(FtpDataConnectionType type, string command, long restart) {
			FtpDataStream stream = null;
			FtpReply reply;
			Match m;
			string host = null;
			int port = 0;

			if (m_stream == null)
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");

			if (type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive) {
				if (!(reply = Execute("EPSV")).Success) {
					// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
					if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoPassive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						return OpenPassiveDataStream(FtpDataConnectionType.PASV, command, restart);
					throw new FtpCommandException(reply);
				}

				m = Regex.Match(reply.Message, @"\(\|\|\|(?<port>\d+)\|\)");
				if (!m.Success) {
					throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
				}

				host = m_host;
				port = int.Parse(m.Groups["port"].Value);
			} else {
				if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
					throw new FtpException("Only IPv4 is supported by the PASV command. Use EPSV instead.");

				if (!(reply = Execute("PASV")).Success)
					throw new FtpCommandException(reply);

				m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

				if (!m.Success || m.Groups.Count != 7)
					throw new FtpException(("Malformed PASV response: " + reply.Message));

				// PASVEX mode ignores the host supplied in the PASV response
				if (type == FtpDataConnectionType.PASVEX)
					host = m_host;
				else
					host = (m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value);

				port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);
			}

			stream = new FtpDataStream(this);
			stream.ConnectTimeout = DataConnectionConnectTimeout;
			stream.ReadTimeout = DataConnectionReadTimeout;
			Connect(stream, host, port, InternetProtocolVersions);
			stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

			if (restart > 0) {
				if (!(reply = Execute("REST " + restart)).Success)
					throw new FtpCommandException(reply);
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

#if !NO_SSL
			// this needs to take place after the command is executed
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
				stream.ActivateEncryption(m_host,
					this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
					m_SslProtocols);
#endif

			return stream;
		}

        /// <summary>
        /// Returns the ip address to be sent to the server for the active connection
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
	    string GetLocalAddress(IPAddress ip)
	    {
            // Use resolver
	        if (m_AddressResolver != null)
	        {
	            return m_Address ?? (m_Address = m_AddressResolver());
	        }

            // Use supplied ip
	        return ip.ToString();
	    }

		/// <summary>
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		FtpDataStream OpenActiveDataStream(FtpDataConnectionType type, string command, long restart) {
			FtpDataStream stream = new FtpDataStream(this);
			FtpReply reply;
#if !CORE
			IAsyncResult ar;
#endif

			if (m_stream == null)
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");

		    if (m_ActivePorts == null || !m_ActivePorts.Any())
		    {
                // Use random port
		        stream.Listen(m_stream.LocalEndPoint.Address, 0);
		    }
		    else
		    {
		        var success = false;
                // Use one of the specified ports
		        foreach (var port in m_ActivePorts)
		        {
		            try
		            {
                        stream.Listen(m_stream.LocalEndPoint.Address, port);
		                success = true;
		            }
		            catch (SocketException se)
		            {
#if NETFX
                        // Already in use
                        if (se.ErrorCode != 10048)
                            throw;
#else
                        throw;
#endif
                    }
		        }

                // No usable port found
                if (!success)
                    throw new Exception("No valid active data port available!");
		    }
#if !CORE
			ar = stream.BeginAccept(null, null);
#endif

			if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive) {
				int ipver = 0;

				switch (stream.LocalEndPoint.AddressFamily) {
					case System.Net.Sockets.AddressFamily.InterNetwork:
						ipver = 1; // IPv4
						break;
					case System.Net.Sockets.AddressFamily.InterNetworkV6:
						ipver = 2; // IPv6
						break;
					default:
						throw new InvalidOperationException("The IP protocol being used is not supported.");
				}

				if (!(reply = Execute("EPRT |" + ipver + "|" + GetLocalAddress(stream.LocalEndPoint.Address) + "|" + stream.LocalEndPoint.Port + "|")).Success) {

					// if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
					if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
						stream.ControlConnection = null; // we don't want this failed EPRT attempt to close our control connection when the stream is closed so clear out the reference.
						stream.Close();
						return OpenActiveDataStream(FtpDataConnectionType.PORT, command, restart);
					} else {
						stream.Close();
						throw new FtpCommandException(reply);
					}
				}
			} else {
				if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
					throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");

				if (!(reply = Execute("PORT " +
                        GetLocalAddress(stream.LocalEndPoint.Address).Replace('.', ',') + "," +
						stream.LocalEndPoint.Port / 256 + "," +
						stream.LocalEndPoint.Port % 256)).Success) {
					stream.Close();
					throw new FtpCommandException(reply);
				}
			}

			if (restart > 0) {
				if (!(reply = Execute("REST " + restart)).Success)
					throw new FtpCommandException(reply);
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

#if CORE
			stream.AcceptAsync().Wait();
#else
			ar.AsyncWaitHandle.WaitOne(m_dataConnectionConnectTimeout);
			if (!ar.IsCompleted) {
				stream.Close();
				throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
			}

			stream.EndAccept(ar);
#endif

#if !NO_SSL
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
				stream.ActivateEncryption(m_host,
					this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
					m_SslProtocols);
#endif

			stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);
			stream.ReadTimeout = m_dataConnectionReadTimeout;

			return stream;
		}

		/// <summary>
		/// Opens a data stream.
		/// </summary>
		/// <param name='command'>The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>The data stream.</returns>
		FtpDataStream OpenDataStream(string command, long restart) {
			FtpDataConnectionType type = m_dataConnectionType;
			FtpDataStream stream = null;

			lock (m_lock) {
				if (!IsConnected)
					Connect();

				// The PORT and PASV commands do not work with IPv6 so
				// if either one of those types are set change them
				// to EPSV or EPRT appropriately.
				if (m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
					switch (type) {
						case FtpDataConnectionType.PORT:
							type = FtpDataConnectionType.EPRT;
							FtpTrace.WriteLine("Changed data connection type to EPRT because we are connected with IPv6.");
							break;
						case FtpDataConnectionType.PASV:
						case FtpDataConnectionType.PASVEX:
							type = FtpDataConnectionType.EPSV;
							FtpTrace.WriteLine("Changed data connection type to EPSV because we are connected with IPv6.");
							break;
					}
				}

				switch (type) {
					case FtpDataConnectionType.AutoPassive:
					case FtpDataConnectionType.EPSV:
					case FtpDataConnectionType.PASV:
					case FtpDataConnectionType.PASVEX:
						stream = OpenPassiveDataStream(type, command, restart);
						break;
					case FtpDataConnectionType.AutoActive:
					case FtpDataConnectionType.EPRT:
					case FtpDataConnectionType.PORT:
						stream = OpenActiveDataStream(type, command, restart);
						break;
				}

				if (stream == null)
					throw new InvalidOperationException("The specified data channel type is not implemented.");
			}

			return stream;
		}

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		internal FtpReply CloseDataStream(FtpDataStream stream) {
			FtpReply reply = new FtpReply();

			if (stream == null)
				throw new ArgumentException("The data stream parameter was null");

			lock (m_lock) {
				try {
					if (IsConnected) {
						// if the command that required the data connection was
						// not successful then there will be no reply from
						// the server, however if the command was successful
						// the server will send a reply when the data connection
						// is closed.
						if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
							if (!(reply = GetReply()).Success) {
								throw new FtpCommandException(reply);
							}
						}
					}
				} finally {
					// if this is a clone of the original control
					// connection we should Dispose()
					if (IsClone) {
						Disconnect();
						Dispose();
					}
				}
			}

			return reply;
		}

		#endregion

		#region Open Read

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path) {
			return OpenRead(path, FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, FtpDataType type) {
			return OpenRead(path, type, 0);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, long restart) {
			return OpenRead(path, FtpDataType.Binary, restart);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public virtual Stream OpenRead(string path, FtpDataType type, long restart) {
			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

			lock (m_lock) {
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				client.SetDataType(type);
				length = client.GetFileSize(path);
				stream = client.OpenDataStream(("RETR " + path.GetFtpPath()), restart);
			}

			if (stream != null) {
				if (length > 0)
					stream.SetLength(length);

				if (restart > 0)
					stream.SetPosition(restart);
			}

			return stream;
		}

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, AsyncCallback callback, object state) {
			return BeginOpenRead(path, FtpDataType.Binary, 0, callback, state);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, FtpDataType type, AsyncCallback callback, object state) {
			return BeginOpenRead(path, type, 0, callback, state);
		}

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, long restart, AsyncCallback callback, object state) {
			return BeginOpenRead(path, FtpDataType.Binary, restart, callback, state);
		}

		delegate Stream AsyncOpenRead(string path, FtpDataType type, long restart);

		/// <summary>
		/// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, FtpDataType type, long restart, AsyncCallback callback, object state) {
			AsyncOpenRead func;
			IAsyncResult ar;

			ar = (func = new AsyncOpenRead(OpenRead)).BeginInvoke(path, type, restart, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="o:BeginOpenRead"/>
		/// </summary>
        /// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenRead"/></param>
		/// <returns>A readable stream of the remote file</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public Stream EndOpenRead(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenRead>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Opens the specified file for reading asynchronously
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A readable stream of the remote file</returns>
	    public async Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, FtpDataType, long, Stream>(
	            (p, t, r, ac, s) => BeginOpenRead(p, t, r, ac, s),
	            ar => EndOpenRead(ar),
	            path, type, restart, null);
	    }

        /// <summary>
        /// Opens the specified file for reading asynchronously
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A readable stream of the remote file</returns>
	    public async Task<Stream> OpenReadAsync(string path, FtpDataType type) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, FtpDataType, Stream>(
	            (p, t, ac, s) => BeginOpenRead(p, t, ac, s),
	            ar => EndOpenRead(ar),
	            path, type, null);
	    }

        /// <summary>
        /// Opens the specified file for reading asynchronously
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <param name="restart">Resume location</param>
        /// <returns>A readable stream of the remote file</returns>
	    public async Task<Stream> OpenReadAsync(string path, long restart) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, long, Stream>(
	            (p, r, ac, s) => BeginOpenRead(p, r, ac, s),
	            ar => EndOpenRead(ar),
	            path, restart, null);
	    }

        /// <summary>
        /// Opens the specified file for reading asynchronously
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>A readable stream of the remote file</returns>
        public async Task<Stream> OpenReadAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, Stream>(
                (p, ac, s) => BeginOpenRead(p, ac, s),
                ar => EndOpenRead(ar),
                path, null);
        }
#endif

		#endregion

		#region Open Write

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
		public Stream OpenWrite(string path) {
			return OpenWrite(path, FtpDataType.Binary);
		}

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
		public virtual Stream OpenWrite(string path, FtpDataType type) {
			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

			lock (m_lock) {
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				client.SetDataType(type);
				length = client.GetFileSize(path);
				stream = client.OpenDataStream(("STOR " + path.GetFtpPath()), 0);

				if (length > 0 && stream != null)
					stream.SetLength(length);
			}

			return stream;
		}

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for writing
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenWrite(string path, AsyncCallback callback, object state) {
			return BeginOpenWrite(path, FtpDataType.Binary, callback, state);
		}

		delegate Stream AsyncOpenWrite(string path, FtpDataType type);

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for writing
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenWrite(string path, FtpDataType type, AsyncCallback callback, object state) {
			AsyncOpenWrite func;
			IAsyncResult ar;

			ar = (func = new AsyncOpenWrite(OpenWrite)).BeginInvoke(path, type, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="o:BeginOpenWrite"/>
		/// </summary>
        /// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenWrite"/></param>
		/// <returns>A writable stream</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public Stream EndOpenWrite(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenWrite>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket. asynchronously
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
	    public async Task<Stream> OpenWriteAsync(string path, FtpDataType type) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, FtpDataType, Stream>(
	            (p, t, ac, s) => BeginOpenWrite(p, t, ac, s),
	            ar => EndOpenWrite(ar),
	            path, type, null);
	    }

        /// <summary>
        /// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket. asynchronously
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <returns>A stream for writing to the file on the server</returns>
        public async Task<Stream> OpenWriteAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, Stream>(
                (p, ac, s) => BeginOpenWrite(p, ac, s),
                ar => EndOpenWrite(ar),
                path, null);
        }
#endif

		#endregion

		#region Open Append

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
		public Stream OpenAppend(string path) {
			return OpenAppend(path, FtpDataType.Binary);
		}

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <param name="type">ASCII/Binary</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
		public virtual Stream OpenAppend(string path, FtpDataType type) {
			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

			lock (m_lock) {
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				client.SetDataType(type);
				length = client.GetFileSize(path);
				stream = client.OpenDataStream(("APPE " + path.GetFtpPath()), 0);

				if (length > 0 && stream != null) {
					stream.SetLength(length);
					stream.SetPosition(length);
				}
			}

			return stream;
		}

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for appending
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenAppend(string path, AsyncCallback callback, object state) {
			return BeginOpenAppend(path, FtpDataType.Binary, callback, state);
		}

		delegate Stream AsyncOpenAppend(string path, FtpDataType type);

		/// <summary>
        /// Begins an asynchronous operation to open the specified file for appending
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenAppend(string path, FtpDataType type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncOpenAppend func;

			ar = (func = new AsyncOpenAppend(OpenAppend)).BeginInvoke(path, type, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="o:BeginOpenAppend"/>
		/// </summary>
        /// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenAppend"/></param>
		/// <returns>A writable stream</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public Stream EndOpenAppend(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenAppend>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Opens the specified file to be appended asynchronously
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <param name="type">ASCII/Binary</param>
        /// <returns>A stream for writing to the file on the server</returns>
        public async Task<Stream> OpenAppendAsync(string path, FtpDataType type){
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, FtpDataType, Stream>(
                (p, t, ac, s) => BeginOpenAppend(p, t, ac, s),
                ar => EndOpenAppend(ar),
                path, type, null);
        }

        /// <summary>
        /// Opens the specified file to be appended asynchronously
        /// </summary>
        /// <param name="path">Full or relative path of the file</param>
        /// <returns>A stream for writing to the file on the server</returns>
        public async Task<Stream> OpenAppendAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, Stream>(
                (p, ac, s) => BeginOpenAppend(p, ac, s),
                ar => EndOpenAppend(ar),
                path, null);
        }
#endif

#endregion

		#region Multi File Upload/Download

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		public int UploadFiles(string[] localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true) {

			int count = 0;

			// ensure ends with slash
			remoteDir = !remoteDir.EndsWith("/") ? remoteDir + "/" : remoteDir;

			//flag to determine if existence checks are required
			bool checkFileExistence = true;

			// create remote dir if wanted
			if (createRemoteDir) {
				if (!DirectoryExists(remoteDir)) {
					CreateDirectory(remoteDir);
					checkFileExistence = false;
				}
			}

			// get all the already existing files
			string[] existingFiles = checkFileExistence ? GetNameListing(remoteDir) : new string[0];

			// per local file
			foreach (string localPath in localPaths) {

				// calc remote path
				string fileName = Path.GetFileName(localPath);
				string remotePath = remoteDir + fileName;

				// try to upload it
				try {
					bool ok = UploadFileFromFile(localPath, remotePath, false, existsMode, existingFiles.Contains(fileName), true);
					if (ok) {
						count++;
					}
				} catch (Exception ex) {
				}

			}

			return count;
		}

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		public int UploadFiles(List<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true) {
			return UploadFiles(localPaths.ToArray(), remoteDir, existsMode, createRemoteDir);
		}

#if (CORE || NETFX45)
        /// <summary>
		/// Uploads the given file paths to a single folder on the server asynchronously.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="token">The token to monitor for cancelation requests</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode, bool createRemoteDir, CancellationToken token) {
            //check if cancellation was requested and throw to set TaskStatus state to Canceled
            token.ThrowIfCancellationRequested();

			int count = 0;
			// ensure ends with slash
			remoteDir = !remoteDir.EndsWith("/") ? remoteDir + "/" : remoteDir;

			//flag to determine if existence checks are required
            bool checkFileExistence = true;

            // create remote dir if wanted
			if (createRemoteDir) {
				if (!await DirectoryExistsAsync(remoteDir)) {
					await CreateDirectoryAsync(remoteDir);
                    checkFileExistence = false;
				}
			}

			// get all the already existing files (if directory was created just create an empty array)
			string[] existingFiles = checkFileExistence ? await GetNameListingAsync(remoteDir) : new string[0];

			// per local file
			foreach (string localPath in localPaths) {

                // check if cancellation was requested and throw to set TaskStatus state to Canceled
			    token.ThrowIfCancellationRequested();

				// calc remote path
				string fileName = Path.GetFileName(localPath);
				string remotePath = remoteDir + fileName;

				// try to upload it
			    try {
					bool ok = await UploadFileFromFileAsync(localPath, remotePath, false, existsMode, existingFiles.Contains(fileName), true, token);
			        if (ok) {
			            count++;
			        }
			    }
                catch (Exception ex) {
                    if (ex is OperationCanceledException) {
                        //DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
                        throw;
                    }
                    //suppress all other upload exceptions (errors are still written to FtpTrace)
				}
			}

			return count;
		}

        /// <summary>
        /// Uploads the given file paths to a single folder on the server asynchronously.
        /// All files are placed directly into the given folder regardless of their path on the local filesystem.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
        /// </summary>
        /// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
        /// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
        /// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true) {
			return await UploadFilesAsync(localPaths, remoteDir, existsMode, createRemoteDir, CancellationToken.None);
	    }
#endif

		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
        /// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		public int DownloadFiles(string localDir, string[] remotePaths, bool overwrite = true) {

			int count = 0;

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (string remotePath in remotePaths) {

				// calc local path
				string localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
					bool ok = DownloadFileToFile(localPath, remotePath, overwrite);
					if (ok) {
						count++;
					}
				} catch (Exception ex) {
				}

			}

			return count;
		}

		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
        /// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		public int DownloadFiles(string localDir, List<string> remotePaths, bool overwrite = true) {
			return DownloadFiles(localDir, remotePaths.ToArray(), overwrite);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Downloads the specified files into a local single directory.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// Same speed as <see cref="o:DownloadFile"/>.
        /// </summary>
        /// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
        /// <param name="remotePaths">The full or relative paths to the files on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="token">The token to monitor for cancelation requests</param>
        /// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
        public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite, CancellationToken token) {
            //check if cancellation was requested and throw to set TaskStatus state to Canceled
            token.ThrowIfCancellationRequested();
            int count = 0;

            // ensure ends with slash
            localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

            foreach (string remotePath in remotePaths) {
                //check if cancellation was requested and throw to set TaskStatus state to Canceled
                token.ThrowIfCancellationRequested();
                // calc local path
                string localPath = localDir + remotePath.GetFtpFileName();

                // try to download it
                try {
                    bool ok = await DownloadFileToFileAsync(localPath, remotePath, overwrite, token);
                    if (ok) {
                        count++;
                    }
                }
                catch (Exception ex) {
                    if (ex is OperationCanceledException) {
                        //DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
                        throw;
                    }
                    //suppress all other upload exceptions (errors are still written to FtpTrace)
                }
            }

            return count;
        }

        /// <summary>
        /// Downloads the specified files into a local single directory.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// Same speed as <see cref="o:DownloadFile"/>.
        /// </summary>
        /// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
        /// <param name="remotePaths">The full or relative paths to the files on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
        public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite = true) {
            return await DownloadFilesAsync(localDir, remotePaths, overwrite, CancellationToken.None);
        }
#endif

#endregion

		#region File Upload/Download

		/// <summary>
		/// Uploads the specified file directly onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public bool UploadFile(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {

			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				return false;
			}

			return UploadFileFromFile(localPath, remotePath, createRemoteDir, existsMode, false, false);
		}

#if (CORE || NETFX45)

        /// <summary>
        /// Uploads the specified file directly onto the server asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="localPath">The full or relative path to the file on the local file system</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token) {
            // skip uploading if the local file does not exist
            if (!File.Exists(localPath)) {
                return false;
            }

            return await UploadFileFromFileAsync(localPath, remotePath, createRemoteDir, existsMode, false, false, token);
        }
        
        /// <summary>
		/// Uploads the specified file directly onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {
			return await UploadFileAsync(localPath, remotePath, existsMode, createRemoteDir, CancellationToken.None);
        }
#endif

		private bool UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown) {
			FileStream fileStream;
			try {
				// connect to the file
				fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);

			} catch (Exception ex1) {
				// catch errors opening file stream
				throw new FtpException("Error while reading the file from the disk. See InnerException for more info.", ex1);
			}

			// write the file onto the server
			using (fileStream) {
				return UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown);
			}
		}

#if (CORE || NETFX45)
	    private async Task<bool> UploadFileFromFileAsync(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, CancellationToken token) {
	        FileStream fileStream;
	        try {
	            //Connect to the file
	            fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
	        }
	        catch (Exception ex1) {
                // catch errors opening file stream
                throw new FtpException("Error while reading the file from the disk. See InnerException for more info.", ex1);
	        }

	        using (fileStream) {
	            return await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, token);
	        }
	    }
#endif
		
		/// <summary>
		/// Uploads the specified stream as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileStream">The full data of the file, as a stream</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		public bool Upload(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {

			// write the file onto the server
			return UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, false, false);
		}
		/// <summary>
		/// Uploads the specified byte array as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		public bool Upload(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {

			// write the file onto the server
			using (MemoryStream ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return UploadFileInternal(ms, remotePath, createRemoteDir, existsMode, false, false);
			}
		}


#if (CORE || NETFX45)

        /// <summary>
        /// Uploads the specified stream as a file onto the server asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileStream">The full data of the file, as a stream</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
         /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token) {
            
            // write the file onto the server
            return await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, false, false, token);
        }
        /// <summary>
        /// Uploads the specified byte array as a file onto the server asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileData">The full data of the file, as a byte array</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token)
        {
            // write the file onto the server
            using (MemoryStream ms = new MemoryStream(fileData)) {
                ms.Position = 0;
				return await UploadFileInternalAsync(ms, remotePath, createRemoteDir, existsMode, false, false, token);
            }
        }

        /// <summary>
        /// Uploads the specified stream as a file onto the server asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileStream">The full data of the file, as a stream</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {
			return await UploadAsync(fileStream, remotePath, existsMode, createRemoteDir, CancellationToken.None);
        }

        /// <summary>
        /// Uploads the specified byte array as a file onto the server asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it uploads data in chunks.
        /// </summary>
        /// <param name="fileData">The full data of the file, as a byte array</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
        /// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {
			return await UploadAsync(fileData, remotePath, existsMode, createRemoteDir, CancellationToken.None);
        }
#endif

		/// <summary>
		/// Downloads the specified file onto the local file system.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool DownloadFile(string localPath, string remotePath, bool overwrite = true) {
			return DownloadFileToFile(localPath, remotePath, overwrite);
		}

		private bool DownloadFileToFile(string localPath, string remotePath, bool overwrite) {

			// skip downloading if the local file exists
			if (!overwrite && File.Exists(localPath)) {
				return false;
			}

			FileStream outStream;
			try {

				// create the folders
				string dirPath = Path.GetDirectoryName(localPath);
				if (!Directory.Exists(dirPath)) {
					Directory.CreateDirectory(dirPath);
				}

				// connect to the file
				outStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);

			} catch (Exception ex1) {

				// catch errors creating local file
				throw new FtpException("Error while saving the file to disk. See InnerException for more info.", ex1);
			}

			// download the file straight to a file stream
			using (outStream) {
				return DownloadFileInternal(remotePath, outStream);
			}
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Downloads the specified file onto the local file system asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="localPath">The full or relative path to the file on the local file system</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <param name="token">The token to monitor for cancellation requests</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        public async Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite, CancellationToken token) {
            return await DownloadFileToFileAsync(localPath, remotePath, overwrite, token);
        }

        /// <summary>
        /// Downloads the specified file onto the local file system asynchronously.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="localPath">The full or relative path to the file on the local file system</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        public async Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite = true) {
            return await DownloadFileToFileAsync(localPath, remotePath, overwrite, CancellationToken.None);
        }

        private async Task<bool> DownloadFileToFileAsync(string localPath, string remotePath, bool overwrite, CancellationToken token) {
            if (string.IsNullOrWhiteSpace(localPath))
                throw new ArgumentNullException("localPath");

            // skip downloading if the local file exists
            if (!overwrite && File.Exists(localPath)) {
                return false;
            }

            FileStream outStream;
            try {
                // create the folders
                string dirPath = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }

                // connect to the file
                outStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);

            }
            catch (Exception ex1){
                // catch errors creating local file
                throw new FtpException("Error while saving the file to disk. See InnerException for more info.", ex1);
            }

            // download the file straight to a file stream
            using (outStream) {
                return await DownloadFileInternalAsync(remotePath, outStream, token);
            }
        }
#endif

		/// <summary>
        /// Downloads the specified file into the specified stream.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(Stream outStream, string remotePath) {
			// download the file from the server
			return DownloadFileInternal(remotePath, outStream);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(out byte[] outBytes, string remotePath) {

			outBytes = null;

			// download the file from the server
			bool ok;
			using (MemoryStream outStream = new MemoryStream()) {
				ok = DownloadFileInternal(remotePath, outStream);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}
			return ok;
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Downloads the specified file into the specified stream asynchronously .
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="token">The token to monitor cancellation requests</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        public async Task<bool> DownloadAsync(Stream outStream, string remotePath, CancellationToken token) {
            // download the file from the server
            return await DownloadFileInternalAsync(remotePath, outStream, token);
        }

        /// <summary>
        /// Downloads the specified file into the specified stream asynchronously .
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <returns>If true then the file was downloaded, false otherwise.</returns>
        public async Task<bool> DownloadAsync(Stream outStream, string remotePath) {
            // download the file from the server
            return await DownloadFileInternalAsync(remotePath, outStream, CancellationToken.None);
        }

        /// <summary>
        /// Downloads the specified file and return the raw byte array.
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="token">The token to monitor cancellation requests</param>
        /// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
        public async Task<byte[]> DownloadAsync(string remotePath, CancellationToken token) {
            // download the file from the server
            using (MemoryStream outStream = new MemoryStream()) {
                bool ok = await DownloadFileInternalAsync(remotePath, outStream, token);
                return ok ? outStream.ToArray() : null;
            }
        }

        /// <summary>
        /// Downloads the specified file into the specified stream asynchronously .
        /// High-level API that takes care of various edge cases internally.
        /// Supports very large files since it downloads data in chunks.
        /// </summary>
        /// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
        public async Task<byte[]> DownloadAsync(string remotePath) {
            // download the file from the server
            return await DownloadAsync(remotePath, CancellationToken.None);
        }
#endif

		/// <summary>
		/// Upload the given stream to the server as a new file. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool UploadFileInternal(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown) {
			Stream upStream = null;

			try {

				long offset = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode != FtpExists.NoCheck) {
					if (!fileExistsKnown) {
						fileExists = FileExists(remotePath);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								return false;
							}
							break;
						case FtpExists.Overwrite:
							if (fileExists) {
								DeleteFile(remotePath);
							}
							break;
						case FtpExists.Append:
							if (fileExists) {
								offset = GetFileSize(remotePath);
								if (offset == -1) {
									offset = 0; // start from the beginning
								}
							}
							break;
					}
				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					string dirname = remotePath.GetFtpDirectoryName();
					if (!DirectoryExists(dirname)) {
						CreateDirectory(dirname);
					}
				}

				// seek to offset
				fileData.Position = offset;

				// open a file connection
				if (offset == 0) {
					upStream = OpenWrite(remotePath);
				} else {
					upStream = OpenAppend(remotePath);
				}

				// loop till entire file uploaded
				long len = fileData.Length;
				byte[] buffer = new byte[TransferChunkSize];
				while (offset < len) {
					try {

						// read a chunk of bytes from the file
						int readBytes;
						while ((readBytes = fileData.Read(buffer, 0, buffer.Length)) > 0) {

							// write chunk to the FTP stream
							upStream.Write(buffer, 0, readBytes);
							upStream.Flush();
							offset += readBytes;
						}

					} catch (IOException ex) {

						// resume if server disconnects midway (fixes #39)
						if (ex.InnerException != null) {
							var iex = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
							int code = (int)iex.SocketErrorCode;
#else
							int code = iex.ErrorCode;
#endif
							if (iex != null && code == 10054) {
								upStream.Dispose();
								upStream = OpenAppend(remotePath);
								upStream.Position = offset;
							} else throw;
						} else throw;

					}
				}

				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!EnableThreadSafeDataConnections) {
					FtpReply status = GetReply();
				}

				return true;

			} catch (Exception ex1) {

				// close stream before throwing error
				try {
					if (upStream != null)
						upStream.Dispose();
				} catch (Exception) { }

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Upload the given stream to the server as a new file asynchronously. Overwrites the file if it exists.
        /// Writes data in chunks. Retries if server disconnects midway.
        /// </summary>
		private async Task<bool> UploadFileInternalAsync(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, CancellationToken token) {
			Stream upStream = null;
			try {
				long offset = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode != FtpExists.NoCheck) {
					if (!fileExistsKnown) {
						fileExists = await FileExistsAsync(remotePath);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								return false;
							}
							break;
						case FtpExists.Overwrite:
							if (fileExists) {
								await DeleteFileAsync(remotePath);
							}
							break;
						case FtpExists.Append:
							if (fileExists) {
								offset = await GetFileSizeAsync(remotePath);
								if (offset == -1) {
									offset = 0; // start from the beginning
								}
							}
							break;
					}
				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					string dirname = remotePath.GetFtpDirectoryName();
					if (!await DirectoryExistsAsync(dirname)) {
						await CreateDirectoryAsync(dirname);
					}
				}

				// seek to offset
				fileData.Position = offset;

				// open a file connection
				if (offset == 0) {
					upStream = await OpenWriteAsync(remotePath);
				} else {
					upStream = await OpenAppendAsync(remotePath);
				}

				// loop till entire file uploaded
				long len = fileData.Length;
				byte[] buffer = new byte[TransferChunkSize];
				while (offset < len) {
					try {
						// read a chunk of bytes from the file
						int readBytes;
						while ((readBytes = await fileData.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
							// write chunk to the FTP stream
							await upStream.WriteAsync(buffer, 0, readBytes, token);
							await upStream.FlushAsync(token);
							offset += readBytes;
						}
					} catch (IOException ex) {
						// resume if server disconnects midway (fixes #39)
						if (ex.InnerException != null) {
							var iex = ex.InnerException as System.Net.Sockets.SocketException;

							if (iex != null) {
#if CORE
							    int code = (int)iex.SocketErrorCode;
#else
								int code = iex.ErrorCode;
#endif
								if (code == 10054) {
									upStream.Dispose();
									//Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
									upStream = OpenAppend(remotePath);
									upStream.Position = offset;
								}
							} else throw;
						} else throw;
					}
				}

				// wait for while transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = GetReply();
				}

				return true;
			} catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null)
						upStream.Dispose();
				} catch (Exception) { }

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}
#endif
		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool DownloadFileInternal(string remotePath, Stream outStream) {

			Stream downStream = null;

			try {

				// exit if file length not available
				downStream = OpenRead(remotePath);
				long fileLen = downStream.Length;
				if (fileLen == 0 && CurrentDataType == FtpDataType.ASCII) {

					// close stream before throwing error
					try {
						downStream.Dispose();
					} catch (Exception) { }

					throw new FtpException("Cannot download file since file has length of 0. Use the FtpDataType.Binary data type and try again.");
				}


				// if the server has not reported a length for this file
				// we use an alternate method to download it - read until EOF
				bool readToEnd = (fileLen == 0);


				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
				long offset = 0;
				while (offset < fileLen || readToEnd) {
					try {

						// read a chunk of bytes from the FTP stream
						int readBytes = 1;
						while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {

							// write chunk to output stream
							outStream.Write(buffer, 0, readBytes);
							offset += readBytes;
						}

						// if we reach here means EOF encountered
						// stop if we are in "read until EOF" mode
						if (readToEnd) {
							break;
						}

					} catch (IOException ex) {

						// resume if server disconnects midway (fixes #39)
						if (ex.InnerException != null) {
							var ie = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
							int code = (int)ie.SocketErrorCode;
#else
							int code = ie.ErrorCode;
#endif
							if (ie != null && code == 10054) {
								downStream.Dispose();
								downStream = OpenRead(remotePath, restart: offset);
							} else throw;
						} else throw;

					}

				}


				// disconnect FTP stream before exiting
				outStream.Flush();
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = GetReply();
				}
				return true;


			} catch (Exception ex1) {

				// close stream before throwing error
				try {
					downStream.Dispose();
				} catch (Exception) { }

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

#if (CORE || NETFX45)
        /// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> DownloadFileInternalAsync(string remotePath, Stream outStream, CancellationToken token) {
			Stream downStream = null;
			try {
				// exit if file length not available
				downStream = await OpenReadAsync(remotePath);
				long fileLen = downStream.Length;
				if (fileLen == 0 && CurrentDataType == FtpDataType.ASCII) {
					// close stream before throwing error
					try {
						downStream.Dispose();
					} catch (Exception) { }

					throw new FtpException("Cannot download file since file has length of 0. Use the FtpDataType.Binary data type and try again.");
				}

				// if the server has not reported a length for this file
				// we use an alternate method to download it - read until EOF
				bool readToEnd = (fileLen == 0);

				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
				long offset = 0;
				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						int readBytes = 1;
						while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
							// write chunk to output stream
							await outStream.WriteAsync(buffer, 0, readBytes, token);
							offset += readBytes;
						}

						// if we reach here means EOF encountered
						// stop if we are in "read until EOF" mode
						if (readToEnd) {
							break;
						}

					} catch (IOException ex) {

						// resume if server disconnects midway (fixes #39)
						if (ex.InnerException != null) {
							var ie = ex.InnerException as System.Net.Sockets.SocketException;
							if (ie != null) {
#if CORE
		    					int code = (int)ie.SocketErrorCode;
#else
                                int code = ie.ErrorCode;
#endif
							    if (code == 10054) {
                                    downStream.Dispose();
                                    //Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
                                    downStream = OpenRead(remotePath, restart: offset);
							    }
							} else throw;
						} else throw;

					}

				}


				// disconnect FTP stream before exiting
				await outStream.FlushAsync(token);
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = GetReply();
				}
				return true;

			} 
            catch (Exception ex1) {
				// close stream before throwing error
				try {
					downStream.Dispose();
				} catch (Exception) { }

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}
#endif

#endregion

		#region Delete File

		/// <summary>
		/// Deletes a file on the server
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <example><code source="..\Examples\DeleteFile.cs" lang="cs" /></example>
		public void DeleteFile(string path) {
			FtpReply reply;

			lock (m_lock) {
				if (!(reply = Execute("DELE " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
			}
		}

		delegate void AsyncDeleteFile(string path);

		/// <summary>
        /// Begins an asynchronous operation to delete the specified file on the server
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteFile(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDeleteFile func;

			ar = (func = new AsyncDeleteFile(DeleteFile)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="BeginDeleteFile"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDeleteFile</param>
		/// <example><code source="..\Examples\BeginDeleteFile.cs" lang="cs" /></example>
		public void EndDeleteFile(IAsyncResult ar) {
			GetAsyncDelegate<AsyncDeleteFile>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Deletes a file from the server asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
	    public async Task DeleteFileAsync(string path) {
	        await Task.Factory.FromAsync<string>(
	            (p, ac, s) => BeginDeleteFile(p, ac, s),
	            ar => EndDeleteFile(ar),
	            path, null);
	    }
#endif

		#endregion

		#region Delete Directory

		/// <summary>
		/// Deletes the specified directory on the server.
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path, bool fastMode = false) {
			DeleteDirectory(path, false, 0, fastMode);
		}

		/// <summary>
		/// Deletes the specified directory on the server
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="force">If the directory is not empty, remove its contents</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path, bool force, bool fastMode = false) {
			DeleteDirectory(path, force, 0, fastMode);
		}

		/// <summary>
		/// Deletes the specified directory on the server
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="force">If the directory is not empty, remove its contents</param>
		/// <param name="options"><see cref="FtpListOption"/> for controlling how the directory's
		/// contents are retrieved with the force option is true. If you experience problems
		/// the file listing can be fine tuned through this parameter.</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <example><code source="..\Examples\DeleteDirectory.cs" lang="cs" /></example>
		public void DeleteDirectory(string path, bool force, FtpListOption options, bool fastMode = false) {
			FtpReply reply;
			string ftppath = path.GetFtpPath();


			lock (m_lock) {



				// DELETE CONTENTS OF THE DIRECTORY
				if (force) {

					// experimental fast mode
					if (fastMode) {

						// when GetListing is called with recursive option, then it does not
						// make any sense to call another DeleteDirectory with force flag set.
						// however this requires always delete files first.
						var forceAgain = !WasGetListingRecursive(options);

						// items, that are deeper in directory tree, are listed first, 
						// then files will  be listed before directories. This matters
						// only if GetListing was called with recursive option.
						FtpListItem[] itemList;
						if (forceAgain)
							itemList = GetListing(path, options);
						else
							itemList = GetListing(path, options).OrderByDescending(x => x.FullName.Count(c => c.Equals('/'))).ThenBy(x => x.Type).ToArray();


						foreach (FtpListItem item in itemList) {
							switch (item.Type) {
								case FtpFileSystemObjectType.File:
									DeleteFile(item.FullName);
									break;
								case FtpFileSystemObjectType.Directory:
									DeleteDirectory(item.FullName, forceAgain, options, fastMode);
									break;
								default:
									throw new FtpException("Don't know how to delete object type: " + item.Type);
							}
						}
					} else {

						// standard mode
						foreach (FtpListItem item in GetListing(path, options)) {

							// This check prevents infinity recursion, 
							// when FtpListItem is actual parent or current directory.
							// This could happen only when MLSD command is used for GetListing method.
							if (!item.FullName.ToLower().Contains(path.ToLower()) || string.Equals(item.FullName.ToLower(), path.ToLower()))
								continue;

							switch (item.Type) {
								case FtpFileSystemObjectType.File:
									DeleteFile(item.FullName);
									break;
								case FtpFileSystemObjectType.Directory:
									DeleteDirectory(item.FullName, true, options, fastMode);
									break;
								default:
									throw new FtpException("Don't know how to delete object type: " + item.Type);
							}
						}
					}
				}



				// SKIP DELETING ROOT DIRS

				// can't delete the working directory and
				// can't delete the server root.
				if (ftppath == "." || ftppath == "./" || ftppath == "/") {
					return;
				}



				// DELETE ACTUAL DIRECTORY

				if (!(reply = Execute("RMD " + ftppath)).Success) {
					throw new FtpCommandException(reply);
				}

			}
		}

		/// <summary>
		/// Checks whether <see cref="o:GetListing"/> will be called recursively or not.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		private bool WasGetListingRecursive(FtpListOption options) {
			if (HasFeature(FtpCapability.MLSD) && (options & FtpListOption.ForceList) != FtpListOption.ForceList)
				return false;

			if ((options & FtpListOption.UseLS) == FtpListOption.UseLS || (options & FtpListOption.NameList) == FtpListOption.NameList)
				return false;

			if ((options & FtpListOption.Recursive) == FtpListOption.Recursive)
				return true;

			return false;
		}

		delegate void AsyncDeleteDirectory(string path, bool force, FtpListOption options, bool fastMode = false);

		/// <summary>
        /// Begins an asynchronous operation to delete the specified directory from the server
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteDirectory(string path, AsyncCallback callback, object state, bool fastMode = false) {
			return BeginDeleteDirectory(path, true, 0, fastMode, callback, state);
		}

		/// <summary>
        /// Begins an asynchronous operation to delete the specified directory from the server
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="force">If the directory is not empty, then remove its contents</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteDirectory(string path, bool force, AsyncCallback callback, object state, bool fastMode = false) {
			return BeginDeleteDirectory(path, force, 0, fastMode, callback, state);
		}

		/// <summary>
        /// Begins an asynchronous operation to delete the specified directory from the server
		/// </summary>
		/// <param name="path">The full or relative path of the directory to delete</param>
		/// <param name="force">If the directory is not empty, remove its contents</param>
		/// <param name="options"><see cref="FtpListOption"/>s for controlling how the directory's
		/// contents are retrieved when the force option is true. If you experience problems
		/// the file listing can be fine tuned through this parameter.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginDeleteDirectory(string path, bool force, FtpListOption options, bool fastMode, AsyncCallback callback, object state) {
			AsyncDeleteDirectory func;
			IAsyncResult ar;

			ar = (func = new AsyncDeleteDirectory(DeleteDirectory)).BeginInvoke(path, force, options, fastMode, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginDeleteDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDeleteDirectory</param>
		/// <example><code source="..\Examples\BeginDeleteDirectory.cs" lang="cs" /></example>
		public void EndDeleteDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncDeleteDirectory>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, remove its contents</param>
        /// <param name="options"><see cref="FtpListOption"/>s for controlling how the directory's
        /// contents are retrieved when the force option is true. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
	    public async Task DeleteDirectoryAsync(string path, bool force, FtpListOption options, bool fastMode = false) {
	        var throwAway = await Task.Factory.FromAsync<string, bool, FtpListOption, bool, bool>(
	            (p, f, o, fm, ac, s) => BeginDeleteDirectory(p, f, o, fm, ac, s),
	            ar => {
	                var invoked = GetAsyncDelegate<AsyncDeleteDirectory>(ar);
	                if (invoked != null) {
	                    invoked.EndInvoke(ar);
                        return true;
	                }

                    return false;
	            },
                path, force, options, fastMode, null);
	    }

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="force">If the directory is not empty, then remove its contents.
        /// Contents are retrieved when the force option is true. If you experience problems
        /// the file listing can be fine tuned through this parameter.</param>
        /// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
	    public async Task DeleteDirectoryAsync(string path, bool force, bool fastMode) {
	        await Task.Factory.FromAsync<string, bool, bool>(
	            (p, f, fm, ac, s) => BeginDeleteDirectory(p, f, ac, s, fm),
	            ar => EndDeleteDirectory(ar),
	            path, force, fastMode, null);
	    }

        /// <summary>
        /// Asynchronously removes a directory from the server
        /// </summary>
        /// <param name="path">The full or relative path of the directory to delete</param>
        /// <param name="fastMode">An experimental fast mode that file listing is only requested for once. This improves bandwidth usage and response time.</param>
        public async Task DeleteDirectoryAsync(string path, bool fastMode = false){
            await Task.Factory.FromAsync<string, bool>(
                (p, fm, ac, s) => BeginDeleteDirectory(p, ac, s, fm),
                ar => EndDeleteDirectory(ar),
                path, fastMode, null);
        }
#endif

		#endregion

		#region Directory Exists

		/// <summary>
		/// Tests if the specified directory exists on the server. This
		/// method works by trying to change the working directory to
		/// the path specified. If it succeeds, the directory is changed
		/// back to the old working directory and true is returned. False
		/// is returned otherwise and since the CWD failed it is assumed
		/// the working directory is still the same.
		/// </summary>
		/// <param name="path">The path of the directory</param>
		/// <returns>True if it exists, false otherwise.</returns>
		/// <example><code source="..\Examples\DirectoryExists.cs" lang="cs" /></example>
		public bool DirectoryExists(string path) {
			string pwd;

			// quickly check if root path, then it always exists!
			string ftppath = path.GetFtpPath();
			if (ftppath == "." || ftppath == "./" || ftppath == "/") {
				return true;
			}

			// check if a folder exists by changing the working dir to it
			lock (m_lock) {
				pwd = GetWorkingDirectory();

				if (Execute("CWD " + ftppath).Success) {
					FtpReply reply = Execute("CWD " + pwd.GetFtpPath());

					if (!reply.Success)
						throw new FtpException("DirectoryExists(): Failed to restore the working directory.");

					return true;
				}
			}

			return false;
		}

		delegate bool AsyncDirectoryExists(string path);

		/// <summary>
        /// Begins an asynchronous operation to test if the specified directory exists on the server. 
        /// This method works by trying to change the working directory to
        /// the path specified. If it succeeds, the directory is changed
        /// back to the old working directory and true is returned. False
        /// is returned otherwise and since the CWD failed it is assumed
        /// the working directory is still the same.
		/// </summary>
		/// <returns>IAsyncResult</returns>
		/// <param name='path'>The full or relative path of the directory to check for</param>
		/// <param name='callback'>Async callback</param>
		/// <param name='state'>State object</param>
		/// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
		public IAsyncResult BeginDirectoryExists(string path, AsyncCallback callback, object state) {
			AsyncDirectoryExists func;
			IAsyncResult ar;

			ar = (func = new AsyncDirectoryExists(DirectoryExists)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginDirectoryExists"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from BeginDirectoryExists</param>
		/// <returns>True if the directory exists. False otherwise.</returns>
		/// <example><code source="..\Examples\BeginDirectoryExists.cs" lang="cs" /></example>
		public bool EndDirectoryExists(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncDirectoryExists>(ar).EndInvoke(ar);
		}

#if (CORE ||NETFX45)
        /// <summary>
        /// Tests if the specified directory exists on the server asynchronously. This
        /// method works by trying to change the working directory to
        /// the path specified. If it succeeds, the directory is changed
        /// back to the old working directory and true is returned. False
        /// is returned otherwise and since the CWD failed it is assumed
        /// the working directory is still the same.
        /// </summary>
        /// <param name='path'>The full or relative path of the directory to check for</param>
        /// <returns>True if the directory exists. False otherwise.</returns>
	    public async Task<bool> DirectoryExistsAsync(string path) {
	        return await Task.Factory.FromAsync<string, bool>(
	            (p, ac, s) => BeginDirectoryExists(p, ac, s),
	            ar => EndDirectoryExists(ar),
	            path, null);
	    }
#endif

		#endregion

		#region File Exists

		/// <summary>
		/// Checks if a file exists on the server.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <returns>True if the file exists</returns>
		/// <example><code source="..\Examples\FileExists.cs" lang="cs" /></example>
		public bool FileExists(string path) {

			lock (m_lock) {

				// calc the absolute filepath
				path = GetAbsolutePath(path.GetFtpPath());

				// since FTP does not include a specific command to check if a file exists
				// here we check if file exists by attempting to get its filesize (SIZE)
				if (HasFeature(FtpCapability.SIZE)) {
					FtpReply reply = Execute("SIZE " + path);
					char ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}
					if (ch == '5' && IsNotFoundError(reply.Message)) {
						return false;
					}
				}

				// check if file exists by attempting to get its date modified (MDTM)
				if (HasFeature(FtpCapability.MDTM)) {
					FtpReply reply = Execute("MDTM " + path);
					char ch = reply.Code[0];
					if (ch == '2') {
						return true;
					}
					if (ch == '5' && IsNotFoundError(reply.Message)) {
						return false;
					}
				}

				// check if file exists by getting a name listing (NLST)
				string[] fileList = GetNameListing(path.GetFtpDirectoryName());
				string pathName = path.GetFtpFileName();
				if (fileList.Contains(pathName)) {
					return true;
				}

				// check if file exists by attempting to download it (RETR)
				/*try {
					Stream stream = OpenRead(path);
					stream.Close();
					return true;
				} catch (FtpException ex) {
				}*/

				return false;
			}
		}

		private static string[] notFoundStrings = new string[] { "can't check for file existence", "does not exist", "failed to open file", "not found", "no such file", "cannot find the file", "cannot find", "could not get file", "not a regular file" };
		private bool IsNotFoundError(string reply) {
			reply = reply.ToLower();
			foreach (string msg in notFoundStrings) {
				if (reply.Contains(msg)) {
					return true;
				}
			}
			return false;
		}

		delegate bool AsyncFileExists(string path);

		/// <summary>
		/// Begins an asynchronous operation to check if a file exists on the 
		/// server by taking a  file listing of the parent directory in the path
		/// and comparing the results the path supplied.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
		public IAsyncResult BeginFileExists(string path, AsyncCallback callback, object state) {
			return BeginFileExists(path, callback, state);
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginFileExists"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="o:BeginFileExists"/></param>
		/// <returns>True if the file exists, false otherwise</returns>
		/// <example><code source="..\Examples\BeginFileExists.cs" lang="cs" /></example>
		public bool EndFileExists(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncFileExists>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Checks if a file exists on the server asynchronously by taking a 
        /// file listing of the parent directory in the path
        /// and comparing the results the path supplied.
        /// </summary>
        /// <param name="path">The full or relative path to the file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public async Task<bool> FileExistsAsync(string path)
        {
            return await Task.Factory.FromAsync<string, bool>(
                (p, ac, s) => BeginFileExists(p, ac, s),
                ar => EndFileExists(ar),
                path, null);
        }
#endif

		#endregion

		#region Create Directory

		/// <summary>
		/// Creates a directory on the server. If the preceding
		/// directories do not exist, then they are created.
		/// </summary>
		/// <param name="path">The full or relative path to the new remote directory</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		public void CreateDirectory(string path) {
			CreateDirectory(path, true);
		}

		/// <summary>
		/// Creates a directory on the server
		/// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to force all non-existent pieces of the path to be created</param>
		/// <example><code source="..\Examples\CreateDirectory.cs" lang="cs" /></example>
		public void CreateDirectory(string path, bool force) {
			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./" || ftppath == "/")
				return;

			lock (m_lock) {
				path = path.GetFtpPath().TrimEnd('/');

				if (force && !DirectoryExists(path.GetFtpDirectoryName())) {
					FtpTrace.WriteLine("CreateDirectory(\"" +path+ "\", " +force+ "): Create non-existent parent: " +path.GetFtpDirectoryName());
					CreateDirectory(path.GetFtpDirectoryName(), true);
				} else if (DirectoryExists(path))
					return;

				FtpTrace.WriteLine("CreateDirectory(\"" + ftppath + "\", " + force + ")");
				
				if (!(reply = Execute("MKD " + ftppath)).Success)
					throw new FtpCommandException(reply);
			}
		}

		delegate void AsyncCreateDirectory(string path, bool force);

		/// <summary>
		/// Begins an asynchronous operation to create a remote directory. If the preceding
        /// directories do not exist, then they are created.
		/// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginCreateDirectory(string path, AsyncCallback callback, object state) {
			return BeginCreateDirectory(path, true, callback, state);
		}

		/// <summary>
        /// Begins an asynchronous operation to create a remote directory
		/// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
		/// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginCreateDirectory(string path, bool force, AsyncCallback callback, object state) {
			AsyncCreateDirectory func;
			IAsyncResult ar;

			ar = (func = new AsyncCreateDirectory(CreateDirectory)).BeginInvoke(path, force, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginCreateDirectory"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="o:BeginCreateDirectory"/></param>
		/// <example><code source="..\Examples\BeginCreateDirectory.cs" lang="cs" /></example>
		public void EndCreateDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncCreateDirectory>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Creates a remote directory asynchronously
        /// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
        /// <param name="force">Try to create the whole path if the preceding directories do not exist</param>
	    public async Task CreateDirectoryAsync(string path, bool force) {
	        await Task.Factory.FromAsync<string, bool>(
	            (p, f, ac, s) => BeginCreateDirectory(p, f, ac, s),
	            ar => EndCreateDirectory(ar),
	            path, force, null);
	    }

        /// <summary>
        /// Creates a remote directory asynchronously. If the preceding
        /// directories do not exist, then they are created.
        /// </summary>
        /// <param name="path">The full or relative path to the new remote directory</param>
        public async Task CreateDirectoryAsync(string path) {
            await Task.Factory.FromAsync<string>(
                (p, ac, s) => BeginCreateDirectory(p, ac, s),
                ar => EndCreateDirectory(ar),
                path, null);
        }
#endif

		#endregion

		#region Rename File/Directory

		/// <summary>
		/// Renames an object on the remote file system.
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The old or new full or relative path including the new name of the object</param>
		/// <example><code source="..\Examples\Rename.cs" lang="cs" /></example>
		public void Rename(string path, string dest) {
			FtpReply reply;

			lock (m_lock) {

				if (!(reply = Execute("RNFR " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);

				if (!(reply = Execute("RNTO " + dest.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
			}
		}

		delegate void AsyncRename(string path, string dest);

		/// <summary>
		/// Begins an asynchronous operation to rename an object on the remote file system
		/// </summary>
		/// <param name="path">The full or relative path to the object</param>
		/// <param name="dest">The old or new full or relative path including the new name of the object</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
		public IAsyncResult BeginRename(string path, string dest, AsyncCallback callback, object state) {
			AsyncRename func;
			IAsyncResult ar;

			ar = (func = new AsyncRename(Rename)).BeginInvoke(path, dest, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginRename"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginRename"/></param>
		/// <example><code source="..\Examples\BeginRename.cs" lang="cs" /></example>
		public void EndRename(IAsyncResult ar) {
			GetAsyncDelegate<AsyncRename>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
	    /// <summary>
	    /// Renames an object on the remote file system asynchronously
	    /// </summary>
	    /// <param name="path">The full or relative path to the object</param>
	    /// <param name="dest">The old or new full or relative path including the new name of the object</param>
	    public async Task RenameAsync(string path, string dest) {
	        await Task.Factory.FromAsync<string, string>(
	            (p, d, ac, s) => BeginRename(p, d, ac, s),
	            ar => EndRename(ar),
	            path, dest, null);
	    }
#endif

#endregion

		#region File Permissions / Chmod

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="permissions">The permissions in CHMOD format</param>
		public void SetFilePermissions(string path, int permissions) {
			FtpReply reply;

			lock (m_lock) {
				if (!(reply = Execute("SITE CHMOD " + permissions.ToString() + " " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
			}
		}

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="permissions">The permissions in CHMOD format</param>
		public void Chmod(string path, int permissions) {
			SetFilePermissions(path, permissions);
		}

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="owner">The owner permissions</param>
		/// <param name="group">The group permissions</param>
		/// <param name="other">The other permissions</param>
		public void SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other) {
			SetFilePermissions(path, CalcChmod(owner, group, other));
		}

		/// <summary>
		/// Modify the permissions of the given file/folder.
		/// Only works on *NIX systems, and not on Windows/IIS servers.
		/// Only works if the FTP server supports the SITE CHMOD command
		/// (requires the CHMOD extension to be installed and enabled).
		/// Throws FtpCommandException if there is an issue.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		/// <param name="owner">The owner permissions</param>
		/// <param name="group">The group permissions</param>
		/// <param name="other">The other permissions</param>
		public void Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other) {
			SetFilePermissions(path, owner, group, other);
		}

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set.
		/// Throws FtpCommandException if there is an issue.
		/// Returns null if the server did not specify a permission value.
		/// Use `GetChmod` if you required the integer value instead.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		public FtpListItem GetFilePermissions(string path) {
			string fullPath = path.GetFtpPath();
			foreach (FtpListItem i in GetListing(path)) {
				if (i.FullName == fullPath) {
					return i;
				}
			}
			return null;
		}

		/// <summary>
		/// Retrieve the permissions of the given file/folder as an integer in the CHMOD format.
		/// Throws FtpCommandException if there is an issue.
		/// Returns 0 if the server did not specify a permission value.
		/// Use `GetFilePermissions` if you required the permissions in the FtpPermission format.
		/// </summary>
		/// <param name="path">The full or relative path to the item</param>
		public int GetChmod(string path) {
			FtpListItem item = GetFilePermissions(path);
			return item != null ? item.Chmod : 0;
		}

#endregion

		#region Dereference Link

		/// <summary>
		/// Recursively dereferences a symbolic link. See the
		/// MaximumDereferenceCount property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		public FtpListItem DereferenceLink(FtpListItem item) {
			return DereferenceLink(item, MaximumDereferenceCount);
		}

		/// <summary>
		/// Recursively dereferences a symbolic link
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <param name="recMax">The maximum depth of recursion that can be performed before giving up.</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		public FtpListItem DereferenceLink(FtpListItem item, int recMax) {
			int count = 0;
			return DereferenceLink(item, recMax, ref count);
		}

		/// <summary>
		/// Derefence a FtpListItem object
		/// </summary>
		/// <param name="item">The item to derefence</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		FtpListItem DereferenceLink(FtpListItem item, int recMax, ref int count) {
			if (item.Type != FtpFileSystemObjectType.Link)
				throw new FtpException("You can only derefernce a symbolic link. Please verify the item type is Link.");

			if (item.LinkTarget == null)
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");

			foreach (FtpListItem obj in GetListing(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList)) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
						if (++count == recMax)
							return null;

						return DereferenceLink(obj, recMax, ref count);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						DateTime modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue)
							obj.Modified = modify;
					}

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE))
						obj.Size = GetFileSize(obj.FullName);

					return obj;
				}
			}

			return null;
		}

		delegate FtpListItem AsyncDereferenceLink(FtpListItem item, int recMax);

		/// <summary>
		/// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, int recMax, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDereferenceLink func;

			ar = (func = new AsyncDereferenceLink(DereferenceLink)).BeginInvoke(item, recMax, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object. See the
        /// <see cref="MaximumDereferenceCount"/> property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, AsyncCallback callback, object state) {
			return BeginDereferenceLink(item, MaximumDereferenceCount, callback, state);
		}

		/// <summary>
        /// Ends a call to <see cref="o:BeginDereferenceLink"/>
		/// </summary>
		/// <param name="ar">IAsyncResult</param>
		/// <returns>A <see cref="FtpListItem"/>, or null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public FtpListItem EndDereferenceLink(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncDereferenceLink>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Dereference a <see cref="FtpListItem"/> object asynchronously
        /// </summary>
        /// <param name="item">The item to dereference</param>
        /// <param name="recMax">Maximum recursive calls</param>
        /// <returns>FtpListItem, null if the link can't be dereferenced</returns>
	    public async Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<FtpListItem, int, FtpListItem>(
                (i, r, ac, s) => BeginDereferenceLink(i, r, ac, s),
                ar => EndDereferenceLink(ar),
                item, recMax, null);
        }

        /// <summary>
        /// Dereference a <see cref="FtpListItem"/> object asynchronously
        /// </summary>
        /// <param name="item">The item to dereference</param>
        /// <returns>FtpListItem, null if the link can't be dereferenced</returns>
        public async Task<FtpListItem> DereferenceLinkAsync(FtpListItem item){
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<FtpListItem, FtpListItem>(
                (i, ac, s) => BeginDereferenceLink(i, ac, s),
                ar => EndDereferenceLink(ar),
                item, null);
        }
#endif

#endregion

		#region Get File Info

		/// <summary>
		/// Returns information about a file system object. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefullness of this method for checking for the existence of an object.
		/// </summary>
		/// <param name="path">The path of the file or folder</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <returns>A FtpListItem object</returns>
		public FtpListItem GetObjectInfo(string path, bool dateModified = false) {
			FtpReply reply;
			string[] res;

			bool supportsMachineList = (Capabilities & FtpCapability.MLSD) == FtpCapability.MLSD;
			
			FtpListItem result = null;

			if (supportsMachineList) {

				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = Execute("MLST " + path)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						string info = "";

						for (int i = 1; i < res.Length; i++) {
							info += res[i];
						}

						result = m_listParser.ParseSingleLine(null, info, m_caps, true);
					}
				} else {
					FtpTrace.WriteLine("Failed to get object info for path " + path + " with error "+ reply.ErrorMessage);
				}
			} else {

				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				string dirPath = path.GetFtpDirectoryName();
				FtpListItem[] dirItems = GetListing(dirPath);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				FtpTrace.WriteLine("Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)){
				result.Modified = GetModifiedTime(path);
			}

			return result;
		}

		delegate FtpListItem AsyncGetObjectInfo(string path, bool dateModified);

		/// <summary>
		/// Begins an asynchronous operation to return information about a remote file system object. 
		/// </summary>
		/// <remarks>
        /// You should check the <see cref="Capabilities"/> property for the <see cref="FtpCapability.MLSD"/> 
        /// flag before calling this method. Failing to do so will result in an InvalidOperationException
        ///  being thrown when the server does not support machine listings. Returns null if the server response can't
        /// be parsed or the server returns a failure completion code. The error for a failure
        /// is logged with FtpTrace. No exception is thrown on error because that would negate
        /// the usefulness of this method for checking for the existence of an object.
		/// </remarks>
		/// <param name="path">Path of the file or folder</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetObjectInfo(string path, bool dateModified, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetObjectInfo func;

			ar = (func = new AsyncGetObjectInfo(GetObjectInfo)).BeginInvoke(path, dateModified, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="BeginGetObjectInfo"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginGetObjectInfo"/></param>
        /// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public FtpListItem EndGetObjectInfo(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetObjectInfo>(ar).EndInvoke(ar);
		}
		
#if (CORE || NETFX45)
        /// <summary>
        /// Return information about a remote file system object asynchronously. 
        /// </summary>
        /// <remarks>
        /// You should check the <see cref="Capabilities"/> property for the <see cref="FtpCapability.MLSD"/> 
        /// flag before calling this method. Failing to do so will result in an InvalidOperationException
        /// being thrown when the server does not support machine listings. Returns null if the server response can't
        /// be parsed or the server returns a failure completion code. The error for a failure
        /// is logged with FtpTrace. No exception is thrown on error because that would negate
        /// the usefulness of this method for checking for the existence of an object.</remarks>
        /// <param name="path">Path of the item to retrieve information about</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <exception cref="InvalidOperationException">Thrown if the server does not support this Capability</exception>
        /// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public async Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, bool, FtpListItem>(
	            (p, dm, ac, s) => BeginGetObjectInfo(p, dm, ac, s),
	            ar => EndGetObjectInfo(ar),
	            path, dateModified, null);
	    }
#endif

		#endregion

		#region Get Listing

		/// <summary>
        /// Gets a file listing from the server from the current working directory. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
		/// <returns>An array of FtpListItem objects</returns>
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
		public FtpListItem[] GetListing() {
			return GetListing(null);
		}

        /// <summary>
        /// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>An array of FtpListItem objects</returns>
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
		public FtpListItem[] GetListing(string path) {
			return GetListing(path, 0);
		}

        /// <summary>
        /// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictacte how a list is performed and what information is gathered.</param>
		/// <returns>An array of FtpListItem objects</returns>
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
		public FtpListItem[] GetListing(string path, FtpListOption options) {
			FtpListItem item = null;
			List<FtpListItem> lst = new List<FtpListItem>();
			List<string> rawlisting = new List<string>();
			string listcmd = null;
			string buf = null;

			// read flags
			bool isIncludeSelf = (options & FtpListOption.IncludeSelfAndParent) == FtpListOption.IncludeSelfAndParent;
			bool isForceList = (options & FtpListOption.ForceList) == FtpListOption.ForceList;
			bool isNoPath = (options & FtpListOption.NoPath) == FtpListOption.NoPath;
			bool isNameList = (options & FtpListOption.NameList) == FtpListOption.NameList;
			bool isUseLS = (options & FtpListOption.UseLS) == FtpListOption.UseLS;
			bool isAllFiles = (options & FtpListOption.AllFiles) == FtpListOption.AllFiles;
			bool isRecursive = (options & FtpListOption.Recursive) == FtpListOption.Recursive;
			bool isDerefLinks = (options & FtpListOption.DerefLinks) == FtpListOption.DerefLinks;
			bool isGetModified = (options & FtpListOption.Modify) == FtpListOption.Modify;
			bool isGetSize = (options & FtpListOption.Size) == FtpListOption.Size;

			// calc path to request
			path = GetAbsolutePath(path);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList = false;
			if ((!isForceList || m_parser == FtpParser.Machine) && HasFeature(FtpCapability.MLSD)) {
				listcmd = "MLSD";
				machineList = true;
			} else {
				if (isUseLS) {
					listcmd = "LS";
				} else if (isNameList) {
					listcmd = "NLST";
				} else {
					string listopts = "";

					listcmd = "LIST";

					if (isAllFiles)
						listopts += "a";

					if (isRecursive)
						listopts += "R";

					if (listopts.Length > 0)
						listcmd += " -" + listopts;
				}
			}

			if (!isNoPath) {
				listcmd = (listcmd + " " + path.GetFtpPath());
			}

			lock (m_lock) {
				Execute("TYPE I");

				// read in raw file listing
				using (FtpDataStream stream = OpenDataStream(listcmd, 0)) {
					try {
						while ((buf = stream.ReadLine(Encoding)) != null) {
							if (buf.Length > 0) {
								rawlisting.Add(buf);
								FtpTrace.WriteLine(buf);
							}
						}
					} finally {
						stream.Close();
					}
				}
			}

			for (int i = 0; i < rawlisting.Count; i++) {
				buf = rawlisting[i];

				if (isNameList) {
					
					// if NLST was used we only have a file name so
					// there is nothing to parse.
					item = new FtpListItem() {
						FullName = buf
					};

					if (DirectoryExists(item.FullName))
						item.Type = FtpFileSystemObjectType.Directory;
					else
						item.Type = FtpFileSystemObjectType.File;

					lst.Add(item);

				} else {

					// if this is a result of LIST -R then the path will be spit out
					// before each block of objects
					if (listcmd.StartsWith("LIST") && isRecursive) {
						if (buf.StartsWith("/") && buf.EndsWith(":")) {
							path = buf.TrimEnd(':');
							continue;
						}
					}

					// if the next line in the listing starts with spaces
					// it is assumed to be a continuation of the current line
					if (i + 1 < rawlisting.Count && (rawlisting[i + 1].StartsWith("\t") || rawlisting[i + 1].StartsWith(" ")))
						buf += rawlisting[++i];

					item = m_listParser.ParseSingleLine(path, buf, m_caps, machineList);

					// FtpListItem.Parse() returns null if the line
					// could not be parsed
					if (item != null) {
						if (isIncludeSelf || !(item.Name == "." || item.Name == "..")) {
							lst.Add(item);
						} else {
							FtpTrace.WriteLine("Skipped self or parent item: " + item.Name);
						}
					} else {
						FtpTrace.WriteLine("Failed to parse file listing: " + buf);
					}
				}

				// load extended information that wasn't available if the list options flags say to do so.
				if (item != null) {

					// try to dereference symbolic links if the appropriate list
					// option was passed
					if (item.Type == FtpFileSystemObjectType.Link && isDerefLinks) {
						item.LinkObject = DereferenceLink(item);
					}

					// if need to get file modified date
					if (isGetModified && HasFeature(FtpCapability.MDTM)) {

						// if the modified date was not loaded or the modified date is more than a day in the future 
						// and the server supports the MDTM command, load the modified date.
						// most servers do not support retrieving the modified date
						// of a directory but we try any way.
						if (item.Modified == DateTime.MinValue || listcmd.StartsWith("LIST")) {
							DateTime modify;

							if (item.Type == FtpFileSystemObjectType.Directory)
								FtpTrace.WriteLine("Trying to retrieve modification time of a directory, some servers don't like this...");

							if ((modify = GetModifiedTime(item.FullName)) != DateTime.MinValue)
								item.Modified = modify;
						}
					}

					// if need to get file size
					if (isGetSize && HasFeature(FtpCapability.SIZE)) {

						// if no size was parsed, the object is a file and the server
						// supports the SIZE command, then load the file size
						if (item.Size == -1) {
							if (item.Type != FtpFileSystemObjectType.Directory) {
								item.Size = GetFileSize(item.FullName);
							} else {
								item.Size = 0;
							}
						}
					}
				}
			}

			return lst.ToArray();
		}

		/// <summary>
        /// Begins an asynchronous operation to get a file listing from the server. 
        /// Each <see cref="FtpListItem"/> object returned contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(AsyncCallback callback, Object state) {
			return BeginGetListing(null, callback, state);
		}

        /// <summary>
        /// Begins an asynchronous operation to get a file listing from the server. 
        /// Each <see cref="FtpListItem"/> object returned contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(string path, AsyncCallback callback, Object state) {
			return BeginGetListing(path, FtpListOption.Modify | FtpListOption.Size, callback, state);
		}

		delegate FtpListItem[] AsyncGetListing(string path, FtpListOption options);

		/// <summary>
		/// Gets a file listing from the server asynchronously
		/// </summary>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(string path, FtpListOption options, AsyncCallback callback, Object state) {
			IAsyncResult ar;
			AsyncGetListing func;

			ar = (func = new AsyncGetListing(GetListing)).BeginInvoke(path, options, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginGetListing"/>
		/// </summary>
		/// <param name="ar">IAsyncResult return from <see cref="o:BeginGetListing"/></param>
		/// <returns>An array of items retrieved in the listing</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public FtpListItem[] EndGetListing(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetListing>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <param name="path">The path to list</param>
        /// <param name="options">Options that dictate how the list operation is performed</param>
        /// <returns>An array of items retrieved in the listing</returns>
	    public async Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, FtpListOption, FtpListItem[]>(
	            (p, o, ac, s) => BeginGetListing(p, o, ac, s),
	            ar => EndGetListing(ar),
	            path, options, null);
	    }

        /// <summary>
        /// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <param name="path">The path to list</param>
        /// <returns>An array of items retrieved in the listing</returns>
        public async Task<FtpListItem[]> GetListingAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, FtpListItem[]>(
                (p, ac, s) => BeginGetListing(p, ac, s),
                ar => EndGetListing(ar),
                path, null);
        }

        /// <summary>
        /// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
        /// contains information about the file that was able to be retrieved. 
        /// </summary>
        /// <remarks>
        /// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
        /// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
        /// is equal to 0, then it means the size of the object could also not
        /// be retrieved.
        /// </remarks>
        /// <returns>An array of items retrieved in the listing</returns>
        public async Task<FtpListItem[]> GetListingAsync() {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<FtpListItem[]>(
                (ac, s) => BeginGetListing(ac, s),
                ar => EndGetListing(ar),
                null);
        }
#endif

#endregion

		#region Get Name Listing

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		public string[] GetNameListing() {
			return GetNameListing(null);
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		/// <example><code source="..\Examples\GetNameListing.cs" lang="cs" /></example>
		public string[] GetNameListing(string path) {
			List<string> listing = new List<string>();

			// calc path to request
			path = GetAbsolutePath(path);

			lock (m_lock) {
				// always get the file listing in binary
				// to avoid any potential character translation
				// problems that would happen if in ASCII.
				Execute("TYPE I");

				using (FtpDataStream stream = OpenDataStream(("NLST " + path.GetFtpPath()), 0)) {
					string buf;

					try {
						while ((buf = stream.ReadLine(Encoding)) != null)
							listing.Add(buf);
					} finally {
						stream.Close();
					}
				}
			}

			return listing.ToArray();
		}

		delegate string[] AsyncGetNameListing(string path);

		/// <summary>
        /// Begin an asynchronous operation to return a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetNameListing(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetNameListing func;

			ar = (func = new AsyncGetNameListing(GetNameListing)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Begin an asynchronous operation to return a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetNameListing(AsyncCallback callback, object state) {
			return BeginGetNameListing(null, callback, state);
		}

		/// <summary>
        /// Ends a call to <see cref="o:BeginGetNameListing"/>
		/// </summary>
        /// <param name="ar">IAsyncResult object returned from <see cref="o:BeginGetNameListing"/></param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public string[] EndGetNameListing(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetNameListing>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Returns a file/directory listing using the NLST command asynchronously
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <returns>An array of file and directory names if any were returned.</returns>
	    public async Task<string[]> GetNameListingAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string, string[]>(
	            (p, ac, s) => BeginGetNameListing(p, ac, s),
	            ar => EndGetNameListing(ar),
	            path, null);
	    }

        /// <summary>
        /// Returns a file/directory listing using the NLST command asynchronously
        /// </summary>
        /// <returns>An array of file and directory names if any were returned.</returns>
        public async Task<string[]> GetNameListingAsync() {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string[]>(
                (ac, s) => BeginGetNameListing(ac, s),
                ar => EndGetNameListing(ar),
                null);
        }
#endif

#endregion

		#region File Hashing - HASH

		/// <summary>
		/// Gets the currently selected hash algorithm for the HASH command.
		/// </summary>
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.None"/> if there was a problem.</returns>
		/// <example><code source="..\Examples\GetHashAlgorithm.cs" lang="cs" /></example>
		public FtpHashAlgorithm GetHashAlgorithm() {
			FtpReply reply;
			FtpHashAlgorithm type = FtpHashAlgorithm.NONE;

			lock (m_lock) {
				if ((reply = Execute("OPTS HASH")).Success) {
					switch (reply.Message) {
						case "SHA-1":
							type = FtpHashAlgorithm.SHA1;
							break;
						case "SHA-256":
							type = FtpHashAlgorithm.SHA256;
							break;
						case "SHA-512":
							type = FtpHashAlgorithm.SHA512;
							break;
						case "MD5":
							type = FtpHashAlgorithm.MD5;
							break;
					}
				}
			}

			return type;
		}

		delegate FtpHashAlgorithm AsyncGetHashAlgorithm();

		/// <summary>
		/// Begins an asynchronous operation to get the currently selected hash algorithm for the HASH command.
		/// </summary>
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetHashAlgorithm(AsyncCallback callback, object state) {
			AsyncGetHashAlgorithm func;
			IAsyncResult ar;

			ar = (func = new AsyncGetHashAlgorithm(GetHashAlgorithm)).BeginInvoke(callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetHashAlgorithm"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetHashAlgorithm"/></param>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.None"/> if there was a problem.</returns>
		public FtpHashAlgorithm EndGetHashAlgorithm(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetHashAlgorithm>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the currently selected hash algorithm for the HASH command asynchronously.
		/// </summary>
		/// <remarks>
		///  This feature is experimental. See this link for details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <returns>The <see cref="FtpHashAlgorithm"/> flag or <see cref="FtpHashAlgorithm.None"/> if there was a problem.</returns>
		public async Task<FtpHashAlgorithm> GetHashAlgorithmAsync() {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<FtpHashAlgorithm>(
				(ac, s) => BeginGetHashAlgorithm(ac, s),
				ar => EndGetHashAlgorithm(ar), null);
		}
#endif

		/// <summary>
		/// Sets the hash algorithm on the server to use for the HASH command. 
		/// </summary>
		/// <remarks>
		/// If you specify an algorithm not listed in <see cref="FtpClient.HashAlgorithms"/>
		/// a <see cref="NotImplementedException"/> will be thrown
		/// so be sure to query that list of Flags before
		/// selecting a hash algorithm. Support for the
		/// HASH command is experimental. Please see
		/// the following link for more details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="type">Hash Algorithm</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		/// <example><code source="..\Examples\SetHashAlgorithm.cs" lang="cs" /></example>
		public void SetHashAlgorithm(FtpHashAlgorithm type) {
			FtpReply reply;
			string algorithm;

			lock (m_lock) {
				if ((HashAlgorithms & type) != type)
					throw new NotImplementedException(("The hash algorithm " + type.ToString() + " was not advertised by the server."));

				switch (type) {
					case FtpHashAlgorithm.SHA1:
						algorithm = "SHA-1";
						break;
					case FtpHashAlgorithm.SHA256:
						algorithm = "SHA-256";
						break;
					case FtpHashAlgorithm.SHA512:
						algorithm = "SHA-512";
						break;
					case FtpHashAlgorithm.MD5:
						algorithm = "MD5";
						break;
					default:
						algorithm = type.ToString();
						break;
				}

				if (!(reply = Execute("OPTS HASH " + algorithm)).Success)
					throw new FtpCommandException(reply);
			}
		}

		delegate void AsyncSetHashAlgorithm(FtpHashAlgorithm type);

		/// <summary>
		/// Begins an asynchronous operation to set the hash algorithm on the server to use for the HASH command. 
		/// </summary>
		/// <remarks>
		/// If you specify an algorithm not listed in <see cref="FtpClient.HashAlgorithms"/>
		/// a <see cref="NotImplementedException"/> will be thrown
		/// so be sure to query that list of Flags before
		/// selecting a hash algorithm. Support for the
		/// HASH command is experimental. Please see
		/// the following link for more details:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="type">Hash algorithm to use</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginSetHashAlgorithm(FtpHashAlgorithm type, AsyncCallback callback, object state) {
			AsyncSetHashAlgorithm func;
			IAsyncResult ar;

			ar = (func = new AsyncSetHashAlgorithm(SetHashAlgorithm)).BeginInvoke(type, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginSetHashAlgorithm"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetHashAlgorithm"/></param>
		public void EndSetHashAlgorithm(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetHashAlgorithm>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Sets the hash algorithm on the server to be used with the HASH command asynchronously.
		/// </summary>
		/// <param name="type">Hash algorithm to use</param>
		/// <exception cref="System.NotImplementedException">Thrown if the selected algorithm is not available on the server</exception>
		public async Task SetHashAlgorithmAsync(FtpHashAlgorithm type) {
			//TODO:  Rewrite as true async method with cancellation support
			await Task.Factory.FromAsync<FtpHashAlgorithm>(
				(t, ac, s) => BeginSetHashAlgorithm(t, ac, s),
				ar => EndSetHashAlgorithm(ar),
				type, null);
		}
#endif

		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// 
		/// This feature is experimental and based on the following draft:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="path">Full or relative path of the object to compute the hash for.</param>
		/// <returns>The hash of the file.</returns>
		/// <exception cref="FtpCommandException">Thrown if the <see cref="HashAlgorithms"/> property is <see cref="FtpHashAlgorithm.NONE"/></exception>
		/// <example><code source="..\Examples\GetHash.cs" lang="cs" /></example>
		public FtpHash GetHash(string path) {
			FtpReply reply;
			FtpHash hash = new FtpHash();
			Match m;

			if (path == null)
				throw new ArgumentException("GetHash(path) argument can't be null");

			lock (m_lock) {
				if (!(reply = Execute("HASH " + path.GetFtpPath())).Success)
					throw new FtpCommandException(reply);
			}

			// Current draft says the server should return this:
			// SHA-256 0-49 169cd22282da7f147cb491e559e9dd filename.ext
			if (!(m = Regex.Match(reply.Message,
					@"(?<algorithm>.+)\s" +
					@"(?<bytestart>\d+)-(?<byteend>\d+)\s" +
					@"(?<hash>.+)\s" +
					@"(?<filename>.+)")).Success) {

				// Current version of FileZilla returns this:
				// SHA-1 21c2ca15cf570582949eb59fb78038b9c27ffcaf 
				m = Regex.Match(reply.Message, @"(?<algorithm>.+)\s(?<hash>.+)\s");
			}

			if (m != null && m.Success) {
				switch (m.Groups["algorithm"].Value) {
					case "SHA-1":
						hash.Algorithm = FtpHashAlgorithm.SHA1;
						break;
					case "SHA-256":
						hash.Algorithm = FtpHashAlgorithm.SHA256;
						break;
					case "SHA-512":
						hash.Algorithm = FtpHashAlgorithm.SHA512;
						break;
					case "MD5":
						hash.Algorithm = FtpHashAlgorithm.MD5;
						break;
					default:
						throw new NotImplementedException("Unknown hash algorithm: " + m.Groups["algorithm"].Value);
				}

				hash.Value = m.Groups["hash"].Value;
			} else {
				FtpTrace.WriteLine("Failed to parse hash from: " + reply.Message);
			}

			return hash;
		}

		delegate FtpHash AsyncGetHash(string path);

		/// <summary>
		/// Begins an asynchronous operation to get the hash of an object on the server using the currently selected hash algorithm. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// 
		/// This feature is experimental and based on the following draft:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="path">The file you want the server to compute the hash for</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetHash(string path, AsyncCallback callback, object state) {
			AsyncGetHash func;
			IAsyncResult ar;

			ar = (func = new AsyncGetHash(GetHash)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetHash"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetHash"/></param>
		public FtpHash EndGetHash(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetHash>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the hash of an object on the server using the currently selected hash algorithm asynchronously. 
		/// </summary>
		/// <remarks>
		/// Supported algorithms, if any, are available in the <see cref="HashAlgorithms"/>
		/// property. You should confirm that it's not equal
		/// to <see cref="FtpHashAlgorithm.NONE"/> before calling this method
		/// otherwise the server trigger a <see cref="FtpCommandException"/>
		/// due to a lack of support for the HASH command. You can
		/// set the algorithm using the <see cref="SetHashAlgorithm"/> method and
		/// you can query the server for the current hash algorithm
		/// using the <see cref="GetHashAlgorithm"/> method.
		/// 
		/// This feature is experimental and based on the following draft:
		/// http://tools.ietf.org/html/draft-bryan-ftpext-hash-02
		/// </remarks>
		/// <param name="path">The file you want the server to compute the hash for</param>
		/// <exception cref="FtpCommandException">Thrown if the <see cref="HashAlgorithms"/> property is <see cref="FtpHashAlgorithm.NONE"/></exception>
		/// <returns>The hash of the file.</returns>
		public async Task<FtpHash> GetHashAsync(string path) {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, FtpHash>(
				(p, ac, s) => BeginGetHash(p, ac, s),
				ar => EndGetHash(ar),
				path, null);
		}
#endif

		#endregion

		#region File Checksum

		delegate FtpHash AsyncGetChecksum(string path);

		/// <summary>
		/// Retrieves a checksum of the given file using a checksum method that the server supports, if any. 
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
		/// 2. MD5 / XMD5 commands
		/// 3. XSHA1 command
		/// 4. XSHA256 command
		/// 5. XSHA512 command
		/// 6. XCRC command
		/// </remarks>
		/// <param name="client"><see cref="FtpClient"/> Object</param>
		/// <param name="path">Full or relative path of the file to checksum</param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		/// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
		public FtpHash GetChecksum(string path) {
			if (HasFeature(FtpCapability.HASH)) {
				return GetHash(path);
			} else {
				FtpHash res = new FtpHash();

				if (HasFeature(FtpCapability.MD5)) {
					res.Value = GetMD5(path);
					res.Algorithm = FtpHashAlgorithm.MD5;
				} else if (HasFeature(FtpCapability.XMD5)) {
					res.Value = GetXMD5(path);
					res.Algorithm = FtpHashAlgorithm.MD5;
				} else if (HasFeature(FtpCapability.XSHA1)) {
					res.Value = GetXSHA1(path);
					res.Algorithm = FtpHashAlgorithm.SHA1;
				} else if (HasFeature(FtpCapability.XSHA256)) {
					res.Value = GetXSHA256(path);
					res.Algorithm = FtpHashAlgorithm.SHA256;
				} else if (HasFeature(FtpCapability.XSHA512)) {
					res.Value = GetXSHA512(path);
					res.Algorithm = FtpHashAlgorithm.SHA512;
				} else if (HasFeature(FtpCapability.XCRC)) {
					res.Value = GetXCRC(path);
					res.Algorithm = FtpHashAlgorithm.CRC;
				}

				return res;
			}
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a checksum of the given file using a checksum method that the server supports, if any. 
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
		/// 2. MD5 / XMD5 commands
		/// 3. XSHA1 command
		/// 4. XSHA256 command
		/// 5. XSHA512 command
		/// 6. XCRC command
		/// </remarks>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetChecksum(string path, AsyncCallback callback,
			object state) {
			AsyncGetChecksum func = new AsyncGetChecksum(GetChecksum);
			IAsyncResult ar = func.BeginInvoke(path, callback, state);
			;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetChecksum"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetChecksum"/></param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		public FtpHash EndGetChecksum(IAsyncResult ar) {
			AsyncGetChecksum func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetChecksum)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Retrieves a checksum of the given file using a checksum method that the server supports, if any. 
		/// </summary>
		/// <remarks>
		/// The algorithm used goes in this order:
		/// 1. HASH command; server preferred algorithm. See <see cref="FtpClient.SetHashAlgorithm"/>
		/// 2. MD5 / XMD5 commands
		/// 3. XSHA1 command
		/// 4. XSHA256 command
		/// 5. XSHA512 command
		/// 6. XCRC command
		/// </remarks>
		/// <param name="client"><see cref="FtpClient"/> Object</param>
		/// <param name="path">Full or relative path of the file to checksum</param>
		/// <returns><see cref="FtpHash"/> object containing the value and algorithm. Use the <see cref="FtpHash.IsValid"/> property to
		/// determine if this command was successful. <see cref="FtpCommandException"/>s can be thrown from
		/// the underlying calls.</returns>
		/// <example><code source="..\Examples\GetChecksum.cs" lang="cs" /></example>
		public async Task<FtpHash> GetChecksumAsync(string path) {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, FtpHash>(
				(p, ac, s) => BeginGetChecksum(p, ac, s),
				ar => EndGetChecksum(ar), path, null);
		}
#endif

		#endregion

		#region File Checksum - MD5
		delegate string AsyncGetMD5(string path);

		/// <summary>
		/// Gets the MD5 hash of the specified file using MD5. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		public string GetMD5(string path) {
			// http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1
			FtpReply reply;
			string response;

			if (!(reply = Execute("MD5 " + path)).Success)
				throw new FtpCommandException(reply);

			response = reply.Message;
			if (response.StartsWith(path)) {
				response = response.Remove(0, path.Length).Trim();
			}

			return response;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a MD5 hash. The MD5 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetMD5(string path, AsyncCallback callback, object state) {
			AsyncGetMD5 func = new AsyncGetMD5(GetMD5);
			IAsyncResult ar = func.BeginInvoke(path, callback, state);
			;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetMD5"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetMD5"/></param>
		/// <returns>The MD5 hash of the specified file.</returns>
		public string EndGetMD5(IAsyncResult ar) {
			AsyncGetMD5 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetMD5)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the MD5 hash of the specified file using MD5 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		public async Task<string> GetMD5Async(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetMD5(p, ac, s),
				ar => EndGetMD5(ar),
				path, null);
		}
#endif
		#endregion

		#region File Checksum - XCRC


		delegate string AsyncGetXCRC(string path);
		/// <summary>
		/// Get the CRC value of the specified file. This is a non-standard extension of the protocol 
		/// and may throw a FtpCommandException if the server does not support it.
		/// </summary>
		/// <param name="client">FtpClient object</param>
		/// <param name="path">The path of the file you'd like the server to compute the CRC value for.</param>
		/// <returns>The response from the server, typically the XCRC value. FtpCommandException thrown on error</returns>
		public string GetXCRC(string path) {
			FtpReply reply;

			if (!(reply = Execute("XCRC " + path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a CRC hash. The XCRC command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXCRC(string path, AsyncCallback callback, object state) {
			AsyncGetXCRC func = new AsyncGetXCRC(GetXCRC);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXCRC"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXCRC"/></param>
		/// <returns>The CRC hash of the specified file.</returns>
		public string EndGetXCRC(IAsyncResult ar) {
			AsyncGetXCRC func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetXCRC)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the CRC hash of the specified file using XCRC asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the CRC hash.</returns>
		public async Task<string> GetXCRCAsync(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetXCRC(p, ac, s),
				ar => EndGetXCRC(ar), path, null);
		}
#endif

		#endregion

		#region File Checksum - XMD5
		delegate string AsyncGetXMD5(string path);

		/// <summary>
		/// Gets the MD5 hash of the specified file using XMD5. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		public string GetXMD5(string path) {
			FtpReply reply;

			if (!(reply = Execute("XMD5 " + path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a XMD5 hash. The XMD5 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXMD5(string path, AsyncCallback callback, object state) {
			AsyncGetXMD5 func = new AsyncGetXMD5(GetXMD5);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXMD5"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXMD5"/></param>
		/// <returns>The MD5 hash of the specified file.</returns>
		public string EndGetXMD5(IAsyncResult ar) {
			AsyncGetXMD5 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetXMD5)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the MD5 hash of the specified file using XMD5 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the MD5 hash.</returns>
		public async Task<string> GetXMD5Async(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetXMD5(p, ac, s),
				ar => EndGetXMD5(ar), path, null);
		}
#endif

		#endregion

		#region File Checksum - XSHA1

		delegate string AsyncGetXSHA1(string path);

		/// <summary>
		/// Gets the SHA-1 hash of the specified file using XSHA1. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-1 hash.</returns>
		public string GetXSHA1(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA1 " + path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA1 hash. The XSHA1 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA1(string path, AsyncCallback callback, object state) {
			AsyncGetXSHA1 func = new AsyncGetXSHA1(GetXSHA1);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA1"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA1"/></param>
		/// <returns>The SHA-1 hash of the specified file.</returns>
		public string EndGetXSHA1(IAsyncResult ar) {
			AsyncGetXSHA1 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetXSHA1)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the SHA-1 hash of the specified file using XSHA1 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-1 hash.</returns>
		public async Task<string> GetXSHA1sync(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetXSHA1(p, ac, s),
				ar => EndGetXSHA1(ar),
				path, null);
		}
#endif

		#endregion

		#region File Checksum - XSHA256

		delegate string AsyncGetXSHA256(string path);

		/// <summary>
		/// Gets the SHA-256 hash of the specified file using XSHA256. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-256 hash.</returns>
		public string GetXSHA256(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA256 " + path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA256 hash. The XSHA256 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA256(string path, AsyncCallback callback, object state) {
			AsyncGetXSHA256 func = new AsyncGetXSHA256(GetXSHA256);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA256"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA256"/></param>
		/// <returns>The SHA-256 hash of the specified file.</returns>
		public string EndGetXSHA256(IAsyncResult ar) {
			AsyncGetXSHA256 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetXSHA256)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the SHA-256 hash of the specified file using XSHA256 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-256 hash.</returns>
		public async Task<string> GetXSHA256Async(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetXSHA256(p, ac, s),
				ar => EndGetXSHA256(ar),
				path, null);
		}
#endif

		#endregion

		#region File Checksum - XSHA512

		delegate string AsyncGetXSHA512(string path);

		/// <summary>
		/// Gets the SHA-512 hash of the specified file using XSHA512. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-512 hash.</returns>
		public string GetXSHA512(string path) {
			FtpReply reply;

			if (!(reply = Execute("XSHA512 " + path)).Success)
				throw new FtpCommandException(reply);

			return reply.Message;
		}

		/// <summary>
		/// Begins an asynchronous operation to retrieve a SHA512 hash. The XSHA512 command is non-standard
		/// and not guaranteed to work.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetXSHA512(string path, AsyncCallback callback, object state) {
			AsyncGetXSHA512 func = new AsyncGetXSHA512(GetXSHA512);
			IAsyncResult ar = func.BeginInvoke(path, callback, state); ;

			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends an asynchronous call to <see cref="BeginGetXSHA512"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetXSHA512"/></param>
		/// <returns>The SHA-512 hash of the specified file.</returns>
		public string EndGetXSHA512(IAsyncResult ar) {
			AsyncGetXSHA512 func = null;

			lock (m_asyncmethods) {
				if (!m_asyncmethods.ContainsKey(ar))
					throw new InvalidOperationException("The specified IAsyncResult was not found in the collection.");

				func = (AsyncGetXSHA512)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func.EndInvoke(ar);
		}

#if (CORE || NETFX45)
		/// <summary>
		/// Gets the SHA-512 hash of the specified file using XSHA512 asynchronously. This is a non-standard extension
		/// to the protocol and may or may not work. A FtpCommandException will be
		/// thrown if the command fails.
		/// </summary>
		/// <param name="client">FtpClient Object</param>
		/// <param name="path">Full or relative path to remote file</param>
		/// <returns>Server response, presumably the SHA-512 hash.</returns>
		public async Task<string> GetXSHA512Async(string path) {
			return await Task.Factory.FromAsync<string, string>(
				(p, ac, s) => BeginGetXSHA512(p, ac, s),
				ar => EndGetXSHA512(ar), path, null);
		}
#endif
		#endregion

		#region Set Data Type

		/// <summary>
		/// Sets the data type of information sent over the data stream
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		protected void SetDataType(FtpDataType type) {
			FtpReply reply;

			lock (m_lock) {
				switch (type) {
					case FtpDataType.ASCII:
						if (!(reply = Execute("TYPE A")).Success)
							throw new FtpCommandException(reply);
						/*if (!(reply = Execute("STRU R")).Success)
							FtpTrace.WriteLine(reply.Message);*/
						break;
					case FtpDataType.Binary:
						if (!(reply = Execute("TYPE I")).Success)
							throw new FtpCommandException(reply);
						/*if (!(reply = Execute("STRU F")).Success)
							FtpTrace.WriteLine(reply.Message);*/
						break;
					default:
						throw new FtpException("Unsupported data type: " + type.ToString());
				}
			}

			CurrentDataType = type;

		}

		delegate void AsyncSetDataType(FtpDataType type);

		/// <summary>
        /// Begins an asynchronous operation to set the data type of information sent over the data stream
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		protected IAsyncResult BeginSetDataType(FtpDataType type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetDataType func;

			ar = (func = new AsyncSetDataType(SetDataType)).BeginInvoke(type, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="BeginSetDataType"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginSetDataType"/></param>
		protected void EndSetDataType(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetDataType>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Sets the data type of information sent over the data stream asynchronously
        /// </summary>
        /// <param name="type">ASCII/Binary</param>
	    protected async Task SetDataTypeAsync(FtpDataType type) {
            //TODO:  Rewrite as true async method with cancellation support
            await Task.Factory.FromAsync<FtpDataType>(
	            (t, ac, s) => BeginSetDataType(t, ac, s),
	            ar => EndSetDataType(ar),
	            type, null);
	    }
#endif
		#endregion

		#region Set Working Dir
		
		/// <summary>
		/// Sets the work directory on the server
		/// </summary>
		/// <param name="path">The path of the directory to change to</param>
		/// <example><code source="..\Examples\SetWorkingDirectory.cs" lang="cs" /></example>
		public void SetWorkingDirectory(string path) {
			FtpReply reply;
			string ftppath = path.GetFtpPath();

			if (ftppath == "." || ftppath == "./")
				return;

			lock (m_lock) {
				if (!(reply = Execute("CWD " + ftppath)).Success)
					throw new FtpCommandException(reply);
			}
		}

		delegate void AsyncSetWorkingDirectory(string path);

		/// <summary>
		/// Begins an asynchronous operation to set the working directory on the server
		/// </summary>
		/// <param name="path">The directory to change to</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginSetWorkingDirectory(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetWorkingDirectory func;

			ar = (func = new AsyncSetWorkingDirectory(SetWorkingDirectory)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginSetWorkingDirectory"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginSetWorkingDirectory"/></param>
		/// <example><code source="..\Examples\BeginSetWorkingDirectory.cs" lang="cs" /></example>
		public void EndSetWorkingDirectory(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetWorkingDirectory>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Sets the working directory on the server asynchronously
        /// </summary>
        /// <param name="path">The directory to change to</param>
	    public async Task SetWorkingDirectoryAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
            await Task.Factory.FromAsync<string>(
	            (p, ac, s) => BeginSetWorkingDirectory(p, ac, s),
	            ar => EndSetWorkingDirectory(ar),
	            path, null);
	    }
#endif
		#endregion

		#region Get Working Dir

		/// <summary>
		/// Gets the current working directory
		/// </summary>
		/// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
		/// <example><code source="..\Examples\GetWorkingDirectory.cs" lang="cs" /></example>
		public string GetWorkingDirectory() {
			FtpReply reply;
			Match m;

			lock (m_lock) {
				if (!(reply = Execute("PWD")).Success)
					throw new FtpCommandException(reply);
			}

			if ((m = Regex.Match(reply.Message, "\"(?<pwd>.*)\"")).Success) {
				return m.Groups["pwd"].Value;
			}

			// check for MODCOMP ftp path mentioned in forums: https://netftp.codeplex.com/discussions/444461
			if ((m = Regex.Match(reply.Message, "PWD = (?<pwd>.*)")).Success) {
				return m.Groups["pwd"].Value;
			}

			FtpTrace.WriteLine("Failed to parse working directory from: " + reply.Message);

			return "./";
		}

		delegate string AsyncGetWorkingDirectory();

		/// <summary>
		/// Begins an asynchronous operation to get the working directory
		/// </summary>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
		public IAsyncResult BeginGetWorkingDirectory(AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetWorkingDirectory func;

			ar = (func = new AsyncGetWorkingDirectory(GetWorkingDirectory)).BeginInvoke(callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetWorkingDirectory"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginGetWorkingDirectory"/></param>
		/// <returns>The current working directory</returns>
		/// <example><code source="..\Examples\BeginGetWorkingDirectory.cs" lang="cs" /></example>
		public string EndGetWorkingDirectory(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetWorkingDirectory>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Gets the current working directory asynchronously
        /// </summary>
        /// <returns>The current working directory, ./ if the response couldn't be parsed.</returns>
	    public async Task<string> GetWorkingDirectoryAsync() {
            //TODO:  Rewrite as true async method with cancellation support
            return await Task.Factory.FromAsync<string>(
	            (ac, s) => BeginGetWorkingDirectory(ac, s),
	            ar => EndGetWorkingDirectory(ar), null);
	    }
#endif
		#endregion

		#region Get File Size
		
		/// <summary>
		/// Gets the size of a remote file
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <returns>-1 if the command fails, otherwise the file size</returns>
		/// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
		public virtual long GetFileSize(string path) {
			FtpReply reply;
			long length = 0;

			lock (m_lock) {
				if (!(reply = Execute("SIZE " + path.GetFtpPath())).Success)
					return -1;

				if (!long.TryParse(reply.Message, out length))
					return -1;
			}

			return length;
		}

		delegate long AsyncGetFileSize(string path);

		/// <summary>
        /// Begins an asynchronous operation to retrieve the size of a remote file
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetFileSize func;

			ar = (func = new AsyncGetFileSize(GetFileSize)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="BeginGetFileSize"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginGetFileSize"/></param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public long EndGetFileSize(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetFileSize>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Retrieve the size of a remote file asynchronously
        /// </summary>
        /// <param name="path">The full or relative path of the file</param>
        /// <returns>The size of the file, -1 if there was a problem.</returns>
	    public async Task<long> GetFileSizeAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, long>(
	            (p, ac, s) => BeginGetFileSize(p, ac, s),
	            ar => EndGetFileSize(ar),
	            path, null);
	    }
#endif
		#endregion

		#region Get Modified Time

		/// <summary>
        /// Gets the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
        /// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
		public virtual DateTime GetModifiedTime(string path) {
			DateTime modify = DateTime.MinValue;
			FtpReply reply;

			lock (m_lock) {
				if ((reply = Execute("MDTM " + path.GetFtpPath())).Success)
					modify = reply.Message.GetFtpDate(DateTimeStyles.AssumeUniversal);
			}

			return modify;
		}

		delegate DateTime AsyncGetModifiedTime(string path);

		/// <summary>
		/// Begins an asynchronous operation to get the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public IAsyncResult BeginGetModifiedTime(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetModifiedTime func;

			ar = (func = new AsyncGetModifiedTime(GetModifiedTime)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
        /// Ends a call to <see cref="BeginGetModifiedTime"/>
		/// </summary>
        /// <param name="ar">IAsyncResult returned from <see cref="BeginGetModifiedTime"/></param>
        /// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public DateTime EndGetModifiedTime(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetModifiedTime>(ar).EndInvoke(ar);
		}

#if (CORE || NETFX45)
        /// <summary>
        /// Gets the modified time of a remote file asynchronously
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
	    public async Task<DateTime> GetModifiedTimeAsync(string path) {
            //TODO:  Rewrite as true async method with cancellation support
	        return await Task.Factory.FromAsync<string, DateTime>(
	            (p, ac, s) => BeginGetModifiedTime(p, ac, s),
	            ar => EndGetModifiedTime(ar),
	            path, null);
	    }
#endif

		#endregion

		#region Utils

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

			} else if (!path.StartsWith("/")) {

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

		private static string DecodeUrl(string url) {
#if CORE
			return WebUtility.UrlDecode(url);
#else
			return HttpUtility.UrlDecode(url);
#endif
		}

		private static byte[] ReadToEnd(Stream stream, long maxLength, int chunkLen) {
			int read = 1;
			byte[] buffer = new byte[chunkLen];
			using (var mem = new MemoryStream()) {
				do {
					long length = maxLength == 0 ? buffer.Length : Math.Min(maxLength - (int)mem.Length, buffer.Length);
					read = stream.Read(buffer, 0, (int)length);
					mem.Write(buffer, 0, read);
					if (maxLength > 0 && mem.Length == maxLength) break;
				} while (read > 0);

				return mem.ToArray();
			}
		}

		/// <summary>
		/// Disables UTF8 support and changes the Encoding property
		/// back to ASCII. If the server returns an error when trying
		/// to turn UTF8 off a FtpCommandException will be thrown.
		/// </summary>
		public void DisableUTF8() {
			FtpReply reply;

			lock (m_lock) {
				if (!(reply = Execute("OPTS UTF8 OFF")).Success)
					throw new FtpCommandException(reply);

				m_textEncoding = Encoding.ASCII;
				m_textEncodingAutoUTF = false;
			}
		}

		private void ReadStaleData() {
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (m_stream.IsConnected && !m_stream.IsEncrypted) {
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					m_stream.RawSocketRead(buf);
					FtpTrace.Write("The data was: ");
					FtpTrace.WriteLine(Encoding.GetString(buf).TrimEnd('\r', '\n'));
				}
			}
		}

		private bool IsProxy() {
			return (this is FtpClientProxy);
		}

#endregion

		#region Static API

		/// <summary>
		/// Calculate the CHMOD integer value given a set of permissions.
		/// </summary>
		public static int CalcChmod(FtpPermission owner, FtpPermission group, FtpPermission other) {

			int chmod = 0;

			if (HasPermission(owner, FtpPermission.Read)) {
				chmod += 400;
			}
			if (HasPermission(owner, FtpPermission.Write)) {
				chmod += 200;
			}
			if (HasPermission(owner, FtpPermission.Execute)) {
				chmod += 100;
			}

			if (HasPermission(group, FtpPermission.Read)) {
				chmod += 40;
			}
			if (HasPermission(group, FtpPermission.Write)) {
				chmod += 20;
			}
			if (HasPermission(group, FtpPermission.Execute)) {
				chmod += 10;
			}

			if (HasPermission(other, FtpPermission.Read)) {
				chmod += 4;
			}
			if (HasPermission(other, FtpPermission.Write)) {
				chmod += 2;
			}
			if (HasPermission(other, FtpPermission.Execute)) {
				chmod += 1;
			}

			return chmod;
		}

		private static bool HasPermission(FtpPermission owner, FtpPermission flag) {
			return (owner & flag) == flag;
		}

        //TODO:  Create async versions of static methods

		/// <summary>
		/// Connects to the specified URI. If the path specified by the URI ends with a
		/// / then the working directory is changed to the path specified.
		/// </summary>
		/// <param name="uri">The URI to parse</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <returns>FtpClient object</returns>
		public static FtpClient Connect(Uri uri, bool checkcertificate) {
			FtpClient cl = new FtpClient();

			if (uri == null)
				throw new ArgumentException("Invalid URI object");

			switch (uri.Scheme.ToLower()) {
				case "ftp":
				case "ftps":
					break;
				default:
					throw new UriFormatException("The specified URI scheme is not supported. Please use ftp:// or ftps://");
			}

			cl.Host = uri.Host;
			cl.Port = uri.Port;

			if (uri.UserInfo != null && uri.UserInfo.Length > 0) {
				if (uri.UserInfo.Contains(":")) {
					string[] parts = uri.UserInfo.Split(':');

					if (parts.Length != 2)
						throw new UriFormatException("The user info portion of the URI contains more than 1 colon. The username and password portion of the URI should be URL encoded.");

					cl.Credentials = new NetworkCredential(DecodeUrl(parts[0]), DecodeUrl(parts[1]));
				} else
					cl.Credentials = new NetworkCredential(DecodeUrl(uri.UserInfo), "");
			} else {
				// if no credentials were supplied just make up
				// some for anonymous authentication.
				cl.Credentials = new NetworkCredential("ftp", "ftp");
			}

			cl.ValidateCertificate += new FtpSslValidation(delegate(FtpClient control, FtpSslValidationEventArgs e) {
				if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None && checkcertificate)
					e.Accept = false;
				else
					e.Accept = true;
			});

			cl.Connect();

			if (uri.PathAndQuery != null && uri.PathAndQuery.EndsWith("/"))
				cl.SetWorkingDirectory(uri.PathAndQuery);

			return cl;
		}

		/// <summary>
		/// Connects to the specified URI. If the path specified by the URI ends with a
		/// / then the working directory is changed to the path specified.
		/// </summary>
		/// <param name="uri">The URI to parse</param>
		/// <returns>FtpClient object</returns>
		public static FtpClient Connect(Uri uri) {
			return Connect(uri, true);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <param name="datatype">ASCII/Binary mode</param>
		/// <param name="restart">Restart location</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
		public static Stream OpenRead(Uri uri, bool checkcertificate, FtpDataType datatype, long restart) {
			FtpClient cl = null;

			CheckURI(uri);

			cl = Connect(uri, checkcertificate);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenRead(uri.PathAndQuery, datatype, restart);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <param name="datatype">ASCII/Binary mode</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
		public static Stream OpenRead(Uri uri, bool checkcertificate, FtpDataType datatype) {
			return OpenRead(uri, checkcertificate, datatype, 0);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
		public static Stream OpenRead(Uri uri, bool checkcertificate) {
			return OpenRead(uri, checkcertificate, FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenReadURI.cs" lang="cs" /></example>
		public static Stream OpenRead(Uri uri) {
			return OpenRead(uri, true, FtpDataType.Binary, 0);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <param name="datatype">ASCII/Binary mode</param> 
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
		public static Stream OpenWrite(Uri uri, bool checkcertificate, FtpDataType datatype) {
			FtpClient cl = null;

			CheckURI(uri);

			cl = Connect(uri, checkcertificate);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenWrite(uri.PathAndQuery, datatype);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
		public static Stream OpenWrite(Uri uri, bool checkcertificate) {
			return OpenWrite(uri, checkcertificate, FtpDataType.Binary);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenWriteURI.cs" lang="cs" /></example>
		public static Stream OpenWrite(Uri uri) {
			return OpenWrite(uri, true, FtpDataType.Binary);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <param name="datatype">ASCII/Binary mode</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
		public static Stream OpenAppend(Uri uri, bool checkcertificate, FtpDataType datatype) {
			FtpClient cl = null;

			CheckURI(uri);

			cl = Connect(uri, checkcertificate);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenAppend(uri.PathAndQuery, datatype);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <param name="checkcertificate">Indicates if a ssl certificate should be validated when using FTPS schemes</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
		public static Stream OpenAppend(Uri uri, bool checkcertificate) {
			return OpenAppend(uri, checkcertificate, FtpDataType.Binary);
		}

		/// <summary>
		/// Opens a stream to the file specified by the URI
		/// </summary>
		/// <param name="uri">FTP/FTPS URI pointing at a file</param>
		/// <returns>Stream object</returns>
		/// <example><code source="..\Examples\OpenAppendURI.cs" lang="cs" /></example>
		public static Stream OpenAppend(Uri uri) {
			return OpenAppend(uri, true, FtpDataType.Binary);
		}

		private static void CheckURI(Uri uri) {

			if (uri.PathAndQuery == null || uri.PathAndQuery.Length == 0) {
				throw new UriFormatException("The supplied URI does not contain a valid path.");
			}

			if (uri.PathAndQuery.EndsWith("/")) {
				throw new UriFormatException("The supplied URI points at a directory.");
			}
		}

        /// <summary>
        /// Static method used to resolve internet IP
        /// </summary>
        /// <returns>ip</returns>
	    public static string GetPublicIP()
	    {
#if NETFX
            var request = WebRequest.Create("https://api.ipify.org/");
	        request.Method = "GET";

            using (var response = request.GetResponse())
            {
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    return stream.ReadToEnd();
                }
            }
#endif
            return null;
        }

#endregion

	}
}