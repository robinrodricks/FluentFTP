using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetWorkingDirectoryExample {

		public static void GetWorkingDirectory() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				Console.WriteLine("The working directory is: " + conn.GetWorkingDirectory());
			}
		}

		public static async Task GetWorkingDirectoryAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.Connect(token);

				Console.WriteLine("The working directory is: " + await conn.GetWorkingDirectory(token));
			}
		}

	}
}