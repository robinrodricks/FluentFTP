using FluentFTP.Client.BaseClient;
using FluentFTP.Proxy.Socks;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.AsyncProxy {

	/// <summary>
	/// A FTP client with a SOCKS4 proxy implementation.
	/// </summary>
	public class AsyncFtpClientSocks4Proxy : AsyncFtpClientProxy {

		/// <summary>
		/// Setup a SOCKS4 proxy
		/// </summary>
		public AsyncFtpClientSocks4Proxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS4 Proxy";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClientSocks4Proxy(Proxy);
		}

		/// <summary>
		/// Called during <see cref="ConnectAsync(FtpSocketStream, CancellationToken)"/>. Typically extended by FTP proxies.
		/// </summary>
		protected override async Task HandshakeAsync(CancellationToken token = default) {
			await ((IInternalFtpClient)this).GetBaseStream().ReadAsync(new byte[6], 0, 6, token);
			await base.HandshakeAsync(token);
		}

		/// <inheritdoc />
		protected override async Task ConnectAsync(FtpSocketStream stream, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new Socks4Proxy(Host, Port, stream);
			await proxy.ConnectAsync(cancellationToken);
		}

		/// <inheritdoc />
		protected override async Task ConnectAsync(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new Socks4Proxy(Host, Port, stream);
			await proxy.ConnectAsync(cancellationToken);
		}
	}
}