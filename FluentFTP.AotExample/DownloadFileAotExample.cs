using FluentFTP;

namespace Examples {

	internal static class DownloadFileAotExample {

		public static async Task RunExampleAsync() {
			var token = new CancellationToken();

			var config = new FtpConfig {
				LogToConsole = true
			};

			await using (var ftp = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest", config: config)) {
				Console.WriteLine("Running AutoDetect to trigger AOT-sensitive logging paths...");

				var profiles = await ftp.AutoDetect(firstOnly: true, token: token);

				if (profiles != null && profiles.Count > 0) {
					Console.WriteLine("Profile found! Connecting...");
					await ftp.Connect(token);

					// download a file and ensure the local directory is created, verify the file after download
					Console.WriteLine("Downloading file...");
					await ftp.DownloadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md", FtpLocalExists.Overwrite, FtpVerify.Retry, token: token);
				}
				else {
					Console.WriteLine("No valid connection profile found (expected if no local FTP server is running).");
				}
			}
		}
	}
}