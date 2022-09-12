using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class SetWorkingDirectoryExample {

		public static void SetWorkingDirectory() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.Connect();
				conn.SetWorkingDirectory("/full/or/relative/path");
			}
		}

		public static async Task SetWorkingDirectoryAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				await conn.ConnectAsync(token);
				await conn.SetWorkingDirectoryAsync("/full/or/relative/path", token);
			}
		}

	}
}