using System;
using System.Net;
using FluentFTP.Client.BaseClient;
using Microsoft.Extensions.Logging;

namespace FluentFTP {

	/// <summary>
	/// An FTP client that manages a connection to a single FTP server.
	/// Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// Uses asynchronous operations only. For the sync version use `FtpClient`.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. Visit our Github Wiki for more info.
	/// </summary>
	public partial class AsyncFtpClient : BaseFtpClient, IInternalFtpClient, IDisposable, IAsyncFtpClient {

		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClient();
		}

		#region Constructors

		/// <summary>
		/// Creates a new instance of an async FTP Client. You will have to setup the FTP host and credentials before connecting.
		/// </summary>
		public AsyncFtpClient() : base(null) {
		}

		/// <summary>
		/// Creates a new instance of an async FTP Client, with the given host and credentials.
		/// </summary>
		public AsyncFtpClient(string host, int port = 0, FtpConfig config = null, ILogger logger = null) : base(config) {

			// set host
			Host = host ?? throw new ArgumentNullException("host");

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
		public AsyncFtpClient(string host, string user, string pass, int port = 0, FtpConfig config = null, ILogger logger = null) : base(config) {
			
			// set host
			Host = host ?? throw new ArgumentNullException("host");

			// set credentials
			if (user == null) throw new ArgumentNullException("user");
			if (pass == null) throw new ArgumentNullException("pass");
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
		public AsyncFtpClient(string host, NetworkCredential credentials, int port = 0, FtpConfig config = null, ILogger logger = null) : base(config) {

			// set host
			Host = host ?? throw new ArgumentNullException("host");

			// set credentials
			Credentials = credentials ?? throw new ArgumentNullException("credentials");
			
			// set port
			if (port > 0) {
				Port = port;
			}

			// set logger
			Logger = logger;
		}

		#endregion

		#region Destructor

		void IInternalFtpClient.DisconnectInternal() {
			// TODO: Call DisconnectAsync
		}

		void IInternalFtpClient.ConnectInternal() {
			// TODO: Call ConnectAsync
		}

		#endregion

	}
}
