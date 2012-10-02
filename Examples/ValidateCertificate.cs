using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public static class ValidateCertificateExample {
        public static void ValidateCertificate() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.EncryptionMode = FtpEncryptionMode.Explicit;
                conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
                conn.Connect();
            }
        }

        static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
            if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
                // invalid cert, do you want to accept it?
                // e.Accept = true;
            }
        }
    }
}
