using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Servers;
using System.Net;
using System.Security.Authentication;
using System.Text;
using FluentFTP.Model.Functions;
using FluentFTP.Rules;

/// <summary>
/// Fake AsyncFtpClient for use in mocking. Write your tests against IAsyncFtpClient.
/// </summary>
public class FakeAsyncFtpClient : IAsyncFtpClient {

	//------------------------------------------------
	//			IMPLEMENT IBaseFtpClient
	//------------------------------------------------
	public FtpConfig Config { get; set; }
	public IFtpLogger Logger { get; set; }
	public bool IsDisposed { get; }
	public bool IsConnected { get; }
	public string Host { get; set; }
	public int Port { get; set; }
	public NetworkCredential Credentials { get; set; }
	public List<FtpCapability> Capabilities { get; }
	public FtpHashAlgorithm HashAlgorithms { get; }
	public event FtpSslValidation ValidateCertificate;
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
	public SslProtocols SslProtocol { get; }
	public string SslCipherSuite { get; }
	public bool IsEncrypted { get; }
	public bool ValidateCertificateHandlerExists { get; }
	public bool RecursiveList { get; }
	public IPEndPoint SocketLocalEndPoint { get; }
	public IPEndPoint SocketRemoteEndPoint { get; }


	//------------------------------------------------
	//			IMPLEMENT IAsyncFtpClient
	//------------------------------------------------
	public void Dispose() { }

	public bool HasFeature(FtpCapability cap) {
		return false;
	}

