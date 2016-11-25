using System;
using System.IO;

namespace FluentFTP.Proxy
{
    /// <summary> A FTP client with a HTTP 1.1 proxy implementation. </summary>
    public class FtpClientHttp11Proxy : FtpClientProxy
    {
        /// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
        /// <param name="proxy">Proxy informations</param>
		public FtpClientHttp11Proxy(ProxyInfo proxy)
			: base(proxy)
        {
            ConnectionType = "HTTP 1.1 Proxy";
        }

        /// <summary> Redefine the first dialog: HTTP Trame for the HTTP 1.1 Proxy </summary>
        protected override void Handshake()
        {
            var writer = new StreamWriter(BaseStream);
            writer.Write("CONNECT {0}:{1} HTTP/1.1", Host, Port);
            writer.Write("Host: {0}:{1}", Host, Port);
            if (Proxy.Credentials != null)
            {
                var credentialsHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Proxy.Credentials.UserName, Proxy.Credentials.Password)));
                writer.WriteLine("Proxy-Authorization: Basic {0}", credentialsHash);
            }
            writer.WriteLine("User-Agent: custom-ftp-client");
            writer.WriteLine();
            writer.Flush();

            var proxyConnectionReply = GetReply();
            if (!proxyConnectionReply.Success)
                throw new FtpException("Can't connect " + Host +" via proxy " + Proxy.Host + ". Message : "+proxyConnectionReply);
        }
    }
}