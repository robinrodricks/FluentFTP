using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class UploadFilesExample {

		public static void UploadFiles() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				// upload many files, skip if they already exist on server
				ftp.UploadFiles(
					new[] {
						@"D:\Drivers\test\file0.exe",
						@"D:\Drivers\test\file1.exe",
						@"D:\Drivers\test\file2.exe",
						@"D:\Drivers\test\file3.exe",
						@"D:\Drivers\test\file4.exe"
					},
					"/public_html/temp/", FtpRemoteExists.Skip);

			}
		}

		public static async Task UploadFilesAsync() {
			var token = new CancellationToken();
			using (var ftp = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.Connect(token);

				// upload many files, skip if they already exist on server
				await ftp.UploadFiles(
					new[] {
						@"D:\Drivers\test\file0.exe",
						@"D:\Drivers\test\file1.exe",
						@"D:\Drivers\test\file2.exe",
						@"D:\Drivers\test\file3.exe",
						@"D:\Drivers\test\file4.exe"
					},
					"/public_html/temp/", FtpRemoteExists.Skip, token: token);

			}
		}

	}
}