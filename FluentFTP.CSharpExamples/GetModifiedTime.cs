using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetModifiedTimeExample {

		public static void GetModifiedTime() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				Console.WriteLine("The modified type is: " +
							  conn.GetModifiedTime("/full/or/relative/path/to/file"));
			}
		}

		public static async Task GetModifiedTimeAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				Console.WriteLine("The modified type is: " +
							  await conn.GetModifiedTimeAsync("/full/or/relative/path/to/file", token));
			}
		}

	}
}