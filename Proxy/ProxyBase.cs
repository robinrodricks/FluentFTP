using System.Net.Sockets;

namespace System.Net.FtpClient.Proxy
{
    /// <summary>
    /// Implements the SOCKS protocol. This is an abstract class; it must be inherited.
    /// </summary>
    public abstract class ProxyBase
    {
        #region Fields

        /// <summary>
        /// The socket value.
        /// </summary>
        private Socket socket;

        /// <summary>
        /// The proxy server username.
        /// </summary>
        private string username;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProxyBase class.
        /// </summary>
        /// <param name="socket">The socket connection with the proxy server.</param>
        /// <param name="username">The username used to connect to the proxy server.</param>
        public ProxyBase(Socket socket, string username)
        {
            this.Socket = socket;
            this.Username = username;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the socket connection with the proxy server.
        /// </summary>
        protected Socket Socket
        {
            get
            {
                return this.socket;
            }

            set
            {
                if (value != null)
                {
                    this.socket = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the username used to connect to the proxy server.
        /// </summary>
        protected string Username
        {
            get
            {
                return this.username;
            }

            set
            {
                if (value != null)
                {
                    this.username = value;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connects to the remote host through the SOCKS proxy server.
        /// </summary>
        /// <param name="host">The remote server to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        public abstract void Connect(string host, int port);


        /// <summary>
        /// Converts a port number to an array of bytes.
        /// </summary>
        /// <param name="port">The port number to convert.</param>
        /// <returns>An array of two bytes representing the port number.</returns>
        protected byte[] GetPortBytes(int port)
        {
            byte[] array = new byte[2];
            array[0] = Convert.ToByte(port / 256);
            array[1] = Convert.ToByte(port % 256);
            return array;
        }

        /// <summary>
        /// Reads a number of bytes from the proxy server.
        /// </summary>
        /// <param name="number">The number of bytes to read.</param>
        /// <returns>An array of bytes containing the proxy server response.</returns>
        protected byte[] Read(int number)
        {
            byte[] buffer = new byte[number];
            int result = 0;

            while (result != number)
            {
                number += this.Socket.Receive(buffer, result, number - result, SocketFlags.None);
            }

            return buffer;
        }

        #endregion
    }
}
