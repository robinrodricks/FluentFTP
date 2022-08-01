namespace FluentFTP.Proxy {
	/// <summary> 
	/// A FTP client with a user@host proxy identification, that works with Blue Coat FTP Service servers.
	/// 
	/// The 'blue coat variant' forces the client to wait for a 220 FTP response code in 
	/// the handshake phase.
	/// </summary>
	public class FtpClientBlueCoatProxy : FtpClientProxy {
		/// <summary> A FTP client with a user@host proxy identification. </summary>
		/// <param name="proxy">Proxy information</param>
		public FtpClientBlueCoatProxy(FtpProxyProfile proxy)
			: base(proxy) {
			ConnectionType = "Blue Coat";
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override FtpClient Create() {
			return new FtpClientBlueCoatProxy(Proxy);
		}

		/// <summary> Redefine the first dialog: auth with proxy information </summary>
		protected override void Handshake() {
			// Proxy authentication eventually needed.
			if (Proxy.ProxyCredentials != null) {
				Authenticate(Proxy.ProxyCredentials.UserName, Proxy.ProxyCredentials.Password, Proxy.ProxyCredentials.Domain);
			}

			// Connection USER@Host means to change user name to add host.
			Credentials.UserName = Credentials.UserName + "@" + Host + ":" + Port;

			var reply = GetReply();
			if (reply.Code == "220") {
				LogLine(FtpTraceLevel.Info, "Status: Server is ready for the new client");
			}

			// TO TEST: if we are able to detect the actual FTP server software from this reply
			HandshakeReply = reply;
		}
	}
}