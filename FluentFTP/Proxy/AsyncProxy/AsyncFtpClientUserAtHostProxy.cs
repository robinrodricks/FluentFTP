using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.AsyncProxy {
	/// <summary> A FTP client with a user@host proxy identification. </summary>
	public class AsyncFtpClientUserAtHostProxy : AsyncFtpClientProxy {
		/// <summary> A FTP client with a user@host proxy identification. </summary>
		/// <param name="proxy">Proxy information</param>
		public AsyncFtpClientUserAtHostProxy(FtpProxyProfile proxy)
			: base(proxy) {
			ConnectionType = "User@Host";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClientUserAtHostProxy(Proxy);
		}

		/// <summary> Redefine the first dialog: auth with proxy information </summary>
		protected override async Task HandshakeAsync(CancellationToken token = default) {

			// Proxy authentication eventually needed.
			if (Proxy.ProxyCredentials != null) {
				await Authenticate(Proxy.ProxyCredentials.UserName, Proxy.ProxyCredentials.Password, Proxy.ProxyCredentials.Domain, token);
			}

			// Connection USER@Host means to change user name to add host.
			Credentials.UserName = Credentials.UserName + "@" + Host + ":" + Port;
		}
	}
}