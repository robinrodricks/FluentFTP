using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ConnectFTPSExample {

		public static void ConnectFTPS() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateAnyCertificate = true;
				conn.Connect();
			}
		}

		public static async Task ConnectFTPSAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.ValidateAnyCertificate = true;
				await conn.ConnectAsync(token);
			}
		}


	}
}