using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Monitors;

namespace Examples {

	internal static class MonitorExample {

		public static async Task DownloadStablePdfFilesAsync(CancellationToken token) {
			var conn = new AsyncFtpClient("127.0.0.1", "ftptest", "ftptest");

			await using var monitor = new AsyncFtpMonitor(conn, "path/to/folder");

			monitor.PollInterval = TimeSpan.FromMinutes(5);
			monitor.WaitTillFileFullyUploaded = true;
			monitor.UnstablePollInterval = TimeSpan.FromSeconds(10);

			monitor.SetHandler(static async (_, e) => {
				foreach (var file in e.Added
				                      .Where(x => x.Type == FtpObjectType.File)
				                      .Where(x => Path.GetExtension(x.Name) == ".pdf")) {
					var localFilePath = Path.Combine(@"C:\LocalFolder", file.Name);
					await e.FtpClient.DownloadFile(localFilePath, file.FullName);
					await e.FtpClient.DeleteFile(file.FullName);
				}
			});

			await conn.Connect();
			await monitor.Start(token);
		}
	}
}
