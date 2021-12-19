using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class DirectoryExistsExample {

		public static void DirectoryExists() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				if (conn.DirectoryExists("/full/or/relative/path")) {
					// do something
				}
			}
		}

		public static async Task DirectoryExistsAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				if (await conn.DirectoryExistsAsync("/full/or/relative/path", token)) {
					// do something
				}
			}
		}
	}
}