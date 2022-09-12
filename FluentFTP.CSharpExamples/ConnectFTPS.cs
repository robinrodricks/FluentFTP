using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ConnectFTPSExample {

		public static void ConnectFTPS() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.Config.ValidateAnyCertificate = true;
				conn.Connect();
			}
		}

		public static async Task ConnectFTPSAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
				conn.Config.ValidateAnyCertificate = true;
				await conn.Connect(token);
			}
		}


	}
}