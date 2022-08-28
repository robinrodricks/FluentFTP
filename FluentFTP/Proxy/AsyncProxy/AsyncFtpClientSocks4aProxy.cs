using FluentFTP.Proxy.Socks;
using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

#if ASYNC
namespace FluentFTP.Proxy.AsyncProxy {
	/// <summary> A FTP client with a SOCKS4a proxy implementation. </summary>
	public class AsyncFtpClientSocks4aProxy : AsyncFtpClientProxy {
		public AsyncFtpClientSocks4aProxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS4a Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClientSocks4aProxy(Proxy);
		}

		/// <summary>
		/// Called during <see cref="ConnectAsync()"/>. Typically extended by FTP proxies.
		/// </summary>
		protected virtual async Task HandshakeAsync(CancellationToken token = default) {
			await ((IInternalFtpClient)this).GetBaseStream().ReadAsync(new byte[6], 0, 6);
			await base.HandshakeAsync();
		}

		protected override async Task ConnectAsync(FtpSocketStream stream, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new Socks4aProxy(Host, Port, stream);
			await proxy.ConnectAsync();
		}
	}
}
#endif