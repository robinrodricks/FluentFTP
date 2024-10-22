namespace FluentFTP.Tests {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentFTP.Model.Functions;
	using FluentFTP.Monitors;
	using FluentFTP.Rules;
	using FluentFTP.Servers;
	using global::Xunit;

	public class AsyncMonitorTests {
		[Fact]
		public async Task FileChangesWithoutWaitTest() {
			using var cts = new CancellationTokenSource();
			var mock = new FtpClientMock();
			await using var monitor = new AsyncFtpMonitor(mock, "");

			monitor.WaitTillFileFullyUploaded = false;
			monitor.PollInterval = TimeSpan.FromSeconds(0.1);

			int eventCounter = 0;

			monitor.SetHandler((_, e) => {
				switch (++eventCounter) {
					case 1:
						Assert.Single(e.Added);
						Assert.Empty(e.Changed);
						Assert.Empty(e.Deleted);
						break;
					case 2:
						Assert.Single(e.Added);
						Assert.Single(e.Changed);
						Assert.Empty(e.Deleted);
						break;
					case 3:
						Assert.Empty(e.Added);
						Assert.Single(e.Changed);
						Assert.Single(e.Deleted);
						cts.Cancel();
						break;
					default:
						Assert.True(false);
						break;
				}

				return Task.CompletedTask;
			});

			var startTask = monitor.Start(cts.Token);
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Add one file
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 100 } });
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// No changes = no call to handler
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Change one file, Add another
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 110 },
				                    new FtpListItem { FullName = "file2.txt", Size = 100 }});
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Change one file, Delete another
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 120 } });
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));

			Assert.Same(startTask, await Task.WhenAny(startTask, Task.Delay(TimeSpan.FromSeconds(0.5))));
			await startTask;
		}

		[Fact]
		public async Task FileChangesWithWaitTest() {
			using var cts = new CancellationTokenSource();
			var mock = new FtpClientMock();
			await using var monitor = new AsyncFtpMonitor(mock, "");

			monitor.WaitTillFileFullyUploaded = true;
			monitor.PollInterval = TimeSpan.FromSeconds(0.1);
			monitor.UnstablePollInterval = TimeSpan.FromSeconds(0.1);

			int eventCounter = 0;

			monitor.SetHandler((_, e) => {
				switch (++eventCounter) {
					case 1:
						Assert.Single(e.Added);
						Assert.Empty(e.Changed);
						Assert.Empty(e.Deleted);
						break;
					case 2:
						Assert.Single(e.Added);
						Assert.Empty(e.Changed);
						Assert.Empty(e.Deleted);
						cts.Cancel();
						break;
					default:
						Assert.True(false);
						break;
				}
				return Task.CompletedTask;
			});

			var startTask = monitor.Start(cts.Token);

			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Add one file. No call to handler
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 100 } });
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Change file, Add another. Still no call to handler
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 110 },
									new FtpListItem { FullName = "file2.txt", Size = 100 }});
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// One file stable, one not
			mock.SetListing(new[] { new FtpListItem { FullName = "file1.txt", Size = 110 },
  				                    new FtpListItem { FullName = "file2.txt", Size = 110 }});
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));
			// Both files stable
			await mock.WaitForPoll(TimeSpan.FromSeconds(1));

			Assert.Same(startTask, await Task.WhenAny(startTask, Task.Delay(TimeSpan.FromSeconds(0.5))));
			await startTask;
		}
	}

