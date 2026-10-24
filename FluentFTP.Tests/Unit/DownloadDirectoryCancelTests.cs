using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FluentFTP.Tests.Unit {
	public class DownloadDirectoryCancelTests : IDisposable {

		private class TestClient : AsyncFtpClient {
			public Task Download(List<FtpResult> toDownload, FtpLocalExists existsMode, CancellationToken token) {
				return DownloadServerFilesAsync(toDownload, existsMode, FtpVerify.None, null, token);
			}
		}

		private readonly string localDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

		public DownloadDirectoryCancelTests() {
			Directory.CreateDirectory(localDir);
		}

		public void Dispose() {
			Directory.Delete(localDir, true);
		}

		private FtpResult FileResult(string name) {
			return new FtpResult { Type = FtpObjectType.File, RemotePath = "/dir/" + name, LocalPath = Path.Combine(localDir, name) };
		}

		[Fact]
		public async Task CancelStopsRemainingFiles() {
			var files = new List<FtpResult> { FileResult("first.txt"), FileResult("second.txt") };

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new TestClient().Download(files, FtpLocalExists.Overwrite, cts.Token));

			// no file should have been attempted after cancellation
			Assert.All(files, f => Assert.False(f.IsFailed));
		}

		[Fact]
		public async Task CancelDuringLastFileThrows() {
			using var cts = new CancellationTokenSource();
			var client = new TestClient();

			// cancel once the download of the file has started
			client.LegacyLogger = (level, message) => {
				if (message.Contains("DownloadFile(")) {
					cts.Cancel();
				}
			};

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.Download(new List<FtpResult> { FileResult("only.txt") }, FtpLocalExists.Skip, cts.Token));
		}
	}
}
