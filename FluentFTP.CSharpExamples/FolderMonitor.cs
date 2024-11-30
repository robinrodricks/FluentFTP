using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Monitors;

namespace Examples {

	internal static class FolderMonitorExample {

		// Downloads all PDF files from a folder on an FTP server
		// when they are fully uploaded (stable)
		public static async Task DownloadStablePdfFilesAsync(CancellationToken token) {
			var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest");

			await using var monitor = new BlockingAsyncFtpMonitor(conn, new List<string> { "path/to/folder" });

			monitor.PollInterval = TimeSpan.FromMinutes(5);
			monitor.WaitForUpload = true;

			monitor.ChangeDetected = (static async (e) => {
				foreach (var file in e.Added
				                      .Where(x => Path.GetExtension(x) == ".pdf")) {
					var localFilePath = Path.Combine(@"C:\LocalFolder", Path.GetFileName(file));
					await e.AsyncFtpClient.DownloadFile(localFilePath, file, token: e.CancellationToken);
					await e.AsyncFtpClient.DeleteFile(file); // don't cancel this operation
				}
			});

			await conn.Connect(token);
			await monitor.Start(token);
		}

		// How to use the monitor in a console application
		public static async Task MainAsync() {
			using var tokenSource = new CancellationTokenSource();
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true; // keep running until monitor is stopped
				tokenSource.Cancel(); // stop the monitor
			};

			await DownloadStablePdfFilesAsync(tokenSource.Token);
		}
	}
}
