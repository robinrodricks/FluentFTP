using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Rules;

namespace Examples {

	internal static class DownloadDirectoryExample {

		public static void DownloadDirectory() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();
				

				// download a folder and all its files
				ftp.DownloadDirectory(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update);
				
				// download a folder and all its files, and delete extra files on disk
				ftp.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);
				
			}
		}

		public static async Task DownloadDirectoryAsync() {
			var token = new CancellationToken();
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.ConnectAsync(token);


				// download a folder and all its files
				await ftp.DownloadDirectoryAsync(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update, token: token);
				
				// download a folder and all its files, and delete extra files on disk
				await ftp.DownloadDirectoryAsync(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror, token: token);
				
			}
		}

	}
}