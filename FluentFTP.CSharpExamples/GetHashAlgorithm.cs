using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetHashAlgorithmExample {

		public static void GetHashAlgorithm() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				Console.WriteLine("The server is using the following algorithm for computing hashes: " +
							  conn.GetHashAlgorithm());
			}
		}

		public static async Task GetHashAlgorithmAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				Console.WriteLine("The server is using the following algorithm for computing hashes: " +
							  await conn.GetHashAlgorithmAsync());
			}
		}

	}
}