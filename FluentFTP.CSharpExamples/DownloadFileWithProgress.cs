using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class DownloadFileWithProgressExample {

		public static void DownloadFile() {
			using (var ftp = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				ftp.Connect();

				// define the progress tracking callback
				Action<FtpProgress> progress = delegate (FtpProgress p) {
					if (p.Progress == 1) {
						// all done!
					}
					else {
						// percent done = (p.Progress * 100)
					}
				};

				// download a file with progress tracking
				ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			}
		}

		public static async Task DownloadFileAsync() {
			var token = new CancellationToken();
			using (var ftp = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await ftp.Connect(token);

				// define the progress tracking callback
				Progress<FtpProgress> progress = new Progress<FtpProgress>(p => {
					if (p.Progress == 1) {
						// all done!
					}
					else {
						// percent done = (p.Progress * 100)
					}
				});

				// download a file and ensure the local directory is created
				await ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Resume, FtpVerify.None, progress, token);

			}
		}

	}
}