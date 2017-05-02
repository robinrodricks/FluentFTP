using System.Net;

namespace FluentFTP.Proxy {
	/// <summary> POCO holding proxy information</summary>
	public class ProxyInfo {
		/// <summary> Proxy host name </summary>
		public string Host { get; set; }

		/// <summary> Proxy port </summary>
		public int Port { get; set; }

		/// <summary> Proxy login credentials </summary>
		public NetworkCredential Credentials { get; set; }
	}
}