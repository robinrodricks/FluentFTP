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
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.Connect(token);

				foreach (var s in await conn.GetNameListing(token)) {
					// load some information about the object
					// returned from the listing...
					var isDirectory = await conn.DirectoryExists(s, token);
					var modify = await conn.GetModifiedTime(s, token);
					var size = isDirectory ? 0 : await conn.GetFileSize(s, -1, token);
				}
			}
		}

	}
}