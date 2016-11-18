namespace FluentFTP
{
    /// <summary> A FTP client with a user@host proxy identification. </summary>
    public class FtpClientUserAtHostProxy : FtpClientProxy
    {
        /// <summary> A FTP client with a user@host proxy identification. </summary>
        /// <param name="proxy">Proxy informations</param>
        public FtpClientUserAtHostProxy(Proxy proxy) 
            : base(proxy)
        {
        }

        /// <summary> Redefine the first dialog: auth with proxy information </summary>
        protected override void Handshake()
        {
            // Proxy authentication eventually needed.
            if (Proxy.Credential != null)
                Authenticate(Proxy.Credential.UserName, Proxy.Credential.Password);

            // Connection USER@Host meens to change user name to add host.
            Credentials.UserName = $"{Credentials.UserName}@{Host}";
        }
    }
}