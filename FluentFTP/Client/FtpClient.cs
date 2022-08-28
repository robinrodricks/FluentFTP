using System;
using System.Net;
using FluentFTP.Client.BaseClient;
using Microsoft.Extensions.Logging;

namespace FluentFTP {

	/// <summary>
	/// An FTP client that manages a connection to a single FTP server.
	/// Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// Uses synchronous operations only. For the async version use `AsyncFtpClient`.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. Visit our Github Wiki for more info.
	/// </summary>
	public partial class FtpClient : BaseFtpClient, IInternalFtpClient, IDisposable, IFtpClient {

		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClient();
		}

		#region Constructors

		/// <summary>
		/// Creates a new instance of a synchronous FTP Client. You will have to setup the FTP host and credentials before connecting.
		/// </summary>
		public FtpClient() : base(null) {
		}

		/// <summary>
		/// Creates a new instance of a synchronous FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, string user, string pass, FtpConfig config = null, ILogger logger = null) : base(config) {
			Host = host;
			if (user != null && pass != null) {
				Credentials = new NetworkCredential(user, pass);
			}
			Logger = logger;
		}

		/// <summary>
		/// Creates a new instance of a synchronous FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials, FtpConfig config = null, ILogger logger = null) : base(config) {
			Host = host;
			Credentials = credentials;
			Logger = logger;
		}

		#endregion

		#region Destructor

		void IInternalFtpClient.DisconnectInternal() {
			Disconnect();
		}

		void IInternalFtpClient.ConnectInternal() {
			Connect();
		}

		#endregion

	}
}
