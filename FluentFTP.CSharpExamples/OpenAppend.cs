using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class OpenAppendExample {

		public static void OpenAppend() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// open an append-only stream to the file
				using (var ostream = conn.OpenAppend("/full/or/relative/path/to/file")) {
					try {
						// be sure to seek your output stream to the appropriate location, i.e., istream.Position
						// istream.Position is incremented accordingly to the writes you perform
						// istream.Position == file size if the server supports getting the file size
						// also note that file size for the same file can vary between ASCII and Binary
						// modes and some servers won't even give a file size for ASCII files! It is
						// recommended that you stick with Binary and worry about character encodings
						// on your end of the connection.
					}
					finally {
						ostream.Close();
					}
				}
			}
		}

		public static async Task OpenAppendAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// open an append-only stream to the file
				using (var ostream = await conn.OpenAppendAsync("/full/or/relative/path/to/file", token)) {
					try {
						// be sure to seek your output stream to the appropriate location, i.e., istream.Position
						// istream.Position is incremented accordingly to the writes you perform
						// istream.Position == file size if the server supports getting the file size
						// also note that file size for the same file can vary between ASCII and Binary
						// modes and some servers won't even give a file size for ASCII files! It is
						// recommended that you stick with Binary and worry about character encodings
						// on your end of the connection.
					}
					finally {
						ostream.Close();
					}
				}
			}
		}

	}
}