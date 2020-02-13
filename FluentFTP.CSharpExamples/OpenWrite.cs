using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class OpenWriteExample {

		public static void OpenWrite() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// open an write-only stream to the file
				using (var ostream = conn.OpenWrite("/full/or/relative/path/to/file")) {
					try {
						// ostream.Position is incremented accordingly to the writes you perform
					}
					finally {
						ostream.Close();
					}
				}
			}
		}

		public static async Task OpenWriteAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// open an write-only stream to the file
				using (var ostream = await conn.OpenWriteAsync("/full/or/relative/path/to/file", token)) {
					try {
						// ostream.Position is incremented accordingly to the writes you perform
					}
					finally {
						ostream.Close();
					}
				}
			}
		}

	}
}