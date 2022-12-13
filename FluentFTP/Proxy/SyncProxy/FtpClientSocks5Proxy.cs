using FluentFTP.Client.BaseClient;
using FluentFTP.Proxy.Socks;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.SyncProxy {

	/// <summary>
	/// A FTP client with a SOCKS5 proxy implementation.
	/// </summary>
	public class FtpClientSocks5Proxy : FtpClientProxy {

		/// <summary>
		/// Setup a SOCKS5 proxy
		/// </summary>
		public FtpClientSocks5Proxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS5 Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClientSocks5Proxy(Proxy);
		}

		/// <summary>
		/// Connect
		/// </summary>
		protected override void Connect(FtpSocketStream stream) {
			base.Connect(stream);
			var proxy = new SocksProxy(Host, Port, stream, Proxy);
			proxy.Negotiate();
			proxy.Authenticate();
			proxy.Connect();
		}

		/// <summary>
		/// Connect
		/// </summary>
		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);
			var proxy = new SocksProxy(Host, port, stream, Proxy);
			proxy.Negotiate();
			proxy.Authenticate();
			proxy.Connect();
		}

	}
}