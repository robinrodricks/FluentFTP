using System.Net.Sockets;
using System.Text;

namespace System.Net.FtpClient.Proxy
{
    /// <summary>
    /// Implements the SOCKS5 protocol.
    /// </summary>
    public class Socks5Proxy : ProxyBase
    {
        #region Fields

        /// <summary>
        /// The password used to connect to the proxy server.
        /// </summary>
        private string password;

        /// <summary>
        /// The SOCKS5 version number.
        /// </summary>
        private const byte Socks5VersionNumber = 5;

        /// <summary>
        /// The SOCKS5 reserved field value.
        /// </summary>
        private const byte Socks5Reserved = 0;

        /// <summary>
        /// The SOCKS5 IPv4 address type.
        /// </summary>
        private const byte Socks5AddressTypeIPv4 = 1;

        /// <summary>
        /// The SOCKS5 Domain name address type.
        /// </summary>
        private const byte Socks5AddressTypeDomainName = 3;

        /// <summary>
        /// The SOCKS5 IPv6 address type.
        /// </summary>
        private const byte Socks5AddressTypeIPv6 = 4;

        /// <summary>
        /// Indicates that the SOCKS5 proxy server doesn't need authentication.
        /// </summary>
        private const byte Socks5NoAuthentication = 0;

        /// <summary>
        /// Indicates that the SOCKS5 proxy server need a username/password authentication.
        /// </summary>
        private const byte Socks5Authentication = 1;

        /// <summary>
        /// The authentication command success code.
        /// </summary>
        private const int Socks5AuthenticateCommandSucceed = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Socks5Proxy class.
        /// </summary>
        /// <param name="socket">The socket connectio with the proxy server.</param>
        /// <param name="username">The username used to connect to the proxy server.</param>
        /// <param name="password">The password used to connect to the proxy server.</param>
        public Socks5Proxy(Socket socket, string username, string password)
            : base(socket, username)
        {
            this.Password = password;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the password used to connect to the proxy server.
        /// </summary>
        private string Password
        {
            get
            {
                return this.password;
            }

            set
            {
                if (value != null)
                {
                    this.password = value;
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
        public override void Connect(string host, int port)
        {
            byte[] connectCommand = this.GetConnectCommand(host, port);
            this.Connect(connectCommand);
        }

        /// <summary>
        /// Authenticates to the proxy server.
        /// </summary>
        private void Authenticate()
        {
            byte[] auth = { 5, 2, 0, 2 };
            this.Socket.Send(auth);

            byte[] buffer = this.Read(2);
            if (buffer[1] != 255)
            {
                if (buffer[1] == Socks5Authentication)
                {
                    this.Socket.Send(this.GetAuthenticateCommand());
                    buffer = this.Read(2);
                    if (buffer[1] != Socks5AuthenticateCommandSucceed)
                    {
                        this.Socket.Close();
                    }
                }
                else if (buffer[1] == Socks5NoAuthentication)
                {
                    // No authentication.
                }
                else
                {
                    // Not valid.
                }
            }
        }

        /// <summary>
        /// Connects to the remote host through the SOCKS proxy server.
        /// </summary>
        /// <param name="connect">The bytes to send when trying to connect.</param>
        private void Connect(byte[] connect)
        {
            this.Authenticate();

            this.Socket.Send(connect);
            byte[] buffer = this.Read(4);
            if (buffer[1] != 0)
            {
                this.Socket.Close();
            }

            switch (buffer[3])
            {
                case Socks5AddressTypeIPv4:
                case Socks5AddressTypeDomainName:
                case Socks5AddressTypeIPv6:
                    // Everything fine, it respond with a right address type.
                    break;
                default:
                    this.Socket.Close();
                    break;
            }
        }

        /// <summary>
        /// Gets the SOCKS5 host type from the host name or IP address.
        /// </summary>
        /// <param name="host">The host name or IP address from which we need the type.</param>
        /// <returns>The corresponding SOCKS5 type.</returns>
        private byte GetHostType(string host)
        {
            IPAddress ipAddress = null;

            bool res = IPAddress.TryParse(host, out ipAddress);

            if (!res)
            {
                return Socks5AddressTypeDomainName;
            }

            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return Socks5AddressTypeIPv4;
                case AddressFamily.InterNetworkV6:
                    return Socks5AddressTypeIPv6;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// Gets the SOCKS5 address bytes, depending on the address type.
        /// </summary>
        /// <param name="addressType">The address type.</param>
        /// <param name="host">The host name or IP address.</param>
        /// <returns>An array of bytes representing the host.</returns>
        private byte[] GetAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case Socks5AddressTypeIPv4:
                case Socks5AddressTypeIPv6:
                    return IPAddress.Parse(host).GetAddressBytes();
                case Socks5AddressTypeDomainName:
                    byte[] bytes = new byte[host.Length + 1];
                    bytes[0] = Convert.ToByte(host.Length);
                    byte[] addressBytes = ASCIIEncoding.ASCII.GetBytes(host);
                    addressBytes.CopyTo(bytes, 1);
                    return bytes;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates an array of bytes representing the SOCKS5 proxy server connect command.
        /// See http://www.ietf.org/rfc/rfc1928.txt
        /// </summary>
        /// <param name="host">The host name or IP address to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <returns>An array of bytes representing the connect command for the specified host/port combination.</returns>
        private byte[] GetConnectCommand(string host, int port)
        {
            byte addressType = this.GetHostType(host);
            byte[] addressBytes = this.GetAddressBytes(addressType, host);
            byte[] portBytes = this.GetPortBytes(port);

            byte[] connect = new byte[7 + host.Length];
            connect[0] = Socks5VersionNumber;
            connect[1] = 1;
            connect[2] = Socks5Reserved;
            connect[3] = addressType;
            addressBytes.CopyTo(connect, 4);
            portBytes.CopyTo(connect, 4 + addressBytes.Length);

            return connect;
        }

        /// <summary>
        /// Converts the username to an array of bytes used to authenticate the user.
        /// </summary>
        /// <returns>An array of bytes representing the username.</returns>
        private byte[] GetUsernameBytes()
        {
            byte[] bytes = new byte[this.Username.Length + 1];
            bytes[0] = Convert.ToByte(this.Username.Length);
            byte[] usernameBytes = ASCIIEncoding.ASCII.GetBytes(this.Username);
            usernameBytes.CopyTo(bytes, 1);
            return bytes;
        }

        /// <summary>
        /// Converts the password to an array of bytes used to authenticate the user.
        /// </summary>
        /// <returns>An array of bytes representing the password.</returns>
        private byte[] GetPasswordBytes()
        {
            byte[] bytes = new byte[this.Password.Length + 1];
            bytes[0] = Convert.ToByte(this.Password.Length);
            byte[] passwordBytes = ASCIIEncoding.ASCII.GetBytes(this.Password);
            passwordBytes.CopyTo(bytes, 1);
            return bytes;
        }

        /// <summary>
        /// Creates an array of bytes representing the SOCKS5 proxy server authenticate command.
        ///// See http://www.ietf.org/rfc/rfc1928.txt
        /// </summary>
        /// <returns>An array of bytes representing the authenticate command for the specified username/password combination.</returns>
        private byte[] GetAuthenticateCommand()
        {
            byte[] usernameBytes = this.GetUsernameBytes();
            byte[] passwordBytes = this.GetPasswordBytes();

            byte[] auth = new byte[3 + this.Username.Length + this.Password.Length];
            auth[0] = 1;
            usernameBytes.CopyTo(auth, 1);
            passwordBytes.CopyTo(auth, (this.Username.Length + 2));
            return auth;
        }

        #endregion
    }
}
