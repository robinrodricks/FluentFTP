using FluentFTP.Proxy.Socks;
using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.SyncProxy {

	/// <summary>
	/// A FTP client with a SOCKS4a proxy implementation.
	/// </summary>
	public class FtpClientSocks4aProxy : FtpClientProxy {

		/// <summary>
		/// Setup a SOCKS4a proxy
		/// </summary>
		public FtpClientSocks4aProxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS4a Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClientSocks4aProxy(Proxy);
		}

		/// <summary>
		/// Called during Connect(). Typically extended by FTP proxies.
		/// </summary>
		protected override void Handshake() {
			((IInternalFtpClient)this).GetBaseStream().Read(new byte[6], 0, 6);
			base.Handshake();
		}

		/// <summary>
		/// Connect
		/// </summary>
		/// <param name="stream"></param>
		protected override void Connect(FtpSocketStream stream) {
			base.Connect(stream);
			var proxy = new Socks4aProxy(Host, Port, stream);
			proxy.Connect();
		}

		/// <summary>
		/// Connect
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="ipVersions"></param>
		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);
			var proxy = new Socks4aProxy(Host, port, stream);
			proxy.Connect();
		}

	}
}