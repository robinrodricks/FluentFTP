using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetFileSizeExample {

		public static void GetFileSize() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				Console.WriteLine("The file size is: " + conn.GetFileSize("/full/or/relative/path/to/file"));
			}
		}

		public static async Task GetFileSizeAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				Console.WriteLine("The file size is: " + await conn.GetFileSizeAsync("/full/or/relative/path/to/file", -1, token));
			}
		}


	}
}