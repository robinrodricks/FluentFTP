using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ConnectFTPSCertificateExample {

		public static void ConnectFTPSCertificate() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
				conn.Connect();
			}
		}

		public static async Task ConnectFTPSCertificateAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
				await conn.ConnectAsync(token);
			}
		}

		private static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
			if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
				// invalid cert, do you want to accept it?
				// e.Accept = true;
			}
			else {
				e.Accept = true;
			}
		}

	}
}