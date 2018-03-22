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
	/// Interface for the FtpClient class. For detailed documentation of the methods, please see the FtpClient class.
	/// </summary>
	public interface IFtpClient : IDisposable {


		// CONNECTION

		bool IsDisposed { get; }
		FtpIpVersion InternetProtocolVersions { get; set; }
		int SocketPollInterval { get; set; }
		bool StaleDataCheck { get; set; }
		bool IsConnected { get; }
		bool EnableThreadSafeDataConnections { get; set; }
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
		FtpCapability Capabilities { get; }
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
		void Disconnect();
		bool HasFeature(FtpCapability cap);
		void DisableUTF8();

#if ASYNC
		Task<FtpReply> ExecuteAsync(string command);
		Task<FtpReply> GetReplyAsync();
		Task ConnectAsync();
		Task DisconnectAsync();
#endif


		// MANAGEMENT

		void DeleteFile(string path);
		void DeleteDirectory(string path);
		void DeleteDirectory(string path, FtpListOption options);
		bool DirectoryExists(string path);
		bool FileExists(string path);
		void CreateDirectory(string path);
		void CreateDirectory(string path, bool force);
		void Rename(string path, string dest);
		bool MoveFile(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);
		bool MoveDirectory(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);
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
		Task DeleteFileAsync(string path);
		Task<bool> DirectoryExistsAsync(string path);
		Task<bool> FileExistsAsync(string path);
		Task CreateDirectoryAsync(string path, bool force);
		Task RenameAsync(string path, string dest);
		Task<bool> MoveFileAsync(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);
		Task<bool> MoveDirectoryAsync(string path, string dest, FtpExists existsMode = FtpExists.Overwrite);
		Task SetFilePermissionsAsync(string path, int permissions);
		Task<FtpListItem> GetFilePermissionsAsync(string path);
		Task<int> GetChmodAsync(string path);
		Task SetWorkingDirectoryAsync(string path);
		Task<string> GetWorkingDirectoryAsync();
		Task<long> GetFileSizeAsync(string path);
		Task<DateTime> GetModifiedTimeAsync(string path, FtpDate type = FtpDate.Original);
		Task SetModifiedTimeAsync(string path, DateTime date, FtpDate type = FtpDate.Original);
#endif


		// LISTING

		FtpListItem GetObjectInfo(string path, bool dateModified = false);
		FtpListItem[] GetListing();
		FtpListItem[] GetListing(string path);
		FtpListItem[] GetListing(string path, FtpListOption options);
		string[] GetNameListing();
		string[] GetNameListing(string path);

#if ASYNC
		Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false);
		Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options);
		Task<string[]> GetNameListingAsync(string path);
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
		Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, bool checkIfFileExists);
		Task<Stream> OpenWriteAsync(string path, FtpDataType type, bool checkIfFileExists);
		Task<Stream> OpenAppendAsync(string path, FtpDataType type, bool checkIfFileExists);
#endif


		// HIGH LEVEL

		int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);
		int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);
		int DownloadFiles(string localDir, IEnumerable<string> remotePaths, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);

		bool UploadFile(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null);
		bool Upload(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null);
		bool Upload(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null);

		bool DownloadFile(string localPath, string remotePath, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null);
		bool Download(Stream outStream, string remotePath, IProgress<double> progress = null);
		bool Download(out byte[] outBytes, string remotePath, IProgress<double> progress = null);

#if ASYNC
		Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode, bool createRemoteDir, FtpVerify verifyOptions, FtpError errorHandling, CancellationToken token);
		Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);
		Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite, FtpVerify verifyOptions, FtpError errorHandling, CancellationToken token);
		Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None);
		Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode, bool createRemoteDir, FtpVerify verifyOptions, CancellationToken token, IProgress<double> progress);
		Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None);
		Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token, IProgress<double> progress);
		Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token, IProgress<double> progress);
		Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false);
		Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false);
		Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite, FtpVerify verifyOptions, CancellationToken token, IProgress<double> progress);
		Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null);
		Task<bool> DownloadAsync(Stream outStream, string remotePath, CancellationToken token, IProgress<double> progress = null);
		Task<bool> DownloadAsync(Stream outStream, string remotePath);
		Task<byte[]> DownloadAsync(string remotePath, CancellationToken token, IProgress<double> progress = null);
		Task<byte[]> DownloadAsync(string remotePath);
#endif

		// HASH

		FtpHashAlgorithm GetHashAlgorithm();
		void SetHashAlgorithm(FtpHashAlgorithm type);
		FtpHash GetHash(string path);
		FtpHash GetChecksum(string path);
		string GetMD5(string path);
		string GetXCRC(string path);
		string GetXMD5(string path);
		string GetXSHA1(string path);
		string GetXSHA256(string path);
		string GetXSHA512(string path);

#if ASYNC
		Task<FtpHashAlgorithm> GetHashAlgorithmAsync();
		Task SetHashAlgorithmAsync(FtpHashAlgorithm type);
		Task<FtpHash> GetHashAsync(string path);
		Task<FtpHash> GetChecksumAsync(string path);
		Task<string> GetMD5Async(string path);
		Task<string> GetXCRCAsync(string path);
		Task<string> GetXMD5Async(string path);
		Task<string> GetXSHA1Async(string path);
		Task<string> GetXSHA256Async(string path);
		Task<string> GetXSHA512Async(string path);
#endif
	}
}