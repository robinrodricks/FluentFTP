using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {

	internal static class UploadFileWithProgressExample {

		public static void UploadFile() {
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

				// upload a file with progress tracking
				ftp.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, false, FtpVerify.None, progress);

			}
		}

		public static async Task UploadFileAsync() {
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

				// upload a file with progress tracking
				await ftp.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpRemoteExists.Overwrite, false, FtpVerify.None, progress, token);

			}
		}

	}
}