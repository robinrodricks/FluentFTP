
namespace System.Net.FtpClient.Proxy
{
    /// <summary>
    /// Specifies the type of proxy servers used by an instance of the ProxySocket class.
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// No proxy server. ProxySocket will behave like a normal Socket.
        /// </summary>
        None,

        /// <summary>
        /// A SOCKS4 proxy server.
        /// </summary>
        Socks4,

        /// <summary>
        /// A SOCKS4A proxy server.
        /// </summary>
        Socks4a,

        /// <summary>
        /// A SOCKS5 proxy server.
        /// </summary>
        Socks5
    }
}