#if NETCOREAPP
#nullable disable warnings
#endif
#pragma warning disable 67 // Event never used
	public class FtpClientMock : IAsyncFtpClient {
		private readonly ManualResetEventSlim _poll = new(false);
		private FtpListItem[] _listing = Array.Empty<FtpListItem>();

		public async Task WaitForPoll(TimeSpan timeout) {
			_poll.Reset();
			if (!await Task.Run(() => _poll.Wait(timeout)))
				throw new TimeoutException();
		}

		public void SetListing(FtpListItem[] listing) => _listing = listing;

		public Task<FtpListItem[]> GetListing(string path, FtpListOption options, CancellationToken token) {
			_poll.Set();
			return Task.FromResult(_listing);
		}

		public List<FtpCapability> Capabilities { get; set; } = new();

		void IDisposable.Dispose() {
			_poll.Dispose();
		}

		ValueTask IAsyncFtpClient.DisposeAsync() => ValueTask.CompletedTask;

		ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

		#region NotImplemented

		public bool HasFeature(FtpCapability cap) => throw new NotImplementedException();

		public Task DisableUTF8(CancellationToken token = default(CancellationToken)) => throw new NotImplementedException();

		public Task<FtpProfile> AutoConnect(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<FtpProfile>> AutoDetect(FtpAutoDetectConfig config, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<FtpProfile>> AutoDetect(bool firstOnly, bool cloneConnection = true, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Connect(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Connect(FtpProfile profile, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Connect(bool reConnect, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Disconnect(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpReply> Execute(string command, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<string>> ExecuteDownloadText(string command, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpReply> GetReply(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task DeleteFile(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task DeleteDirectory(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task DeleteDirectory(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task EmptyDirectory(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task EmptyDirectory(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> DirectoryExists(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> FileExists(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> CreateDirectory(string path, bool force, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> CreateDirectory(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Rename(string path, string dest, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> MoveFile(string path,
		                           string dest,
		                           FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                           CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> MoveDirectory(string path,
		                                string dest,
		                                FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task SetFilePermissions(string path, int permissions, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Chmod(string path, int permissions, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task SetFilePermissions(string path,
		                               FtpPermission owner,
		                               FtpPermission group,
		                               FtpPermission other,
		                               CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task Chmod(string path,
		                  FtpPermission owner,
		                  FtpPermission group,
		                  FtpPermission other,
		                  CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpListItem> GetFilePermissions(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<int> GetChmod(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task SetWorkingDirectory(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<string> GetWorkingDirectory(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<long> GetFileSize(string path, long defaultValue = -1, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<DateTime> GetModifiedTime(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task SetModifiedTime(string path, DateTime date, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpListItem> GetObjectInfo(string path, bool dateModified = false, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpListItem[]> GetListing(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpListItem[]> GetListing(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<string[]> GetNameListing(string path, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<string[]> GetNameListing(CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path,
		                                                          FtpListOption options,
		                                                          CancellationToken token = default,
		                                                          CancellationToken enumToken = default) {
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path,
		                                                          CancellationToken token = default,
		                                                          CancellationToken enumToken = default) {
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<FtpListItem> GetListingEnumerable(CancellationToken token = default, CancellationToken enumToken = default) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenRead(string path,
		                             FtpDataType type = FtpDataType.Binary,
		                             long restart = 0,
		                             bool checkIfFileExists = true,
		                             CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenRead(string path,
		                             FtpDataType type,
		                             long restart,
		                             long fileLen,
		                             CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenWrite(string path,
		                              FtpDataType type = FtpDataType.Binary,
		                              bool checkIfFileExists = true,
		                              CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenWrite(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenAppend(string path,
		                               FtpDataType type = FtpDataType.Binary,
		                               bool checkIfFileExists = true,
		                               CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<Stream> OpenAppend(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<FtpResult>> UploadFiles(IEnumerable<string> localPaths,
		                                         string remoteDir,
		                                         FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                         bool createRemoteDir = true,
		                                         FtpVerify verifyOptions = FtpVerify.None,
		                                         FtpError errorHandling = FtpError.None,
		                                         CancellationToken token = default(CancellationToken),
		                                         IProgress<FtpProgress> progress = null,
		                                         List<FtpRule> rules = null) {
			throw new NotImplementedException();
		}

		public Task<List<FtpResult>> UploadFiles(IEnumerable<FileInfo> localFiles,
		                                         string remoteDir,
		                                         FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                         bool createRemoteDir = true,
		                                         FtpVerify verifyOptions = FtpVerify.None,
		                                         FtpError errorHandling = FtpError.None,
		                                         CancellationToken token = default(CancellationToken),
		                                         IProgress<FtpProgress> progress = null,
		                                         List<FtpRule> rules = null) {
			throw new NotImplementedException();
		}

		public Task<List<FtpResult>> DownloadFiles(string localDir,
		                                           IEnumerable<string> remotePaths,
		                                           FtpLocalExists existsMode = FtpLocalExists.Overwrite,
		                                           FtpVerify verifyOptions = FtpVerify.None,
		                                           FtpError errorHandling = FtpError.None,
		                                           CancellationToken token = default(CancellationToken),
		                                           IProgress<FtpProgress> progress = null,
		                                           List<FtpRule> rules = null) {
			throw new NotImplementedException();
		}

		public Task<FtpStatus> UploadFile(string localPath,
		                                  string remotePath,
		                                  FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                  bool createRemoteDir = false,
		                                  FtpVerify verifyOptions = FtpVerify.None,
		                                  IProgress<FtpProgress> progress = null,
		                                  CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpStatus> UploadStream(Stream fileStream,
		                                    string remotePath,
		                                    FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                    bool createRemoteDir = false,
		                                    IProgress<FtpProgress> progress = null,
		                                    CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpStatus> UploadBytes(byte[] fileData,
		                                   string remotePath,
		                                   FtpRemoteExists existsMode = FtpRemoteExists.Overwrite,
		                                   bool createRemoteDir = false,
		                                   IProgress<FtpProgress> progress = null,
		                                   CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpStatus> DownloadFile(string localPath,
		                                    string remotePath,
		                                    FtpLocalExists existsMode = FtpLocalExists.Overwrite,
		                                    FtpVerify verifyOptions = FtpVerify.None,
		                                    IProgress<FtpProgress> progress = null,
		                                    CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<bool> DownloadStream(Stream outStream,
		                                 string remotePath,
		                                 long restartPosition = 0,
		                                 IProgress<FtpProgress> progress = null,
		                                 CancellationToken token = default(CancellationToken),
		                                 long stopPosition = 0) {
			throw new NotImplementedException();
		}

		public Task<byte[]> DownloadBytes(string remotePath,
		                                  long restartPosition = 0,
		                                  IProgress<FtpProgress> progress = null,
		                                  CancellationToken token = default(CancellationToken),
		                                  long stopPosition = 0) {
			throw new NotImplementedException();
		}

		public Task<byte[]> DownloadBytes(string remotePath, CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<byte[]> DownloadUriBytes(string uri,
		                                     IProgress<FtpProgress> progress = null,
		                                     CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<FtpResult>> DownloadDirectory(string localFolder,
		                                               string remoteFolder,
		                                               FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
		                                               FtpLocalExists existsMode = FtpLocalExists.Skip,
		                                               FtpVerify verifyOptions = FtpVerify.None,
		                                               List<FtpRule> rules = null,
		                                               IProgress<FtpProgress> progress = null,
		                                               CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<List<FtpResult>> UploadDirectory(string localFolder,
		                                             string remoteFolder,
		                                             FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
		                                             FtpRemoteExists existsMode = FtpRemoteExists.Skip,
		                                             FtpVerify verifyOptions = FtpVerify.None,
		                                             List<FtpRule> rules = null,
		                                             IProgress<FtpProgress> progress = null,
		                                             CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpHash> GetChecksum(string path,
		                                 FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE,
		                                 CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		public Task<FtpCompareResult> CompareFile(string localPath,
		                                          string remotePath,
		                                          FtpCompareOption options = FtpCompareOption.Auto,
		                                          CancellationToken token = default(CancellationToken)) {
			throw new NotImplementedException();
		}

		void IAsyncFtpClient.Dispose() { }

		public FtpConfig Config { get; set; }

		public IFtpLogger Logger { get; set; }

		public bool IsDisposed { get; }

		public bool IsConnected { get; }

		public string Host { get; set; }

		public int Port { get; set; }

		public NetworkCredential Credentials { get; set; }

		public FtpHashAlgorithm HashAlgorithms { get; }

		public event FtpSslValidation? ValidateCertificate;

		public string SystemType { get; }

		public FtpServer ServerType { get; }

		public FtpBaseServer ServerHandler { get; set; }

		public FtpOperatingSystem ServerOS { get; }

		public string ConnectionType { get; }

		public FtpReply LastReply { get; }

		public List<FtpReply> LastReplies { get; set; }

		public Encoding Encoding { get; set; }

		public Action<FtpTraceLevel, string> LegacyLogger { get; set; }

		public FtpClientState Status { get; }

		public FtpIpVersion? InternetProtocol { get; }

		public bool IsAuthenticated { get; }

		public bool IsEncrypted { get; }

		public bool ValidateCertificateHandlerExists { get; }

		public bool RecursiveList { get; }

		public IPEndPoint SocketLocalEndPoint { get; }

		public IPEndPoint SocketRemoteEndPoint { get; }

		#endregion
	}
}