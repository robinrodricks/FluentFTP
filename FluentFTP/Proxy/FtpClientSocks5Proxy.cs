using System.Net;
using FluentFTP.Proxy.Socks;
#if ASYNC

using System.Threading;
using System.Threading.Tasks;

#endif


namespace FluentFTP.Proxy {
	/// <summary> A FTP client with a SOCKS5 proxy implementation. </summary>
	public class FtpClientSocks5Proxy : FtpClientProxy {
		public FtpClientSocks5Proxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS5 Proxy";
		}

		protected override void Connect(FtpSocketStream stream) {
			base.Connect(stream);
			var proxy = new SocksProxy(Host, Port, stream, this.Proxy);
			proxy.Negotiate();
			proxy.Authenticate();
			proxy.Connect();
		}

		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);
			var proxy = new SocksProxy(Host, port, stream, this.Proxy);
			proxy.Negotiate();
			proxy.Authenticate();
			proxy.Connect();
		}

#if ASYNC
		protected override async Task ConnectAsync(FtpSocketStream stream, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new SocksProxy(Host, Port, stream, this.Proxy);
			await proxy.NegotiateAsync();
			await proxy.AuthenticateAsync();
			await proxy.ConnectAsync();
		}
#endif
	}
}