using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class OpenReadExample {

		public static void OpenRead() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// open an read-only stream to the file
				using (var istream = conn.OpenRead("/full/or/relative/path/to/file")) {
					try {
						// istream.Position is incremented accordingly to the reads you perform
						// istream.Length == file size if the server supports getting the file size
						// also note that file size for the same file can vary between ASCII and Binary
						// modes and some servers won't even give a file size for ASCII files! It is
						// recommended that you stick with Binary and worry about character encodings
						// on your end of the connection.
					}
					finally {
						Console.WriteLine();
						istream.Close();
					}
				}
			}
		}

		public static async Task OpenReadAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// open an read-only stream to the file
				using (var istream = await conn.OpenReadAsync("/full/or/relative/path/to/file", token)) {
					try {
						// istream.Position is incremented accordingly to the reads you perform
						// istream.Length == file size if the server supports getting the file size
						// also note that file size for the same file can vary between ASCII and Binary
						// modes and some servers won't even give a file size for ASCII files! It is
						// recommended that you stick with Binary and worry about character encodings
						// on your end of the connection.
					}
					finally {
						Console.WriteLine();
						istream.Close();
					}
				}
			}
		}

	}
}