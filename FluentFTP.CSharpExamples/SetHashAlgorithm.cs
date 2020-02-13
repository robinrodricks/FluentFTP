using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class SetHashAlgorithmExample {

		public static void SetHashAlgorithm() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				if (conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
					conn.SetHashAlgorithm(FtpHashAlgorithm.MD5);
				}
			}
		}

		public static async Task SetHashAlgorithmAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				if (conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
					await conn.SetHashAlgorithmAsync(FtpHashAlgorithm.MD5);
				}
			}
		}

	}
}