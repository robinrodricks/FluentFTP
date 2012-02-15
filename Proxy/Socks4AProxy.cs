using System.Net.Sockets;
using System.Text;

namespace System.Net.FtpClient.Proxy
{
    /// <summary>
    /// Implements the SOCKS4A protocol.
    /// </summary>
    public class Socks4AProxy : ProxyBase
    {
        #region Fields

        /// <summary>
        /// The SOCKS4A version number.
        /// </summary>
        private const byte Socks4AVersionNumber = 4;

        /// <summary>
        /// The SOCKS4A connect command value.
        /// </summary>
        private const byte Socks4AConnectCommand = 1;

        /// <summary>
        /// The SOCKS4A connect command succeed value.
        /// </summary>
        private const int Socks4ACommandSucceed = 90;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Socks4AProxy class.
        /// </summary>
        /// <param name="socket">The socket connection with the proxy server.</param>
        /// <param name="username">The username used to connect to the proxy server.</param>
        public Socks4AProxy(Socket socket, string username)
            : base(socket, username)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connects to the remote host through the SOCKS proxy server.
        /// </summary>
        /// <param name="host">The remote server to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        public override void Connect(string host, int port)
        {
            this.Connect(GetConnectCommand(host, port));
        }

        /// <summary>
        /// Connects to the remote host through the SOCKS proxy server.
        /// </summary>
        /// <param name="connect">The bytes to send when trying to connect.</param>
        private void Connect(byte[] connect)
        {
            this.Socket.Send(connect);

            byte[] buffer = new byte[8];
            int received = 0;
            while (received != 8)
            {
                received += this.Socket.Receive(buffer, received, 8 - received, SocketFlags.None);
            }

            if (buffer[1] != Socks4ACommandSucceed)
            {
                this.Socket.Close();
            }
        }

        /// <summary>
        /// Creates an array of bytes representing the SOCKS4A proxy server connect command.
        /// See http://ss5.sourceforge.net/socks4A.protocol.txt
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <returns>An array of bytes representing the connect command for the specified host/port combination.</returns>
        private byte[] GetConnectCommand(string host, int port)
        {
            byte[] invalidBytes = { 0, 0, 0, 1 };
            byte[] addressBytes = ASCIIEncoding.ASCII.GetBytes(host);
            byte[] portBytes = this.GetPortBytes(port);
            byte[] userBytes = ASCIIEncoding.ASCII.GetBytes(this.Username);

            byte[] connect = new byte[10 + this.Username.Length + host.Length];
            connect[0] = Socks4AVersionNumber;
            connect[1] = Socks4AConnectCommand;
            portBytes.CopyTo(connect, 2);
            invalidBytes.CopyTo(connect, 4);
            userBytes.CopyTo(connect, 8);
            connect[8 + this.Username.Length] = 0;
            addressBytes.CopyTo(connect, 9 + this.Username.Length);
            connect[9 + this.Username.Length + host.Length] = 0;

            return connect;
        }

        #endregion
    }
}
