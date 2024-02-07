using System;
using System.Net;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;

namespace FluentFTP {

	/// <summary>
	/// An FTP client that manages a connection to a single FTP server.
	/// Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// Uses synchronous operations only. For the async version use `AsyncFtpClient`.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. Visit our Github Wiki for more info.
	/// </summary>
	public partial class FtpClient : BaseFtpClient, IInternalFtpClient, IDisposable, IFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		#region Constructors

		/// <summary>
		/// Creates a new instance of a synchronous FTP Client. You will have to setup the FTP host and credentials before connecting.
		/// </summary>
		public FtpClient() : base(null) {
		}

		/// <summary>
		/// Creates a new instance of a synchronous FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

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
		/// Creates a new instance of a synchronous FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, string user, string pass, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

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
		/// Creates a new instance of a synchronous FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials, int port = 0, FtpConfig config = null, IFtpLogger logger = null) : base(config) {

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

		#endregion

		#region Destructor

		#endregion

		protected override BaseFtpClient Create() {
			return new FtpClient();
		}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}
