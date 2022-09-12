using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Rules;

namespace Examples {

	internal static class UploadDirectoryExample {

		public static void UploadDirectory() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();


				// upload a folder and all its files
				ftp.UploadDirectory(@"C:\website\videos\", @"/public_html/videos", FtpFolderSyncMode.Update);
				
				// upload a folder and all its files, and delete extra files on the server
				ftp.UploadDirectory(@"C:\website\assets\", @"/public_html/assets", FtpFolderSyncMode.Mirror);
				
			}
		}

		public static async Task UploadDirectoryAsync() {
			var token = new CancellationToken();
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.ConnectAsync(token);


				// upload a folder and all its files
				await ftp.UploadDirectoryAsync(@"C:\website\videos\", @"/public_html/videos", FtpFolderSyncMode.Update, token: token);
				
				// upload a folder and all its files, and delete extra files on the server
				await ftp.UploadDirectoryAsync(@"C:\website\assets\", @"/public_html/assets", FtpFolderSyncMode.Mirror, token: token);
				
			}
		}

	}
}