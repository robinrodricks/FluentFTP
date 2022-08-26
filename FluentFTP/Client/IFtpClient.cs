using FluentFTP.Rules;
using FluentFTP.Servers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {

	/// <summary>
	/// Interface for the FtpClient class.
	/// For detailed documentation of the methods, please see the FtpClient class or check the Wiki on the FluentFTP Github project.
	/// </summary>
	public interface IFtpClient : IDisposable {

		// PROPERTIES (From FtpClient_Properties)

		bool IsDisposed { get; }
		FtpIpVersion InternetProtocolVersions { get; set; }
		int SocketPollInterval { get; set; }
		bool StaleDataCheck { get; set; }
		bool IsConnected { get; }
		int NoopInterval { get; set; }
		bool CheckCapabilities { get; set; }
		Encoding Encoding { get; set; }
		string Host { get; set; }
		int Port { get; set; }
		NetworkCredential Credentials { get; set; }
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
#if !NETSTANDARD
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
#if NETSTANDARD
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
		void SetWorkingDirectory(string path);
		string GetWorkingDirectory();
		long GetFileSize(string path, long defaultValue = -1);
		DateTime GetModifiedTime(string path);
		void SetModifiedTime(string path, DateTime date);



		// LISTING

		FtpListItem GetObjectInfo(string path, bool dateModified = false);
		FtpListItem[] GetListing();
		FtpListItem[] GetListing(string path);
		FtpListItem[] GetListing(string path, FtpListOption options);
		string[] GetNameListing();
		string[] GetNameListing(string path);


		// LOW LEVEL

		Stream OpenRead(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true);
		Stream OpenRead(string path, FtpDataType type, long restart, long fileLen);
		Stream OpenWrite(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true);
		Stream OpenWrite(string path, FtpDataType type, long fileLen);
		Stream OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true);
		Stream OpenAppend(string path, FtpDataType type, long fileLen);


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


		// HASH
		FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);


		// COMPARE
		FtpCompareResult CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto);

	}
}