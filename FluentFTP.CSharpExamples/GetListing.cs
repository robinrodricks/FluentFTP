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

						case FtpFileSystemObjectType.Directory:

							Console.WriteLine("Directory!  " + item.FullName);
							Console.WriteLine("Modified date:  " + conn.GetModifiedTime(item.FullName));

							break;

						case FtpFileSystemObjectType.File:

							Console.WriteLine("File!  " + item.FullName);
							Console.WriteLine("File size:  " + conn.GetFileSize(item.FullName));
							Console.WriteLine("Modified date:  " + conn.GetModifiedTime(item.FullName));
							Console.WriteLine("Chmod:  " + conn.GetChmod(item.FullName));

							break;

						case FtpFileSystemObjectType.Link:
							break;
					}
				}

			}
		}

		public static async Task GetListingAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// get a recursive listing of the files & folders in a specific folder
				foreach (var item in await conn.GetListingAsync("/htdocs", FtpListOption.Recursive, token)) {
					switch (item.Type) {

						case FtpFileSystemObjectType.Directory:

							Console.WriteLine("Directory!  " + item.FullName);
							Console.WriteLine("Modified date:  " + await conn.GetModifiedTimeAsync(item.FullName, FtpDate.Original, token));

							break;

						case FtpFileSystemObjectType.File:

							Console.WriteLine("File!  " + item.FullName);
							Console.WriteLine("File size:  " + await conn.GetFileSizeAsync(item.FullName, token));
							Console.WriteLine("Modified date:  " + await conn.GetModifiedTimeAsync(item.FullName, FtpDate.Original, token));
							Console.WriteLine("Chmod:  " + await conn.GetChmodAsync(item.FullName, token));

							break;

						case FtpFileSystemObjectType.Link:
							break;
					}
				}

			}
		}

	}
}