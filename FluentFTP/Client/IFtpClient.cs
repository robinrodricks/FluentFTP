using FluentFTP.Rules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
#if (CORE || NETFX)
using System.Threading;

#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif

namespace FluentFTP {

	/// <summary>
	/// Interface for the FtpClient class.
	/// For detailed documentation of the methods, please see the FtpClient class or check the Wiki on the FluentFTP Github project.
	/// </summary>
	public interface IFtpClient : IDisposable {
		// CONNECTION

		bool IsDisposed { get; }
		FtpIpVersion InternetProtocolVersions { get; set; }
		int SocketPollInterval { get; set; }
		bool StaleDataCheck { get; set; }
		bool IsConnected { get; }
		bool EnableThreadSafeDataConnections { get; set; }
		int NoopInterval { get; set; }
		Encoding Encoding { get; set; }
		string Host { get; set; }
		int Port { get; set; }
		NetworkCredential Credentials { get; set; }
		int MaximumDereferenceCount { get; set; }
		X509CertificateCollection ClientCertificates { get; }
		Func<string> AddressResolver { get; set; }
		IEnumerable<int> ActivePorts { get; set; }
		FtpDataConnectionType DataConnectionType { get; set; }
		bool UngracefullDisconnection { get; set; }
		int ConnectTimeout { get; set; }
		int ReadTimeout { get; set; }
		int DataConnectionConnectTimeout { get; set; }
		int DataConnectionReadTimeout { get; set; }
		bool SocketKeepAlive { get; set; }
		List<FtpCapability> Capabilities { get; }
		FtpHashAlgorithm HashAlgorithms { get; }
		FtpEncryptionMode EncryptionMode { get; set; }
		bool DataConnectionEncryption { get; set; }
		SslProtocols SslProtocols { get; set; }
		string SystemType { get; }
		string ConnectionType { get; }
		FtpParser ListingParser { get; set; }
		CultureInfo ListingCulture { get; set; }
		double TimeOffset { get; set; }
		bool RecursiveList { get; set; }
		bool BulkListing { get; set; }
		int BulkListingLength { get; set; }
		int TransferChunkSize { get; set; }
		int RetryAttempts { get; set; }
		uint UploadRateLimit { get; set; }
		uint DownloadRateLimit { get; set; }
		FtpDataType UploadDataType { get; set; }
		FtpDataType DownloadDataType { get; set; }
		event FtpSslValidation ValidateCertificate;
		FtpReply Execute(string command);
		FtpReply GetReply();
		void Connect();
		void Connect(FtpProfile profile);
		List<FtpProfile> AutoDetect(bool firstOnly);
		FtpProfile AutoConnect();
		void Disconnect();
		bool HasFeature(FtpCapability cap);
		void DisableUTF8();

#if ASYNC
		Task<FtpReply> ExecuteAsync(string command, CancellationToken token = default(CancellationToken));
		Task<FtpReply> GetReplyAsync(CancellationToken token = default(CancellationToken));
		Task ConnectAsync(CancellationToken token = default(CancellationToken));
		Task ConnectAsync(FtpProfile profile, CancellationToken token = default(CancellationToken));
		Task<FtpProfile> AutoConnectAsync(CancellationToken token = default(CancellationToken));

		Task DisconnectAsync(CancellationToken token = default(CancellationToken));
#endif


		// MANAGEMENT

		void DeleteFile(string path);
		void DeleteDirectory(string path);
		void DeleteDirectory(string path, FtpListOption options);
		bool DirectoryExists(string path);
		bool FileExists(string path);
		bool CreateDirectory(string path);
		bool CreateDirectory(string path, bool force);
		void Rename(string path, string dest);
		bool MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite);
		bool MoveDirectory(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite);
		void SetFilePermissions(string path, int permissions);
		void Chmod(string path, int permissions);
		void SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other);
		void Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other);
		FtpListItem GetFilePermissions(string path);
		int GetChmod(string path);
		FtpListItem DereferenceLink(FtpListItem item);
		FtpListItem DereferenceLink(FtpListItem item, int recMax);
		void SetWorkingDirectory(string path);
		string GetWorkingDirectory();
		long GetFileSize(string path);
		DateTime GetModifiedTime(string path, FtpDate type = FtpDate.Original);
		void SetModifiedTime(string path, DateTime date, FtpDate type = FtpDate.Original);

