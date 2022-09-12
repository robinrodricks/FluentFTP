using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ConnectExample {

		public static void Connect() {

			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				conn.Connect();
			}
		}

		public static void ConnectAlt() {

			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				conn.Connect();
			}
		}

		public static async Task ConnectAsync() {
			var token = new CancellationToken();

			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				await conn.ConnectAsync(token);
			}
		}

		public static async Task ConnectAsyncAlt() {
			var token = new CancellationToken();

			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {

				await conn.ConnectAsync(token);
			}
		}

	}
}