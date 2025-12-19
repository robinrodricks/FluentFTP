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
/// Fake FtpClient for use in mocking. Write your tests against IFtpClient.
/// </summary>
public class FakeFtpClient : IFtpClient {

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
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public event FtpSslAuthentication ConfigureAuthentication;
#endif
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
	//			IMPLEMENT IFtpClient
	//------------------------------------------------

	public void Connect() { }
    public void Connect(FtpProfile profile) { }
    public void Connect(bool reConnect) { }
    public void Disconnect() { }
    public bool HasFeature(FtpCapability cap) => false;
    public void DisableUTF8() { }
    public FtpProfile AutoConnect() => null;
    public List<FtpProfile> AutoDetect(FtpAutoDetectConfig config) => null;
    public List<FtpProfile> AutoDetect(bool firstOnly, bool cloneConnection = true) => null;
    public FtpReply Execute(string command) => new FtpReply();
    public FtpReply Execute(string command, int linesExpected) => new FtpReply();
    public List<string> ExecuteDownloadText(string command) => null;
    public FtpReply GetReply() => new FtpReply();

    public void DeleteFile(string path) { }
    public void DeleteDirectory(string path) { }
    public void DeleteDirectory(string path, FtpListOption options) { }
    public void EmptyDirectory(string path) { }
    public void EmptyDirectory(string path, FtpListOption options) { }
    public bool DirectoryExists(string path) => false;
    public bool FileExists(string path) => false;
    public bool CreateDirectory(string path) => false;
    public bool CreateDirectory(string path, bool force) => false;
    public void Rename(string path, string dest) { }
    public bool MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite) => false;
    public bool MoveDirectory(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite) => false;
    public void SetFilePermissions(string path, int permissions) { }
    public void Chmod(string path, int permissions) { }
    public void SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other) { }
    public void Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other) { }
    public FtpListItem GetFilePermissions(string path) => null;
    public int GetChmod(string path) => 0;
    public void SetWorkingDirectory(string path) { }
    public string GetWorkingDirectory() => null;
    public long GetFileSize(string path, long defaultValue = -1) => defaultValue;
    public DateTime GetModifiedTime(string path) => DateTime.MinValue;
    public void SetModifiedTime(string path, DateTime date) { }

    public FtpListItem GetObjectInfo(string path, bool dateModified = false) => null;
    public FtpListItem[] GetListing() => null;
    public FtpListItem[] GetListing(string path) => null;
    public FtpListItem[] GetListing(string path, FtpListOption options) => null;
    public string[] GetNameListing() => null;
    public string[] GetNameListing(string path) => null;

    public Stream OpenRead(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true) => null;
    public Stream OpenRead(string path, FtpDataType type, long restart, long fileLen) => null;
    public Stream OpenWrite(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true) => null;
    public Stream OpenWrite(string path, FtpDataType type, long fileLen) => null;
    public Stream OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true) => null;
    public Stream OpenAppend(string path, FtpDataType type, long fileLen) => null;

    public List<FtpResult> UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null) => null;

    public List<FtpResult> UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null) => null;

    public List<FtpResult> DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null) => null;

    public FtpStatus UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) => FtpStatus.Success;

    public FtpStatus UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null) => FtpStatus.Success;

    public FtpStatus UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null) => FtpStatus.Success;

    public FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) => FtpStatus.Success;

    public bool DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null, long stopPosition = 0) => true;

    public bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null, long stopPosition = 0) { outBytes = null; return true; }

    public bool DownloadUriBytes(out byte[] outBytes, string uri, Action<FtpProgress> progress = null) { outBytes = null; return true; }

    public List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) => null;

    public List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) => null;

    public FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE) => null;
    public FtpCompareResult CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto) => FtpCompareResult.NotEqual;

    public void Dispose() { }
}
