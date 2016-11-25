namespace FluentFTP
{
    /// <summary>
    /// Abstraction of an FtpClient with a proxy
    /// </summary>
    public abstract class FtpClientProxy : FtpClient
    {
        /// <summary> The proxy informations </summary>
        protected Proxy Proxy { get; }

        /// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
        /// <param name="proxy">Proxy informations</param>
        protected FtpClientProxy(Proxy proxy)
        {
            Proxy = proxy;
        }

        /// <summary> Redefine connect for FtpClient : authentication on the Proxy  </summary>
        /// <param name="stream">The socket stream.</param>
        protected override void Connect(FtpSocketStream stream)
        {
            stream.Connect(Proxy.Host, Proxy.Port, InternetProtocolVersions);
        }
    }
}