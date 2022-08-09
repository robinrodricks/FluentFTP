using FluentFTP.Rules;
using FluentFTP.Servers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
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

		// PROPERTIES (From FtpClient_Properties)

		bool IsDisposed { get;}
		FtpIpVersion InternetProtocolVersions { get; set; }
		int SocketPollInterval { get; set; }
		bool StaleDataCheck { get; set; }
		bool IsConnected { get; }
		bool EnableThreadSafeDataConnections { get; set; }
		int NoopInterval { get; set; }
		bool CheckCapabilities { get; set; }
		Encoding Encoding { get; set; }
		string Host { get; set; }
		int Port { get; set; }
		NetworkCredential Credentials { get; set; }
		int MaximumDereferenceCount { get; set; }
		X509CertificateCollection ClientCertificates { get; }
		Func<string> AddressResolver { get; set; }
		IEnumerable<int> ActivePorts { get; set; }
		FtpDataConnectionType DataConnectionType { get; set; }
		bool DisconnectWithQuit { get; set; }
		bool DisconnectWithShutdown { get; set; }
		int ConnectTimeout { get; set; }
		int ReadTimeout { get; set; }
		int DataConnectionConnectTimeout { get; set; }
		int DataConnectionReadTimeout { get; set; }
		bool SocketKeepAlive { get; set; }
		List<FtpCapability> Capabilities { get; }
		FtpHashAlgorithm HashAlgorithms { get; }
		FtpEncryptionMode EncryptionMode { get; set; }
		bool DataConnectionEncryption { get; set; }
#if !CORE
		bool PlainTextEncryption { get; set; }
#endif
		SslProtocols SslProtocols { get; set; }
		FtpsBuffering SslBuffering { get; set; }
		event FtpSslValidation ValidateCertificate;
		bool ValidateAnyCertificate { get; set; }
		bool ValidateCertificateRevocation { get; set; }
		string SystemType { get; }
		FtpServer ServerType { get; }
		FtpBaseServer ServerHandler { get; set; }
		FtpOperatingSystem ServerOS { get; }
		string ConnectionType { get; }
		FtpReply LastReply { get; }
		FtpDataType ListingDataType { get; set; }
		FtpParser ListingParser { get; set; }
		CultureInfo ListingCulture { get; set; }
		bool RecursiveList { get; set; }
		double TimeZone { get; set; }
#if CORE
		double LocalTimeZone { get; set; }
#endif
		FtpDate TimeConversion { get; set; }
		bool BulkListing { get; set; }
		int BulkListingLength { get; set; }
		int TransferChunkSize { get; set; }
		int LocalFileBufferSize { get; set; }
		int RetryAttempts { get; set; }
		uint UploadRateLimit { get; set; }
		uint DownloadRateLimit { get; set; }
		bool DownloadZeroByteFiles { get; set; }
		FtpDataType UploadDataType { get; set; }
		FtpDataType DownloadDataType { get; set; }
		bool UploadDirectoryDeleteExcluded { get; set; }
		bool DownloadDirectoryDeleteExcluded { get; set; }
		FtpDataType FXPDataType { get; set; }
		int FXPProgressInterval { get; set; }
		bool SendHost { get; set; }
		string SendHostDomain { get; set; }



		// METHODS

		FtpReply Execute(string command);
		FtpReply GetReply();
		void Connect();
		void Connect(FtpProfile profile);
		List<FtpProfile> AutoDetect(bool firstOnly = true, bool cloneConnection = true);
		FtpProfile AutoConnect();
		void Disconnect();
		bool HasFeature(FtpCapability cap);
		void DisableUTF8();

#if ASYNC
		Task<FtpReply> ExecuteAsync(string command, CancellationToken token = default(CancellationToken));
		Task<FtpReply> GetReplyAsync(CancellationToken token = default(CancellationToken));
		Task ConnectAsync(CancellationToken token = default(CancellationToken));
		Task ConnectAsync(FtpProfile profile, CancellationToken token = default(CancellationToken));
		Task<List<FtpProfile>> AutoDetectAsync(bool firstOnly = true, bool cloneConnection = true, CancellationToken token = default(CancellationToken));
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
		long GetFileSize(string path, long defaultValue = -1);
		DateTime GetModifiedTime(string path);
		void SetModifiedTime(string path, DateTime date);

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
		Task<long> GetFileSizeAsync(string path, long defaultValue = -1, CancellationToken token = default(CancellationToken));
		Task<DateTime> GetModifiedTimeAsync(string path, CancellationToken token = default(CancellationToken));

		Task SetModifiedTimeAsync(string path, DateTime date, CancellationToken token = default(CancellationToken));
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

#if NET50_OR_LATER
		IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, FtpListOption options, CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken));
		IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken));
		IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken));
#endif


		// LOW LEVEL

		Stream OpenRead(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true);
		Stream OpenRead(string path, FtpDataType type, long restart, long fileLen);
		Stream OpenWrite(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true);
		Stream OpenWrite(string path, FtpDataType type, long fileLen);
		Stream OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true);
		Stream OpenAppend(string path, FtpDataType type, long fileLen);

#if ASYNC
		Task<Stream> OpenReadAsync(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, long fileLen, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWriteAsync(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWriteAsync(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppendAsync(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppendAsync(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken));
#endif


		// HIGH LEVEL

		int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);
		int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);
		int DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null);

		FtpStatus UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		FtpStatus UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);
		FtpStatus UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);

		FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		bool DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null);
		bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null);

		List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);
		List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);

#if ASYNC
		Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null);
		Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null);

		Task<FtpStatus> UploadFileAsync(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadStreamAsync(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadBytesAsync(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<bool> DownloadStreamAsync(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<byte[]> DownloadBytesAsync(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));

		Task<List<FtpResult>> DownloadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<List<FtpResult>> UploadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
#endif

		// HASH
		FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);

#if ASYNC
		Task<FtpHash> GetChecksumAsync(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken token = default(CancellationToken));

#endif

		// COMPARE
		FtpCompareResult CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto);

#if ASYNC
		Task<FtpCompareResult> CompareFileAsync(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto, CancellationToken token = default(CancellationToken));

#endif
	}
}