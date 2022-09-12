using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class DownloadFilesExample {

		public static void DownloadFiles() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				// download many files, skip if they already exist on disk
				ftp.DownloadFiles(@"D:\Drivers\test\",
					new[] {
						@"/public_html/temp/file0.exe",
						@"/public_html/temp/file1.exe",
						@"/public_html/temp/file2.exe",
						@"/public_html/temp/file3.exe",
						@"/public_html/temp/file4.exe"
					}, FtpLocalExists.Skip);

			}
		}

		public static async Task DownloadFilesAsync() {
			var token = new CancellationToken();
			using (var ftp = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.Connect(token);

				// download many files, skip if they already exist on disk
				await ftp.DownloadFiles(@"D:\Drivers\test\",
					new[] {
						@"/public_html/temp/file0.exe",
						@"/public_html/temp/file1.exe",
						@"/public_html/temp/file2.exe",
						@"/public_html/temp/file3.exe",
						@"/public_html/temp/file4.exe"
					}, FtpLocalExists.Skip, token: token);

			}
		}

	}
}