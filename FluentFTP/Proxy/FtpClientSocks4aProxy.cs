using FluentFTP.Proxy.Socks;

#if ASYNC

using System.Threading;
using System.Threading.Tasks;

#endif

namespace FluentFTP.Proxy {
	/// <summary> A FTP client with a SOCKS4a proxy implementation. </summary>
	public class FtpClientSocks4aProxy : FtpClientProxy {
		public FtpClientSocks4aProxy(FtpProxyProfile proxy) : base(proxy) {
			ConnectionType = "SOCKS4a Proxy";
		}

		/// <summary>
		/// Called during Connect(). Typically extended by FTP proxies.
		/// </summary>
		protected override void Handshake() {
			BaseStream.Read(new byte[6], 0, 6);
			base.Handshake();
		}

#if ASYNC
		/// <summary>
		/// Called during <see cref="ConnectAsync()"/>. Typically extended by FTP proxies.
		/// </summary>
		protected virtual async Task HandshakeAsync(CancellationToken token = default(CancellationToken)) {
			await BaseStream.ReadAsync(new byte[6], 0, 6);
			base.Handshake();
		}
#endif

		protected override void Connect(FtpSocketStream stream) {
			base.Connect(stream);
			var proxy = new Socks4aProxy(Host, Port, stream);
			proxy.Connect();
		}

		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);
			var proxy = new Socks4aProxy(Host, port, stream);
			proxy.Connect();
		}

#if ASYNC
		protected override async Task ConnectAsync(FtpSocketStream stream, CancellationToken cancellationToken) {
			await base.ConnectAsync(stream, cancellationToken);
			var proxy = new Socks4aProxy(Host, Port, stream);
			await proxy.ConnectAsync();
		}
#endif
	}
}