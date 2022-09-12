using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class CreateDirectoryExample {

		public static void CreateDirectory() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				conn.CreateDirectory("/test/path/that/should/be/created", true);
			}
		}
		
		public static async Task CreateDirectoryAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				await conn.CreateDirectoryAsync("/test/path/that/should/be/created", true, token);
			}
		}
	}
}