using System;
using System.Net;
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

		/// <summary>
		/// Creates a new instance of this class.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClient();
		}

		#region Constructors

		/// <summary>
		/// Creates a new instance of an FTP Client.
		/// </summary>
		public FtpClient() : base() {
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host.
		/// </summary>
		public FtpClient(string host) : base() {
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(string host, NetworkCredential credentials) : base() {
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials), "Credentials must be provided");
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(string host, int port, NetworkCredential credentials) : base() {
			Host = host ?? throw new ArgumentNullException(nameof(host), "Host must be provided");
			Port = port;
			Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials), "Credentials must be provided");
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, username and password.
		/// </summary>
		public FtpClient(string host, string user, string pass) : base() {
			Host = host;
			Credentials = new NetworkCredential(user, pass);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, username, password and account
		/// </summary>
		public FtpClient(string host, string user, string pass, string account) : base() {
			Host = host;
			Credentials = new NetworkCredential(user, pass, account);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port, username and password.
		/// </summary>
		public FtpClient(string host, int port, string user, string pass) : base() {
			Host = host;
			Port = port;
			Credentials = new NetworkCredential(user, pass);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port, username, password and account
		/// </summary>
		public FtpClient(string host, int port, string user, string pass, string account) : base() {
			Host = host;
			Port = port;
			Credentials = new NetworkCredential(user, pass, account);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host.
		/// </summary>
		public FtpClient(Uri host) : base() {
			Host = ValidateHost(host);
			Port = host.Port;
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, NetworkCredential credentials) : base() {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = credentials;
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, string user, string pass) : base() {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = new NetworkCredential(user, pass);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host and credentials.
		/// </summary>
		public FtpClient(Uri host, string user, string pass, string account) : base() {
			Host = ValidateHost(host);
			Port = host.Port;
			Credentials = new NetworkCredential(user, pass, account);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(Uri host, int port, string user, string pass) : base() {
			Host = ValidateHost(host);
			Port = port;
			Credentials = new NetworkCredential(user, pass);
			
		}

		/// <summary>
		/// Creates a new instance of an FTP Client, with the given host, port and credentials.
		/// </summary>
		public FtpClient(Uri host, int port, string user, string pass, string account) : base() {
			Host = ValidateHost(host);
			Port = port;
			Credentials = new NetworkCredential(user, pass, account);
			
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
