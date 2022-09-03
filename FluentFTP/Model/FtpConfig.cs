using FluentFTP.Client.BaseClient;
using FluentFTP.Servers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP {

#if NETFRAMEWORK
	[Serializable]
#endif
	/// <summary>
	/// Holds all the configuration settings for a single FTP client.
	/// One FtpConfig object can only be bound to one client at a time.
	/// If you want to reuse it across multiple FTP clients, then clone it and then reuse it.
	/// </summary>
	public class FtpConfig {

		private BaseFtpClient _client = null;

		/// <summary>
		/// Which FtpClient are we bound to?
		/// </summary>
		public BaseFtpClient Client {
			get => _client;
		}

		private bool _logToConsole = false;

		/// <summary>
		/// Should the function calls be logged in Verbose mode?
		/// </summary>
		public bool LogToConsole {
			get => _logToConsole;
			set => _logToConsole = value;
		}

		private bool _logHost = false;

		/// <summary>
		/// Should the FTP server host IP/domain be shown in the logs (true) or masked out (false)?
		/// </summary>
		public bool LogHost {
			get => _logHost;
			set => _logHost = value;
		}

		private bool _logCredentials = false;

		/// <summary>
		/// Should the FTP credentials be shown in the logs (true) or masked out (false)?
		/// </summary>
		public bool LogCredentials {
			get => _logCredentials;
			set => _logCredentials = value;
		}

		protected FtpIpVersion _ipVersions = FtpIpVersion.ANY;

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
			get => _ipVersions;
			set => _ipVersions = value;
		}

		protected int _socketPollInterval = 15000;

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
			get => _socketPollInterval;
			set {
				_socketPollInterval = value;

				// set this value on the FtpClient's base stream
				if (_client != null) {
					var stream = ((IInternalFtpClient)_client).GetBaseStream();
					if (stream != null) {
						stream.SocketPollInterval = value;
					}
				}
			}
		}

		protected bool _staleDataTest = true;

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
			get => _staleDataTest;
			set => _staleDataTest = value;
		}

		protected int _noopInterval = 0;

		/// <summary>
		/// Gets or sets the length of time in milliseconds after last command
		/// (NOOP or other) that a NOOP command is sent by <see cref="Noop"/>.
		/// This is called during downloading/uploading if
		/// <see cref="EnableThreadSafeDataConnections"/> is false. Setting this
		/// interval to 0 disables <see cref="Noop"/> all together.
		/// The default value is 0 (disabled).
		/// </summary>
		public int NoopInterval {
			get => _noopInterval;
			set => _noopInterval = value;
		}

		protected bool _checkCapabilities = true;

		/// <summary>
		/// When this value is set to true (default) the control connection
		/// will set which features are available by executing the FEAT command
		/// when the connect method is called.
		/// </summary>
		public bool CheckCapabilities {
			get => _checkCapabilities;
			set => _checkCapabilities = value;
		}

		protected X509CertificateCollection _clientCerts = new X509CertificateCollection();

		/// <summary>
		/// Client certificates to be used in SSL authentication process
		/// </summary>
		public X509CertificateCollection ClientCertificates {
			get => _clientCerts;
			protected set => _clientCerts = value;
		}

		// Holds the cached resolved address
		protected string _Address;

		protected Func<string> _AddressResolver;

		/// <summary>
		/// Delegate used for resolving local address, used for active data connections
		/// This can be used in case you're behind a router, but port forwarding is configured to forward the
		/// ports from your router to your internal IP. In that case, we need to send the router's IP instead of our internal IP.
		/// </summary>
		public Func<string> AddressResolver {
			get => _AddressResolver;
			set => _AddressResolver = value;
		}

		protected IEnumerable<int> _ActivePorts;

		/// <summary>
		/// Ports used for Active Data Connection.
		/// Useful when your FTP server has certain ports that are blocked or used for other purposes.
		/// </summary>
		public IEnumerable<int> ActivePorts {
			get => _ActivePorts;
			set => _ActivePorts = value;
		}

		protected IEnumerable<int> _PassiveBlockedPorts;

		/// <summary>
		/// Ports blocked for Passive Data Connection (PASV and EPSV).
		/// Useful when your FTP server has certain ports that are blocked or used for other purposes.
		/// </summary>
		public IEnumerable<int> PassiveBlockedPorts {
			get => _PassiveBlockedPorts;
			set => _PassiveBlockedPorts = value;
		}

		protected int _PassiveMaxAttempts = 100;

		/// <summary>
		/// Maximum number of passive connections made in order to find a working port for Passive Data Connection (PASV and EPSV).
		/// Only used if PassiveBlockedPorts is non-null.
		/// </summary>
		public int PassiveMaxAttempts {
			get => _PassiveMaxAttempts;
			set => _PassiveMaxAttempts = value;
		}

		protected FtpDataConnectionType _dataConnectionType = FtpDataConnectionType.AutoPassive;

		/// <summary>
		/// Data connection type, default is AutoPassive which tries
		/// a connection with EPSV first and if it fails then tries
		/// PASV before giving up. If you know exactly which kind of
		/// connection you need you can slightly increase performance
		/// by defining a specific type of passive or active data
		/// connection here.
		/// </summary>
		public FtpDataConnectionType DataConnectionType {
			get => _dataConnectionType;
			set => _dataConnectionType = value;
		}

		protected bool _DisconnectWithQuit = true;

		/// <summary>
		/// Disconnect from the server without sending QUIT. This helps
		/// work around IOExceptions caused by buggy connection resets
		/// when closing the control connection.
		/// </summary>
		public bool DisconnectWithQuit {
			get => _DisconnectWithQuit;
			set => _DisconnectWithQuit = value;
		}

		protected bool _DisconnectWithShutdown = false;

		/// <summary>
		/// Before we disconnect from the server, send the Shutdown signal on the socket stream.
		/// </summary>
		public bool DisconnectWithShutdown {
			get => _DisconnectWithShutdown;
			set => _DisconnectWithShutdown = value;
		}

		protected int _connectTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds to wait for a connection 
		/// attempt to succeed before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int ConnectTimeout {
			get => _connectTimeout;
			set => _connectTimeout = value;
		}

		protected int _readTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time wait in milliseconds for data to be
		/// read from the underlying stream. The default value is 15000 (15 seconds).
		/// </summary>
		public int ReadTimeout {
			get => _readTimeout;
			set => _readTimeout = value;
		}

		protected int _dataConnectionConnectTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds for a data connection
		/// to be established before giving up. Default is 15000 (15 seconds).
		/// </summary>
		public int DataConnectionConnectTimeout {
			get => _dataConnectionConnectTimeout;
			set => _dataConnectionConnectTimeout = value;
		}

		protected int _dataConnectionReadTimeout = 15000;

		/// <summary>
		/// Gets or sets the length of time in milliseconds the data channel
		/// should wait for the server to send data. Default value is 
		/// 15000 (15 seconds).
		/// </summary>
		public int DataConnectionReadTimeout {
			get => _dataConnectionReadTimeout;
			set => _dataConnectionReadTimeout = value;
		}

		protected bool _keepAlive = false;

		/// <summary>
		/// Gets or sets a value indicating if <see cref="System.Net.Sockets.SocketOptionName.KeepAlive"/> should be set on 
		/// the underlying stream's socket. If the connection is alive, the option is
		/// adjusted in real-time. The value is stored and the KeepAlive option is set
		/// accordingly upon any new connections. The value set here is also applied to
		/// all future data streams. It has no affect on cloned control connections or
		/// data connections already in progress. The default value is false.
		/// </summary>
		public bool SocketKeepAlive {
			get => _keepAlive;
			set {
				_keepAlive = value;

				// set this value on the FtpClient's base stream
				if (_client != null) {
					var stream = ((IInternalFtpClient)_client).GetBaseStream();
					if (stream != null) {
						stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, value);
					}
				}
			}
		}

		protected FtpEncryptionMode _encryptionmode = FtpEncryptionMode.None;

		/// <summary>
		/// Type of SSL to use, or none. Default is none. Explicit is TLS, Implicit is SSL.
		/// </summary>
		public FtpEncryptionMode EncryptionMode {
			get => _encryptionmode;
			set => _encryptionmode = value;
		}

		protected bool _dataConnectionEncryption = true;

		/// <summary>
		/// Indicates if data channel transfers should be encrypted. Only valid if <see cref="EncryptionMode"/>
		/// property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// </summary>
		public bool DataConnectionEncryption {
			get => _dataConnectionEncryption;
			set => _dataConnectionEncryption = value;
		}

		protected SslProtocols _SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
		
		/// <summary>
		/// Encryption protocols to use. Only valid if EncryptionMode property is not equal to <see cref="FtpEncryptionMode.None"/>.
		/// Default value is .NET Framework defaults from the <see cref="System.Net.Security.SslStream"/> class.
		/// </summary>
		public SslProtocols SslProtocols {
			get => _SslProtocols;
			set => _SslProtocols = value;
		}

		protected FtpsBuffering _SslBuffering = FtpsBuffering.Auto;

		/// <summary>
		/// Whether to use SSL Buffering to speed up data transfer during FTP operations.
		/// SSL Buffering is always disabled on .NET 5.0 and later due to platform issues (see issue 682 in FluentFTP issue tracker).
		/// </summary>
		public FtpsBuffering SslBuffering {
			get => _SslBuffering;
			set => _SslBuffering = value;
		}

		protected bool _ValidateAnyCertificate = false;

		/// <summary>
		/// Accept any SSL certificate received from the server and skip performing
		/// the validation using the ValidateCertificate callback.
		/// Useful for Powershell users.
		/// </summary>
		public bool ValidateAnyCertificate {
			get => _ValidateAnyCertificate;
			set => _ValidateAnyCertificate = value;
		}

		protected bool _ValidateCertificateRevocation = false;

		/// <summary>
		/// Indicates if the certificate revocation list is checked during authentication.
		/// Useful when you need to maintain the certificate chain validation,
		/// but skip the certificate revocation check.
		/// WARNING: Enabling this can cause memory leaks in some conditions (see issue #710 for details).
		/// </summary>
		public bool ValidateCertificateRevocation {
			get => _ValidateCertificateRevocation;
			set => _ValidateCertificateRevocation = value;
		}

		protected FtpDataType _ListingDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the file listings are downloaded in Binary or ASCII mode.
		/// </summary>
		public FtpDataType ListingDataType {
			get => _ListingDataType;
			set => _ListingDataType = value;
		}

		protected FtpParser _parser = FtpParser.Auto;

		/// <summary>
		/// File listing parser to be used. 
		/// Automatically calculated based on the type of the server at the time of connection.
		/// If you want to override this property, make sure to do it after calling Connect.
		/// </summary>
		public FtpParser ListingParser {
			get => _parser;
			set {
				_parser = value;

				// configure parser
				if (_client != null) {
					((IInternalFtpClient)_client).SetListingParser(value);
				}
			}
		}

		protected CultureInfo _parserCulture = CultureInfo.InvariantCulture;

		/// <summary>
		/// Culture used to parse file listings
		/// </summary>
		public CultureInfo ListingCulture {
			get => _parserCulture;
			set => _parserCulture = value;
		}

		protected CustomParser _customParser = null;

		/// <summary>
		/// Custom file listing parser to be used.
		/// </summary>
		public CustomParser ListingCustomParser {
			get => _customParser;
			set {
				_customParser = value;

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
		public delegate FtpListItem CustomParser(string line, List<FtpCapability> capabilities, BaseFtpClient client);

		protected double _serverTimeZone = 0;
		protected TimeSpan _serverTimeOffset = new TimeSpan();

		/// <summary>
		/// The timezone of the FTP server. If the server is in Tokyo with UTC+9 then set this to 9.
		/// If the server returns timestamps in UTC then keep this 0.
		/// </summary>
		public double TimeZone {
			get => _serverTimeZone;
			set {
				if (value < -14 || value > 14) {
					throw new ArgumentOutOfRangeException(nameof(value), "TimeZone must be within -14 to +14 to represent UTC-14 to UTC+14");
				}
				_serverTimeZone = value;

				// configure parser
				if (value == 0) {
					_serverTimeOffset = TimeSpan.Zero;
				}
				else {
					var hours = (int)Math.Floor(_serverTimeZone);
					var mins = (int)Math.Floor((_serverTimeZone - Math.Floor(_serverTimeZone)) * 60);
					_serverTimeOffset = new TimeSpan(hours, mins, 0);
				}
			}
		}

		public TimeSpan GetServerTimeOffset() {
			return _serverTimeOffset;
		}


#if NETSTANDARD
		protected double _localTimeZone = 0;
		protected TimeSpan _localTimeOffset = new TimeSpan();

		/// <summary>
		/// The timezone of your machine. If your machine is in Tokyo with UTC+9 then set this to 9.
		/// If your machine is synchronized with UTC then keep this 0.
		/// </summary>
		public double LocalTimeZone {
			get => _localTimeZone;
			set {
				if (value < -14 || value > 14) {
					throw new ArgumentOutOfRangeException(nameof(value), "LocalTimeZone must be within -14 to +14 to represent UTC-14 to UTC+14");
				}
				_localTimeZone = value;

				// configure parser
				if (value == 0) {
					_localTimeOffset = TimeSpan.Zero;
				}
				else {
					var hours = (int)Math.Floor(_localTimeZone);
					var mins = (int)Math.Floor((_localTimeZone - Math.Floor(_localTimeZone)) * 60);
					_localTimeOffset = new TimeSpan(hours, mins, 0);
				}
			}
		}
		public TimeSpan GetLocalTimeOffset() {
			return _localTimeOffset;
		}
#endif

		protected FtpDate _timeConversion = FtpDate.ServerTime;

		/// <summary>
		/// Server timestamps are converted into the given timezone.
		/// ServerTime will return the original timestamp.
		/// LocalTime will convert the timestamp into your local machine's timezone.
		/// UTC will convert the timestamp into UTC format (GMT+0).
		/// You need to set TimeZone and LocalTimeZone (.NET core only) for these to work.
		/// </summary>
		public FtpDate TimeConversion {
			get => _timeConversion;
			set {
				_timeConversion = value;
			}
		}

		protected bool _bulkListing = true;

		/// <summary>
		/// If true, increases performance of GetListing by reading multiple lines
		/// of the file listing at once. If false then GetListing will read file
		/// listings line-by-line. If GetListing is having issues with your server,
		/// set it to false.
		/// 
		/// The number of bytes read is based upon <see cref="BulkListingLength"/>.
		/// </summary>
		public bool BulkListing {
			get => _bulkListing;
			set => _bulkListing = value;
		}

		protected int _bulkListingLength = 128;

		/// <summary>
		/// Bytes to read during GetListing. Only honored if <see cref="BulkListing"/> is true.
		/// </summary>
		public int BulkListingLength {
			get => _bulkListingLength;
			set => _bulkListingLength = value;
		}

		protected int? _transferChunkSize;

		/// <summary>
		/// Gets or sets the number of bytes transferred in a single chunk (a single FTP command).
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// to transfer large files in multiple chunks.
		/// </summary>
		public int TransferChunkSize {
			get => _transferChunkSize ?? 65536;
			set => _transferChunkSize = value;
		}

		protected int? _localFileBufferSize;

		/// <summary>
		/// Gets or sets the size of the file buffer when reading and writing files on the local file system.
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// and all the other file and directory transfer methods.
		/// </summary>
		public int LocalFileBufferSize {
			get => _localFileBufferSize ?? 4096;
			set => _localFileBufferSize = value;
		}

		protected int _retryAttempts = 3;

		/// <summary>
		/// Gets or sets the retry attempts allowed when a verification failure occurs during download or upload.
		/// This value must be set to 1 or more.
		/// </summary>
		public int RetryAttempts {
			get => _retryAttempts;
			set => _retryAttempts = value > 0 ? value : 1;
		}

		protected uint _uploadRateLimit = 0;

		/// <summary>
		/// Rate limit for uploads in kbyte/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint UploadRateLimit {
			get => _uploadRateLimit;
			set => _uploadRateLimit = value;
		}

		protected uint _downloadRateLimit = 0;

		/// <summary>
		/// Rate limit for downloads in kbytes/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint DownloadRateLimit {
			get => _downloadRateLimit;
			set => _downloadRateLimit = value;
		}

		protected bool _DownloadZeroByteFiles = true;

		/// <summary>
		/// Controls if zero-byte files should be downloaded or skipped.
		/// If false, then no file is created/overwritten into the filesystem.
		/// </summary>
		public bool DownloadZeroByteFiles {
			get => _DownloadZeroByteFiles;
			set => _DownloadZeroByteFiles = value;
		}

		protected FtpDataType _UploadDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the high-level API uploads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType UploadDataType {
			get => _UploadDataType;
			set => _UploadDataType = value;
		}

		protected FtpDataType _DownloadDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the high-level API downloads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType DownloadDataType {
			get => _DownloadDataType;
			set => _DownloadDataType = value;
		}

		protected bool _UploadDirectoryDeleteExcluded = true;

		/// <summary>
		/// Controls if the UploadDirectory API deletes the excluded files when uploading in Mirror mode.
		/// If true, then any files that are excluded will be deleted from the FTP server if they are
		/// excluded from the local system. This is done to keep the server in sync with the local system.
		/// But if it is false, the excluded files are not touched on the server, and simply ignored.
		/// </summary>
		public bool UploadDirectoryDeleteExcluded {
			get => _UploadDirectoryDeleteExcluded;
			set => _UploadDirectoryDeleteExcluded = value;
		}

		protected bool _DownloadDirectoryDeleteExcluded = true;

		/// <summary>
		/// Controls if the DownloadDirectory API deletes the excluded files when downloading in Mirror mode.
		/// If true, then any files that are excluded will be deleted from the local filesystem if they are
		/// excluded from the FTP server. This is done to keep the local filesystem in sync with the FTP server.
		/// But if it is false, the excluded files are not touched on the local filesystem, and simply ignored.
		/// </summary>
		public bool DownloadDirectoryDeleteExcluded {
			get => _DownloadDirectoryDeleteExcluded;
			set => _DownloadDirectoryDeleteExcluded = value;
		}

		protected FtpDataType _FXPDataType = FtpDataType.Binary;

		/// <summary>
		/// Controls if the FXP server-to-server file transfer API uses Binary or ASCII mode.
		/// </summary>
		public FtpDataType FXPDataType {
			get => _FXPDataType;
			set => _FXPDataType = value;
		}

		protected int _FXPProgressInterval = 1000;

		/// <summary>
		/// Controls how often the progress reports are sent during an FXP file transfer.
		/// The default value is 1000 (1 second).
		/// </summary>
		public int FXPProgressInterval {
			get => _FXPProgressInterval;
			set => _FXPProgressInterval = value;
		}

		protected bool _SendHost;
		/// <summary>
		/// Controls if the HOST command is sent immediately after the handshake.
		/// Useful when you are using shared hosting and you need to inform the
		/// FTP server which domain you want to connect to.
		/// </summary>
		public bool SendHost {
			get => _SendHost;
			set => _SendHost = value;
		}

		protected string _SendHostDomain = null;
		/// <summary>
		/// Controls which domain is sent with the HOST command.
		/// If this is null, then the Host parameter of the FTP client is sent.
		/// </summary>
		public string SendHostDomain {
			get => _SendHostDomain;
			set => _SendHostDomain = value;
		}

		protected IPAddress _SocketLocalIp;
		/// <summary>
		/// The local socket will be bound to the given local IP/interface.
		/// This is useful if you have several usable public IP addresses and want to use a particular one.
		/// </summary>
		public IPAddress SocketLocalIp {
			get => _SocketLocalIp;
			set => _SocketLocalIp = value;
		}



		//-------------------------------------------------------------//
		// ADD NEW PROPERTIES INTO THIS FUNCTION: FtpConfig.CopyTo()
		//-------------------------------------------------------------//




		/// <summary>
		/// Bind this FtpConfig object to the given FTP client.
		/// </summary>
		/// <param name="client"></param>
		public void BindTo(BaseFtpClient client) {
			_client = client;
		}

		/// <summary>
		/// Return a deep clone of this FtpConfig object.
		/// </summary>
		public FtpConfig Clone() {
			var cloned = new FtpConfig();
			CopyTo(this, cloned);
			return cloned;
		}

		/// <summary>
		/// Copy settings from one config object to another.
		/// </summary>
		public static void CopyTo(FtpConfig read, FtpConfig write) {

			// copy settings
			write.LogToConsole = read.LogToConsole;
			write.InternetProtocolVersions = read.InternetProtocolVersions;
			write.SocketPollInterval = read.SocketPollInterval;
			write.StaleDataCheck = read.StaleDataCheck;
			write.NoopInterval = read.NoopInterval;
			write.DataConnectionType = read.DataConnectionType;
			write.DisconnectWithQuit = read.DisconnectWithQuit;
			write.DisconnectWithShutdown = read.DisconnectWithShutdown;
			write.ConnectTimeout = read.ConnectTimeout;
			write.ReadTimeout = read.ReadTimeout;
			write.DataConnectionConnectTimeout = read.DataConnectionConnectTimeout;
			write.DataConnectionReadTimeout = read.DataConnectionReadTimeout;
			write.SocketKeepAlive = read.SocketKeepAlive;
			write.EncryptionMode = read.EncryptionMode;
			write.DataConnectionEncryption = read.DataConnectionEncryption;
			write.SslProtocols = read.SslProtocols;
			write.SslBuffering = read.SslBuffering;
			write.TransferChunkSize = read.TransferChunkSize;
			write.LocalFileBufferSize = read.LocalFileBufferSize;
			write.ListingDataType = read.ListingDataType;
			write.ListingParser = read.ListingParser;
			write.ListingCulture = read.ListingCulture;
			write.ListingCustomParser = read.ListingCustomParser;
			write.TimeZone = read.TimeZone;
			write.TimeConversion = read.TimeConversion;
			write.RetryAttempts = read.RetryAttempts;
			write.UploadRateLimit = read.UploadRateLimit;
			write.DownloadZeroByteFiles = read.DownloadZeroByteFiles;
			write.DownloadRateLimit = read.DownloadRateLimit;
			write.DownloadDataType = read.DownloadDataType;
			write.UploadDataType = read.UploadDataType;
			write.ActivePorts = read.ActivePorts;
			write.PassiveBlockedPorts = read.PassiveBlockedPorts;
			write.PassiveMaxAttempts = read.PassiveMaxAttempts;
			write.SendHost = read.SendHost;
			write.SendHostDomain = read.SendHostDomain;
			write.FXPDataType = read.FXPDataType;
			write.FXPProgressInterval = read.FXPProgressInterval;
			write.UploadDirectoryDeleteExcluded = read.UploadDirectoryDeleteExcluded;
			write.DownloadDirectoryDeleteExcluded = read.DownloadDirectoryDeleteExcluded;
			write.SocketLocalIp = read.SocketLocalIp;

#if NETSTANDARD
			write.LocalTimeZone = read.LocalTimeZone;
#endif

			// copy certificates from self
			write.ClientCertificates.Clear();
			write.ClientCertificates.AddRange(read.ClientCertificates);

		}


	}
}