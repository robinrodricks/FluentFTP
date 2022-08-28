using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.SyncProxy {
	/// <summary>
	/// Abstraction of an FtpClient with a proxy
	/// </summary>
	public abstract class FtpClientProxy : FtpClient {
		private FtpProxyProfile _proxy;

		/// <summary> The proxy connection info. </summary>
		protected FtpProxyProfile Proxy => _proxy;

		/// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
		/// <param name="proxy">Proxy information</param>
		protected FtpClientProxy(FtpProxyProfile proxy) {
			_proxy = proxy;

			// set the FTP server details into the client if provided
			if (_proxy.FtpHost != null) {
				Host = _proxy.FtpHost;
				Port = _proxy.FtpPort;
				Credentials = _proxy.FtpCredentials;
			}
		}

		/// <summary> Redefine connect for FtpClient : authentication on the Proxy  </summary>
		/// <param name="stream">The socket stream.</param>
		protected override void Connect(FtpSocketStream stream) {
			stream.Connect(Proxy.ProxyHost, Proxy.ProxyPort, Config.InternetProtocolVersions);
		}

	}
}