using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetNameListingExample {

		public static void GetNameListing() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				foreach (var s in conn.GetNameListing()) {
					// load some information about the object
					// returned from the listing...
					var isDirectory = conn.DirectoryExists(s);
					var modify = conn.GetModifiedTime(s);
					var size = isDirectory ? 0 : conn.GetFileSize(s);
				}
			}
		}

		public static async Task GetNameListingAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				foreach (var s in await conn.GetNameListingAsync(token)) {
					// load some information about the object
					// returned from the listing...
					var isDirectory = await conn.DirectoryExistsAsync(s, token);
					var modify = await conn.GetModifiedTimeAsync(s, token);
					var size = isDirectory ? 0 : await conn.GetFileSizeAsync(s, -1, token);
				}
			}
		}

	}
}