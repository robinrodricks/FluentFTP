using System;
using System.Net;
using System.Net.FtpClient;
using System.Security.Authentication;

namespace Examples
{
    public static class SetEncryptionProtocolsExample {
        public static void ValidateCertificate()
        {
            using (FtpClient conn = new FtpClient())
            {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.EncryptionMode = FtpEncryptionMode.Explicit;

                // Override the default protocols. The example line sets the protocols
                // to support TLS 1.1 and TLS 1.2. These are only available if the 
                // executable targets .NET 4.5 (or higher). The framework does not enable these
                // protocols by default.
                //conn.SslProtocols = SslProtocols.Default | SslProtocols.Tls11 | SslProtocols.Tls12;

                // Alternate example: Only support the latest level.
                //conn.SslProtocols = SslProtocols.Tls12;
                
                conn.Connect();
            }
        }
    }
}
