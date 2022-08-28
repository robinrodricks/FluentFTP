using FluentFTP.Client.BaseClient;

namespace FluentFTP.Proxy.SyncProxy {
	/// <summary> A FTP client with a user@host proxy identification. </summary>
	public class FtpClientUserAtHostProxy : FtpClientProxy {
		/// <summary> A FTP client with a user@host proxy identification. </summary>
		/// <param name="proxy">Proxy information</param>
		public FtpClientUserAtHostProxy(FtpProxyProfile proxy)
			: base(proxy) {
			ConnectionType = "User@Host";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new FtpClientUserAtHostProxy(Proxy);
		}

		/// <summary> Redefine the first dialog: auth with proxy information </summary>
		protected override void Handshake() {
			// Proxy authentication eventually needed.
			if (Proxy.ProxyCredentials != null) {
				Authenticate(Proxy.ProxyCredentials.UserName, Proxy.ProxyCredentials.Password, Proxy.ProxyCredentials.Domain);
			}

			// Connection USER@Host means to change user name to add host.
			Credentials.UserName = Credentials.UserName + "@" + Host + ":" + Port;
		}
	}
}