#if ASYNC
		Task DeleteFileAsync(string path, CancellationToken token = default(CancellationToken));
		Task DeleteDirectoryAsync(string path, CancellationToken token = default(CancellationToken));
		Task DeleteDirectoryAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken));
		Task<bool> DirectoryExistsAsync(string path, CancellationToken token = default(CancellationToken));
		Task<bool> FileExistsAsync(string path, CancellationToken token = default(CancellationToken));
		Task<bool> CreateDirectoryAsync(string path, bool force, CancellationToken token = default(CancellationToken));
		Task<bool> CreateDirectoryAsync(string path, CancellationToken token = default(CancellationToken));
		Task RenameAsync(string path, string dest, CancellationToken token = default(CancellationToken));
		Task<bool> MoveFileAsync(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken));
		Task<bool> MoveDirectoryAsync(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken));
		Task SetFilePermissionsAsync(string path, int permissions, CancellationToken token = default(CancellationToken));
		Task ChmodAsync(string path, int permissions, CancellationToken token = default(CancellationToken));
		Task SetFilePermissionsAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken));
		Task ChmodAsync(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken));
		Task<FtpListItem> GetFilePermissionsAsync(string path, CancellationToken token = default(CancellationToken));
		Task<int> GetChmodAsync(string path, CancellationToken token = default(CancellationToken));
		Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, CancellationToken token = default(CancellationToken));
		Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, CancellationToken token = default(CancellationToken));
		Task SetWorkingDirectoryAsync(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetWorkingDirectoryAsync(CancellationToken token = default(CancellationToken));
		Task<long> GetFileSizeAsync(string path, CancellationToken token = default(CancellationToken));
		Task<DateTime> GetModifiedTimeAsync(string path, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken));

		Task SetModifiedTimeAsync(string path, DateTime date, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken));
#endif


		// LISTING

		FtpListItem GetObjectInfo(string path, bool dateModified = false);
		FtpListItem[] GetListing();
		FtpListItem[] GetListing(string path);
		FtpListItem[] GetListing(string path, FtpListOption options);
		string[] GetNameListing();
		string[] GetNameListing(string path);

#if ASYNC
		Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListingAsync(string path, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListingAsync(CancellationToken token = default(CancellationToken));
		Task<string[]> GetNameListingAsync(string path, CancellationToken token = default(CancellationToken));

		Task<string[]> GetNameListingAsync(CancellationToken token = default(CancellationToken));
#endif


		// LOW LEVEL

		Stream OpenRead(string path);
		Stream OpenRead(string path, FtpDataType type);
		Stream OpenRead(string path, FtpDataType type, bool checkIfFileExists);
		Stream OpenRead(string path, FtpDataType type, long restart);
		Stream OpenRead(string path, long restart);
		Stream OpenRead(string path, long restart, bool checkIfFileExists);
		Stream OpenRead(string path, FtpDataType type, long restart, bool checkIfFileExists);
		Stream OpenWrite(string path);
		Stream OpenWrite(string path, FtpDataType type);
		Stream OpenWrite(string path, FtpDataType type, bool checkIfFileExists);
		Stream OpenAppend(string path);
		Stream OpenAppend(string path, FtpDataType type);
		Stream OpenAppend(string path, FtpDataType type, bool checkIfFileExists);

#if ASYNC
		Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, bool checkIfFileExists, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenReadAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenReadAsync(string path, long restart, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenReadAsync(string path, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWriteAsync(string path, FtpDataType type, bool checkIfFileExists, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWriteAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWriteAsync(string path, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppendAsync(string path, FtpDataType type, bool checkIfFileExists, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppendAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken));

		Task<Stream> OpenAppendAsync(string path, CancellationToken token = default(CancellationToken));
#endif


		// HIGH LEVEL

		int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);
		int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);
		int DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);

		FtpStatus UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		FtpStatus Upload(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);
		FtpStatus Upload(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);

		FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		bool Download(Stream outStream, string remotePath, long restartPosition, Action<FtpProgress> progress = null);
		bool Download(out byte[] outBytes, string remotePath, long restartPosition, Action<FtpProgress> progress = null);

		List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);
		List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);

#if ASYNC
		Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null);
		Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null);

		Task<FtpStatus> UploadFileAsync(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadAsync(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadAsync(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<bool> DownloadAsync(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<byte[]> DownloadAsync(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));

		Task<List<FtpResult>> DownloadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<List<FtpResult>> UploadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
#endif

		// HASH

		FtpHashAlgorithm GetHashAlgorithm();
		void SetHashAlgorithm(FtpHashAlgorithm type);
		FtpHash GetHash(string path);
		FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);
		string GetMD5(string path);
		string GetXCRC(string path);
		string GetXMD5(string path);
		string GetXSHA1(string path);
		string GetXSHA256(string path);
		string GetXSHA512(string path);

#if ASYNC
		Task<FtpHashAlgorithm> GetHashAlgorithmAsync(CancellationToken token = default(CancellationToken));
		Task SetHashAlgorithmAsync(FtpHashAlgorithm type, CancellationToken token = default(CancellationToken));
		Task<FtpHash> GetHashAsync(string path, CancellationToken token = default(CancellationToken));
		Task<FtpHash> GetChecksumAsync(string path, CancellationToken token = default(CancellationToken), FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);
		Task<string> GetMD5Async(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetXCRCAsync(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetXMD5Async(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetXSHA1Async(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetXSHA256Async(string path, CancellationToken token = default(CancellationToken));

		Task<string> GetXSHA512Async(string path, CancellationToken token = default(CancellationToken));
#endif
	}
}