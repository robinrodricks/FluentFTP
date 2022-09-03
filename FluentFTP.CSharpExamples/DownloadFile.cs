using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class DownloadFileExample {

		public static void DownloadFile() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				// download a file and ensure the local directory is created
				ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md");

				// download a file and ensure the local directory is created, verify the file after download
				ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.Retry);

			}
		}

		public static async Task DownloadFileAsync() {
			var token = new CancellationToken();
			using (var ftp = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.Connect(token);

				// download a file and ensure the local directory is created
				await ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", token: token);

				// download a file and ensure the local directory is created, verify the file after download
				await ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.Retry, token: token);

			}
		}

	}
}