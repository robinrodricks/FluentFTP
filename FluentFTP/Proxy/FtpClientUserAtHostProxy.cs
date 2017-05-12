namespace FluentFTP.Proxy {
	/// <summary> A FTP client with a user@host proxy identification. </summary>
	public class FtpClientUserAtHostProxy : FtpClientProxy {
		/// <summary> A FTP client with a user@host proxy identification. </summary>
		/// <param name="proxy">Proxy information</param>
		public FtpClientUserAtHostProxy(ProxyInfo proxy)
			: base(proxy) {
			ConnectionType = "User@Host";
		}

	    /// <summary>
	    /// Creates a new instance of this class. Useful in FTP proxy classes.
	    /// </summary>
		protected override FtpClient Create() {
			return new FtpClientUserAtHostProxy(Proxy);
		}

		/// <summary> Redefine the first dialog: auth with proxy information </summary>
		protected override void Handshake() {
			// Proxy authentication eventually needed.
			if (Proxy.Credentials != null)
				Authenticate(Proxy.Credentials.UserName, Proxy.Credentials.Password);

			// Connection USER@Host means to change user name to add host.
			Credentials.UserName = Credentials.UserName + "@" + Host;
		}
	}
}