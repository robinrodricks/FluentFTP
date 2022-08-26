using FluentFTP.Client.BaseClient;
using FluentFTP.Proxy.Socks;
using System.Threading;
using System.Threading.Tasks;

#if ASYNC
namespace FluentFTP.Proxy.AsyncProxy {
	/// <summary> A FTP client with a SOCKS5 proxy implementation. </summary>
	public class AsyncFtpClientSocks5Proxy : AsyncFtpClientProxy {
		public AsyncFtpClientSocks5Proxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS5 Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClientSocks5Proxy(Proxy);
		}


		protected override async Task ConnectAsync(FtpSocketStream stream, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new SocksProxy(Host, Port, stream, Proxy);
			await proxy.NegotiateAsync();
			await proxy.AuthenticateAsync();
			await proxy.ConnectAsync();
		}
	}
}
#endif