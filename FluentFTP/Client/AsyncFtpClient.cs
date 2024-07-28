using System;
using System.Net;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;

namespace FluentFTP {

	/// <summary>
	/// An FTP client that manages a connection to a single FTP server.
	/// Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// Uses asynchronous operations only. For the sync version use `FtpClient`.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. Visit our Github Wiki for more info.
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	// IAsyncDisposable can be used
	public partial class AsyncFtpClient : BaseFtpClient, IInternalFtpClient, IDisposable, IAsyncDisposable, IAsyncFtpClient {
#else
	// IAsyncDisposable is not available
	public partial class AsyncFtpClient : BaseFtpClient, IInternalFtpClient, IDisposable, IAsyncFtpClient {
#endif

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		#region Constructors

		/// <summary>
		/// Creates a new instance of an async FTP Client. You will have to setup the FTP host and credentials before connecting.
		/// </summary>
		public AsyncFtpClient() : base(null) {
		}

		/// <summary>
		/// Creates a new instance of an async FTP Client, with the given host and credentials.
		/// </summary>
		public AsyncFtpClient(string host, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

			// set host
			Host = host ?? throw new ArgumentNullException(nameof(host));

			// set port
			if (port > 0) {
				Port = port;
			}

			// set logger
			Logger = logger;
		}

		/// <summary>
		/// Creates a new instance of an async FTP Client, with the given host and credentials.
		/// </summary>
		public AsyncFtpClient(string host, string user, string pass, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

			// set host
			Host = host ?? throw new ArgumentNullException(nameof(host));

			// set credentials
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (pass == null) throw new ArgumentNullException(nameof(pass));
			Credentials = new NetworkCredential(user, pass);

			// set port
			if (port > 0) {
				Port = port;
			}

			// set logger
			Logger = logger;
		}

		/// <summary>
		/// Creates a new instance of an async FTP Client, with the given host and credentials.
		/// </summary>
		public AsyncFtpClient(string host, NetworkCredential credentials, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

			// set host
			Host = host ?? throw new ArgumentNullException(nameof(host));

			// set credentials
			Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

			// set port
			if (port > 0) {
				Port = port;
			}

			// set logger
			Logger = logger;
		}

		protected override BaseFtpClient Create() {
			return new AsyncFtpClient();
		}
		#endregion

		#region Destructor
		public override void Dispose() {
			LogFunction(nameof(Dispose));
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
			// This can happen when someone is "using" the async client without specifying "await using"
			LogWithPrefix(FtpTraceLevel.Verbose, "Warning: sync dispose called for " + this.ClientType + " object");
#endif
			DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		public async ValueTask DisposeAsync() {
#else
		public async Task DisposeAsync() {
#endif
			if (IsDisposed) {
				return;
			}

			LogFunction(nameof(DisposeAsync));
			LogWithPrefix(FtpTraceLevel.Verbose, "Disposing(async) " + this.ClientType);

			await DisposeAsyncCore();

			await Task.Run(() => {
				WaitForDaemonTermination();
			});

			IsDisposed = true;

			GC.SuppressFinalize(this);
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		protected virtual async ValueTask DisposeAsyncCore() {
#else
		protected virtual async Task DisposeAsyncCore() {
#endif
			try {
				if (IsConnected) {
					await Disconnect();
				}
			}
			catch {
			}

			if (m_stream != null) {
				try {
					await m_stream.DisposeAsync();
				}
				catch {
				}
				finally {
					m_stream = null;
				}
			}

			m_credentials = null;
			m_textEncoding = null;
			m_host = null;
		}

		#endregion

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}

