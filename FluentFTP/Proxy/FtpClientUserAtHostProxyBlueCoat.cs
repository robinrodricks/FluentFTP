namespace FluentFTP.Proxy {
    /// <summary> 
    /// A FTP client with a user@host proxy identification, that works with Blue Coat FTP Service servers.
	/// 
    /// The 'blue coat variant' forces the client to wait for a 220 FTP response code in 
    /// the handshake phase.
    /// </summary>
    public class FtpClientUserAtHostProxyBlueCoat : FtpClientProxy
    {
        /// <summary> A FTP client with a user@host proxy identification. </summary>
        /// <param name="proxy">Proxy information</param>
        public FtpClientUserAtHostProxyBlueCoat(ProxyInfo proxy)
            : base(proxy)
        {
            ConnectionType = "User@Host";
        }

        /// <summary>
        /// Creates a new instance of this class. Useful in FTP proxy classes.
        /// </summary>
        protected override FtpClient Create()
        {
            return new FtpClientUserAtHostProxyBlueCoat(Proxy);
        }

        /// <summary> Redefine the first dialog: auth with proxy information </summary>
        protected override void Handshake()
        {
            // Proxy authentication eventually needed.
            if (Proxy.Credentials != null)
                Authenticate(Proxy.Credentials.UserName, Proxy.Credentials.Password);

            // Connection USER@Host means to change user name to add host.
            Credentials.UserName = Credentials.UserName + "@" + Host;

            FtpReply reply = GetReply();
            if (reply.Code == "220")
                this.LogLine(FtpTraceLevel.Info, "Status: Server is ready for the new client");

			// TO TEST: if we are able to detect the actual FTP server software from this reply
			HandshakeReply = reply;
		}
    }

}