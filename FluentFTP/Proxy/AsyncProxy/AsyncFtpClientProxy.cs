using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.AsyncProxy {
	/// <summary>
	/// Abstraction of an FtpClient with a proxy
	/// </summary>
	public abstract class AsyncFtpClientProxy : AsyncFtpClient {
		private FtpProxyProfile _proxy;

		/// <summary> The proxy connection info. </summary>
		protected FtpProxyProfile Proxy => _proxy;

		/// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
		/// <param name="proxy">Proxy information</param>
		protected AsyncFtpClientProxy(FtpProxyProfile proxy) {
			_proxy = proxy;

			// set the FTP server details into the client if provided
			if (_proxy.FtpHost != null) {
				this.Host = _proxy.FtpHost;
				this.Port = _proxy.FtpPort;
				this.Credentials = _proxy.FtpCredentials;
			}
		}

		/// <summary> Redefine connect for FtpClient : authentication on the Proxy  </summary>
		/// <inheritdoc />
		protected override Task ConnectAsync(FtpSocketStream stream, CancellationToken token) {
			return stream.ConnectAsync(Proxy.ProxyHost, Proxy.ProxyPort, Config.InternetProtocolVersions, token);
		}

		/// <summary> Redefine connect for FtpClient : authentication on the Proxy  </summary>
		/// <inheritdoc />
		protected override Task ConnectAsync(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions, CancellationToken token) {
			return stream.ConnectAsync(Proxy.ProxyHost, Proxy.ProxyPort, Config.InternetProtocolVersions, token);
		}
	}
}