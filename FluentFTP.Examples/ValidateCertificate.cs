using System;
using System.Net;
using FluentFTP;

namespace Examples {
	public static class ValidateCertificateExample {
		public static void ValidateCertificate() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
				conn.Connect();
			}
		}

		private static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
			if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
				// invalid cert, do you want to accept it?
				// e.Accept = true;
			}
		}
	}
}