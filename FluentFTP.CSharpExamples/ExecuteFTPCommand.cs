using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ExecuteFTPCommandExample {

		public static void Execute() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				FtpReply reply;
				if (!(reply = conn.Execute("SITE CHMOD 640 FOO.TXT")).Success) {
					throw new FtpCommandException(reply);
				}
			}
		}

		public static async Task ExecuteAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				FtpReply reply;
				if (!(reply = await conn.ExecuteAsync("SITE CHMOD 640 FOO.TXT", token)).Success) {
					throw new FtpCommandException(reply);
				}
			}
		}


	}
}