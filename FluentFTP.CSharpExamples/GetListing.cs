using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetListingExample {

		public static void GetListing() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// get a recursive listing of the files & folders in a specific folder
				foreach (var item in conn.GetListing("/htdocs", FtpListOption.Recursive)) {
					switch (item.Type) {

						case FtpObjectType.Directory:

							Console.WriteLine("Directory!  " + item.FullName);
							Console.WriteLine("Modified date:  " + conn.GetModifiedTime(item.FullName));

							break;

						case FtpObjectType.File:

							Console.WriteLine("File!  " + item.FullName);
							Console.WriteLine("File size:  " + conn.GetFileSize(item.FullName));
							Console.WriteLine("Modified date:  " + conn.GetModifiedTime(item.FullName));
							Console.WriteLine("Chmod:  " + conn.GetChmod(item.FullName));

							break;

						case FtpObjectType.Link:
							break;
					}
				}

			}
		}

		public static async Task GetListingAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.Connect(token);

				// get a recursive listing of the files & folders in a specific folder
				foreach (var item in await conn.GetListing("/htdocs", FtpListOption.Recursive, token)) {

					switch (item.Type) {

						case FtpObjectType.Directory:

							Console.WriteLine("Directory!  " + item.FullName);
							Console.WriteLine("Modified date:  " + await conn.GetModifiedTime(item.FullName, token));

							break;

						case FtpObjectType.File:

							Console.WriteLine("File!  " + item.FullName);
							Console.WriteLine("File size:  " + await conn.GetFileSize(item.FullName, -1, token));
							Console.WriteLine("Modified date:  " + await conn.GetModifiedTime(item.FullName, token));
							Console.WriteLine("Chmod:  " + await conn.GetChmod(item.FullName, token));

							break;

						case FtpObjectType.Link:
							break;
					}
				}

			}
		}

	}
}