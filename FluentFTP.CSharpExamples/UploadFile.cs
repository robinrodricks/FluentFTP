using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class UploadFileExample {

		public static void UploadFile() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				// upload a file to an existing FTP directory
				ftp.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md");

				// upload a file and ensure the FTP directory is created on the server
				ftp.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, true);

				// upload a file and ensure the FTP directory is created on the server, verify the file after upload
				ftp.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, true, FtpVerify.Retry);

			}
		}

		public static async Task UploadFileAsync() {
			var token = new CancellationToken();
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.ConnectAsync(token);

				// upload a file to an existing FTP directory
				await ftp.UploadFileAsync(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", token: token);

				// upload a file and ensure the FTP directory is created on the server
				await ftp.UploadFileAsync(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, true, token: token);

				// upload a file and ensure the FTP directory is created on the server, verify the file after upload
				await ftp.UploadFileAsync(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, true, FtpVerify.Retry, token: token);

			}
		}

	}
}