using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class RenameExample {

		public static void Rename() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// renaming a directory is dependent on the server! if you attempt it
				// and it fails it's not because FluentFTP has a bug!
				conn.Rename("/full/or/relative/path/to/src", "/full/or/relative/path/to/dest");
			}
		}

		public static async Task RenameAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// renaming a directory is dependent on the server! if you attempt it
				// and it fails it's not because FluentFTP has a bug!
				await conn.RenameAsync("/full/or/relative/path/to/src", "/full/or/relative/path/to/dest", token);
			}
		}

	}
}