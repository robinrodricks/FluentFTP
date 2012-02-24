using System.Net.Sockets;
using System.Text;

namespace System.Net.FtpClient.Proxy {
	/// <summary>
	/// Implements the SOCKS4 protocol.
	/// </summary>
	public class Socks4Proxy : ProxyBase {
		#region Fields

		/// <summary>
		/// The SOCKS4 version number.
		/// </summary>
		private const byte Socks4VersionNumber = 4;

		/// <summary>
		/// The SOCKS4 connect command value.
		/// </summary>
		private const byte Socks4ConnectCommand = 1;

		/// <summary>
		/// The SOCKS4 connect command succeed value.
		/// </summary>
		private const int Socks4CommandSucceed = 90;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the Socks4Proxy class.
		/// </summary>
		/// <param name="socket">The socket connection with the proxy server.</param>
		/// <param name="username">The username used to connect to the proxy server.</param>
		public Socks4Proxy(Socket socket, string username)
			: base(socket, username) {
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the remote host through the SOCKS proxy server.
		/// </summary>
		/// <param name="host">The remote server to connect to.</param>
		/// <param name="port">The remote port to connect to.</param>
		public override void Connect(string host, int port) {
			byte[] connectCommand = this.GetConnectCommand(host, port);
			this.Connect(connectCommand);
		}

		/// <summary>
		/// Connects to the remote host through the SOCKS proxy server.
		/// </summary>
		/// <param name="connect">The bytes to send when trying to connect.</param>
		private void Connect(byte[] connect) {
			this.Socket.Send(connect);

			byte[] buffer = new byte[8];
			int received = 0;
			while(received != 8) {
				received += this.Socket.Receive(buffer, received, 8 - received, SocketFlags.None);
			}

			if(buffer[1] != Socks4CommandSucceed) {
				this.Socket.Close();
			}
		}

		/// <summary>
		/// Converts an IP address to an array of bytes.
		/// </summary>
		/// <param name="host">The host name or IP address to convert.</param>
		/// <returns>An array of bytes representing the IP address.</returns>
		private byte[] GetHostBytes(string host) {
			IPAddress ipAddress = null;

			if(!IPAddress.TryParse(host, out ipAddress)) {
				try {
					ipAddress = Dns.GetHostEntry(host).AddressList[0];
				}
				catch(Exception) {
					throw;
				}
			}

			return ipAddress.GetAddressBytes();
		}

		/// <summary>
		/// Creates an array of bytes representing the SOCKS4 proxy server connect command.
		/// See http://ftp.icm.edu.pl/packages/socks/socks4/SOCKS4.protocol
		/// </summary>
		/// <param name="host">The host name or IP address to connect to.</param>
		/// <param name="port">The port number to connect to.</param>
		/// <returns>An array of bytes representing the connect command for the specified host/port combination.</returns>
		private byte[] GetConnectCommand(string host, int port) {
			byte[] addressBytes = this.GetHostBytes(host);
			byte[] portBytes = this.GetPortBytes(port);
			byte[] userBytes = ASCIIEncoding.ASCII.GetBytes(this.Username);

			byte[] connect = new byte[9 + userBytes.Length];
			connect[0] = Socks4VersionNumber;
			connect[1] = Socks4ConnectCommand;
			portBytes.CopyTo(connect, 2);
			addressBytes.CopyTo(connect, 4);
			userBytes.CopyTo(connect, 8);
			connect[8 + userBytes.Length] = 0;

			return connect;
		}

		#endregion
	}
}
