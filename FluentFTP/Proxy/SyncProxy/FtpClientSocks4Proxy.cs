
/* Unmerged change from project 'FluentFTP (net472)'
Before:
using FluentFTP.Proxy.Socks;
After:
using FluentFTP;
using FluentFTP.Proxy;
using FluentFTP.Proxy.Socks;
*/

/* Unmerged change from project 'FluentFTP (net462)'
Before:
using FluentFTP.Proxy.Socks;
After:
using FluentFTP;
using FluentFTP.Proxy;
using FluentFTP.Proxy.Socks;
*/
using FluentFTP.Client.BaseClient;
using FluentFTP.Proxy.Socks;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.SyncProxy {

	/// <summary>
	/// A FTP client with a SOCKS4 proxy implementation.
	/// </summary>
	public class FtpClientSocks4Proxy : FtpClientProxy {

		/// <summary>
		/// Setup a SOCKS4 proxy
		/// </summary>
		public FtpClientSocks4Proxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS4 Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClientSocks4Proxy(Proxy);
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
		protected override void Connect(FtpSocketStream stream) {
			base.Connect(stream);
			var proxy = new Socks4Proxy(Host, Port, stream);
			proxy.Connect();
		}

		/// <summary>
		/// Connect
		/// </summary>
		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);
			var proxy = new Socks4Proxy(Host, port, stream);
			proxy.Connect();
		}

	}
}