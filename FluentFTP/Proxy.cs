using System.Net;

namespace FluentFTP
{
    /// <summary> POCO holding proxy informations </summary>
    public class Proxy
    {
        /// <summary> Proxy host name </summary>
        public string Host { get; set; }

        /// <summary> Proxy port </summary>
        public int Port { get; set; }

        /// <summary> Proxy credentials infomrations </summary>
        public NetworkCredential Credential { get; set; }
    }
}