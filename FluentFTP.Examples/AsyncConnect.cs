using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class AsyncConnectExample {
		public static async Task AsyncConnectAsync() {
			var cts = new CancellationTokenSource(10000);
			using (var conn = new FtpClient()) {
				conn.Host = "127.0.0.1";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				await conn.ConnectAsync(cts.Token);
				await conn.UploadAsync(new byte[1000 * 1000], "test/file", createRemoteDir: true, token: cts.Token);
			}
		}
	}
}