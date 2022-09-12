using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class DeleteFileExample {

		public static void DeleteFile() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				conn.DeleteFile("/full/or/relative/path/to/file");
			}
		}

		public static async Task DeleteDirectoryAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				await conn.DeleteFileAsync("/full/or/relative/path/to/file", token);
			}
		}

	}
}