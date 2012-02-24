using System.Net.Sockets;

namespace System.Net.FtpClient.Proxy {
	/// <summary>
	/// Implements a Socket class that can connect through a proxy server.
	/// </summary>
	public class ProxySocket : Socket {
		#region Fields

		/// <summary>
		/// The proxy server endpoint value.
		/// </summary>
		private IPEndPoint proxyEndPoint;

		/// <summary>
		/// The proxy server type value.
		/// </summary>
		private ProxyType proxyType;

		/// <summary>
		/// The username used to connect to the proxy server.
		/// </summary>
		private string proxyUsername;

		/// <summary>
		/// The password used to connect to the proxy server.
		/// </summary>
		private string proxyPassword;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the ProxySocket class.
		/// </summary>
		/// <param name="addressFamily">One of the AddressFamily values.</param>
		/// <param name="socketType">One of the SocketType values.</param>
		/// <param name="protocolType">One of the ProtocolType values.</param>
		public ProxySocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
			: base(addressFamily, socketType, protocolType) {
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the ProxyEndPoint.
		/// </summary>
		public IPEndPoint ProxyEndPoint {
			get { return this.proxyEndPoint; }
			set { this.proxyEndPoint = value; }
		}

		/// <summary>
		/// Gets or sets the ProxyType.
		/// </summary>
		public ProxyType ProxyType {
			get { return this.proxyType; }
			set { this.proxyType = value; }
		}

		/// <summary>
		/// Gets or sets the proxy server username.
		/// </summary>
		public string ProxyUsername {
			get { return this.proxyUsername; }
			set { this.proxyUsername = value; }
		}

		/// <summary>
		/// Gets or sets the proxy server password.
		/// </summary>
		public string ProxyPassword {
			get { return this.proxyPassword; }
			set { this.proxyPassword = value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Establishes a connection to a remote host. The host is specified by an IP address and a port number.
		/// </summary>
		/// <param name="host">The IP address of the remote host.</param>
		/// <param name="port">The port number of the remote host.</param>
		public new void Connect(string host, int port) {
			if(host == null) {
				throw new ArgumentNullException("host");
			}

			if(port <= 0 || port > 65535) {
				throw new ArgumentOutOfRangeException("port", "port must be greater than zero and less than 65535");
			}

			if(this.ProtocolType != ProtocolType.Tcp || this.ProxyType == ProxyType.None || this.ProxyEndPoint == null) {
				base.Connect(host, port);
			}
			else {
				base.Connect(this.ProxyEndPoint);
				ProxyBase proxy = null;
				if(this.ProxyType == ProxyType.Socks4) {
					proxy = new Socks4Proxy(this, this.ProxyUsername);
				}
				else if(this.ProxyType == ProxyType.Socks4a) {
					proxy = new Socks4AProxy(this, this.ProxyUsername);
				}
				else if(this.ProxyType == ProxyType.Socks5) {
					proxy = new Socks5Proxy(this, this.ProxyUsername, this.ProxyPassword);
				}

				if(proxy != null) {
					proxy.Connect(host, port);
				}
			}
		}

		#endregion
	}
}
