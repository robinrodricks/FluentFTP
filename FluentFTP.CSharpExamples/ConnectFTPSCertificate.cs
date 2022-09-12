using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Client.BaseClient;

namespace Examples {
	internal static class ConnectFTPSCertificateExample {

		public static void ConnectFTPSCertificate() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
				conn.Connect();
			}
		}

		public static async Task ConnectFTPSCertificateAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
				await conn.Connect(token);
			}
		}

		private static void OnValidateCertificate(BaseFtpClient control, FtpSslValidationEventArgs e) {
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