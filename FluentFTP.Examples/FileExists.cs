using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class FileExistsExample {

		public static void FileExists() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// The last parameter forces FluentFTP to use LIST -a 
				// for getting a list of objects in the parent directory.
				if (conn.FileExists("/full/or/relative/path")) {
					// dome something
				}
			}
		}

		public static async Task FileExistsAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// The last parameter forces FluentFTP to use LIST -a 
				// for getting a list of objects in the parent directory.
				if (await conn.FileExistsAsync("/full/or/relative/path", token)) {
					// dome something
				}
			}
		}

	}
}