	public async Task DisableUTF8(CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<FtpProfile> AutoConnect(CancellationToken token = default) {
		return await Task.FromResult(new FtpProfile());
	}

	public async Task<List<FtpProfile>> AutoDetect(FtpAutoDetectConfig config, CancellationToken token = default) {
		return await Task.FromResult(new List<FtpProfile>());
	}

	public async Task<List<FtpProfile>> AutoDetect(bool firstOnly, bool cloneConnection = true, CancellationToken token = default) {
		return await Task.FromResult(new List<FtpProfile>());
	}

	public async Task Connect(CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task Connect(FtpProfile profile, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task Connect(bool reConnect, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task Disconnect(CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<FtpReply> Execute(string command, CancellationToken token = default) {
		return await Task.FromResult(new FtpReply());
	}

	public async Task<FtpReply> Execute(string command, int linesExpected, CancellationToken token = default) {
		return await Task.FromResult(new FtpReply());
	}

	public async Task<List<string>> ExecuteDownloadText(string command, CancellationToken token = default) {
		return await Task.FromResult(new List<string>());
	}

	public async Task<FtpReply> GetReply(CancellationToken token = default) {
		return await Task.FromResult(new FtpReply());
	}

	public async Task DeleteFile(string path, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task DeleteDirectory(string path, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task DeleteDirectory(string path, FtpListOption options, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task EmptyDirectory(string path, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task EmptyDirectory(string path, FtpListOption options, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<bool> DirectoryExists(string path, CancellationToken token = default) {
		return await Task.FromResult(false);
	}

	public async Task<bool> FileExists(string path, CancellationToken token = default) {
		return await Task.FromResult(false);
	}

	public async Task<bool> CreateDirectory(string path, bool force, CancellationToken token = default) {
		return await Task.FromResult(true);
	}

	public async Task<bool> CreateDirectory(string path, CancellationToken token = default) {
		return await Task.FromResult(true);
	}

	public async Task Rename(string path, string dest, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<bool> MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default) {
		return await Task.FromResult(true);
	}

	public async Task<bool> MoveDirectory(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default) {
		return await Task.FromResult(true);
	}

	public async Task SetFilePermissions(string path, int permissions, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task Chmod(string path, int permissions, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<FtpListItem> GetFilePermissions(string path, CancellationToken token = default) {
		return await Task.FromResult(new FtpListItem());
	}

	public async Task<int> GetChmod(string path, CancellationToken token = default) {
		return await Task.FromResult(644);
	}

	public async Task SetWorkingDirectory(string path, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<string> GetWorkingDirectory(CancellationToken token = default) {
		return await Task.FromResult("/");
	}

	public async Task<long> GetFileSize(string path, long defaultValue = -1, CancellationToken token = default) {
		return await Task.FromResult(0L);
	}

	public async Task<DateTime> GetModifiedTime(string path, CancellationToken token = default) {
		return await Task.FromResult(DateTime.UtcNow);
	}

	public async Task SetModifiedTime(string path, DateTime date, CancellationToken token = default) {
		await Task.CompletedTask;
	}

	public async Task<FtpListItem> GetObjectInfo(string path, bool dateModified = false, CancellationToken token = default) {
		return await Task.FromResult(new FtpListItem());
	}

	public async Task<FtpListItem[]> GetListing(string path, FtpListOption options, CancellationToken token = default) {
		return await Task.FromResult(new FtpListItem[0]);
	}

	public async Task<FtpListItem[]> GetListing(string path, CancellationToken token = default) {
		return await Task.FromResult(new FtpListItem[0]);
	}

	public async Task<FtpListItem[]> GetListing(CancellationToken token = default) {
		return await Task.FromResult(new FtpListItem[0]);
	}

	public async Task<string[]> GetNameListing(string path, CancellationToken token = default) {
		return await Task.FromResult(new string[0]);
	}

	public async Task<string[]> GetNameListing(CancellationToken token = default) {
		return await Task.FromResult(new string[0]);
	}

	public async Task<Stream> OpenRead(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<Stream> OpenRead(string path, FtpDataType type, long restart, long fileLen, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<Stream> OpenWrite(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<Stream> OpenWrite(string path, FtpDataType type, long fileLen, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<Stream> OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<Stream> OpenAppend(string path, FtpDataType type, long fileLen, CancellationToken token = default) {
		return await Task.FromResult(Stream.Null);
	}

	public async Task<List<FtpResult>> UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default, IProgress<FtpProgress> progress = null, List<FtpRule> rules = null) {
		return await Task.FromResult(new List<FtpResult>());
	}

	public async Task<List<FtpResult>> UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default, IProgress<FtpProgress> progress = null, List<FtpRule> rules = null) {
		return await Task.FromResult(new List<FtpResult>());
	}

	public async Task<List<FtpResult>> DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default, IProgress<FtpProgress> progress = null, List<FtpRule> rules = null) {
		return await Task.FromResult(new List<FtpResult>());
	}

	public async Task<FtpStatus> UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(FtpStatus.Success);
	}

	public async Task<FtpStatus> UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(FtpStatus.Success);
	}

	public async Task<FtpStatus> UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(FtpStatus.Success);
	}

	public async Task<FtpStatus> DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(FtpStatus.Success);
	}

	public async Task<bool> DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default, long stopPosition = 0) {
		return await Task.FromResult(true);
	}

	public async Task<byte[]> DownloadBytes(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default, long stopPosition = 0) {
		return await Task.FromResult(new byte[0]);
	}

	public async Task<byte[]> DownloadBytes(string remotePath, CancellationToken token = default) {
		return await Task.FromResult(new byte[0]);
	}

	public async Task<byte[]> DownloadUriBytes(string uri, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(new byte[0]);
	}

	public async Task<List<FtpResult>> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(new List<FtpResult>());
	}

	public async Task<List<FtpResult>> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default) {
		return await Task.FromResult(new List<FtpResult>());
	}

	public async Task<FtpHash> GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken token = default) {
		return await Task.FromResult(new FtpHash());
	}

	public async Task<FtpCompareResult> CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto, CancellationToken token = default) {
		return await Task.FromResult(new FtpCompareResult());
	}

#if NET5_0_OR_GREATER
	public async IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, FtpListOption options, CancellationToken token = default, CancellationToken enumToken = default) {
		await Task.CompletedTask;
		yield break;
	}

	public async IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, CancellationToken token = default, CancellationToken enumToken = default) {
		await Task.CompletedTask;
		yield break;
	}

	public async IAsyncEnumerable<FtpListItem> GetListingEnumerable(CancellationToken token = default, CancellationToken enumToken = default) {
		await Task.CompletedTask;
		yield break;
	}
#endif
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	ValueTask IAsyncDisposable.DisposeAsync() {
		throw new NotImplementedException();
	}
#endif

}
