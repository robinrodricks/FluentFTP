using FluentFTP.Model.Config;
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
	/// Interface for the AsyncFtpClient class.
	/// For detailed documentation of the methods, please see the FtpClient class or check the Wiki on the FluentFTP Github project.
	/// </summary>
	public interface IAsyncFtpClient : IDisposable, IBaseFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		// METHODS

		bool HasFeature(FtpCapability cap);
		void DisableUTF8();

		Task<FtpProfile> AutoConnect(CancellationToken token = default(CancellationToken));
		Task<List<FtpProfile>> AutoDetect(FtpAutoDetectConfig config, CancellationToken token = default(CancellationToken));
		Task Connect(CancellationToken token = default(CancellationToken));
		Task Connect(FtpProfile profile, CancellationToken token = default(CancellationToken));
		Task Connect(bool reConnect, CancellationToken token = default(CancellationToken));
		Task Disconnect(CancellationToken token = default(CancellationToken));
		Task<FtpReply> Execute(string command, CancellationToken token = default(CancellationToken));
		Task<List<string>> ExecuteDownloadText(string command, CancellationToken token = default(CancellationToken));
		Task<FtpReply> GetReply(CancellationToken token = default(CancellationToken));

		// MANAGEMENT

		Task DeleteFile(string path, CancellationToken token = default(CancellationToken));
		Task DeleteDirectory(string path, CancellationToken token = default(CancellationToken));
		Task DeleteDirectory(string path, FtpListOption options, CancellationToken token = default(CancellationToken));
		Task EmptyDirectory(string path, CancellationToken token = default(CancellationToken));
		Task EmptyDirectory(string path, FtpListOption options, CancellationToken token = default(CancellationToken));
		Task<bool> DirectoryExists(string path, CancellationToken token = default(CancellationToken));
		Task<bool> FileExists(string path, CancellationToken token = default(CancellationToken));
		Task<bool> CreateDirectory(string path, bool force, CancellationToken token = default(CancellationToken));
		Task<bool> CreateDirectory(string path, CancellationToken token = default(CancellationToken));
		Task Rename(string path, string dest, CancellationToken token = default(CancellationToken));
		Task<bool> MoveFile(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken));
		Task<bool> MoveDirectory(string path, string dest, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, CancellationToken token = default(CancellationToken));
		Task SetFilePermissions(string path, int permissions, CancellationToken token = default(CancellationToken));
		Task Chmod(string path, int permissions, CancellationToken token = default(CancellationToken));
		Task SetFilePermissions(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken));
		Task Chmod(string path, FtpPermission owner, FtpPermission group, FtpPermission other, CancellationToken token = default(CancellationToken));
		Task<FtpListItem> GetFilePermissions(string path, CancellationToken token = default(CancellationToken));
		Task<int> GetChmod(string path, CancellationToken token = default(CancellationToken));
		Task SetWorkingDirectory(string path, CancellationToken token = default(CancellationToken));
		Task<string> GetWorkingDirectory(CancellationToken token = default(CancellationToken));
		Task<long> GetFileSize(string path, long defaultValue = -1, CancellationToken token = default(CancellationToken));
		Task<DateTime> GetModifiedTime(string path, CancellationToken token = default(CancellationToken));

		Task SetModifiedTime(string path, DateTime date, CancellationToken token = default(CancellationToken));

		// LISTING

		Task<FtpListItem> GetObjectInfo(string path, bool dateModified = false, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListing(string path, FtpListOption options, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListing(string path, CancellationToken token = default(CancellationToken));
		Task<FtpListItem[]> GetListing(CancellationToken token = default(CancellationToken));
		Task<string[]> GetNameListing(string path, CancellationToken token = default(CancellationToken));
		Task<string[]> GetNameListing(CancellationToken token = default(CancellationToken));

#if NET50_OR_LATER
		IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, FtpListOption options, CancellationToken token = default, CancellationToken enumToken = default);
		IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, CancellationToken token = default, CancellationToken enumToken = default);
		IAsyncEnumerable<FtpListItem> GetListingEnumerable(CancellationToken token = default, CancellationToken enumToken = default);
#endif

		// LOW LEVEL

		Task<Stream> OpenRead(string path, FtpDataType type = FtpDataType.Binary, long restart = 0, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenRead(string path, FtpDataType type, long restart, long fileLen, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWrite(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenWrite(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken));
		Task<Stream> OpenAppend(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken));

		// HIGH LEVEL

		Task<List<FtpResult>> UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null, List<FtpRule> rules = null);
		Task<List<FtpResult>> UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null, List<FtpRule> rules = null);
		Task<List<FtpResult>> DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null, List<FtpRule> rules = null);

		Task<FtpStatus> UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<FtpStatus> DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<bool> DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken), long stopPosition = 0);
		Task<byte[]> DownloadBytes(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken), long stopPosition = 0);
		Task<byte[]> DownloadBytes(string remotePath, CancellationToken token = default(CancellationToken));

		Task<List<FtpResult>> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));
		Task<List<FtpResult>> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken));

		// HASH

		Task<FtpHash> GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken token = default(CancellationToken));

		// COMPARE

		Task<FtpCompareResult> CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto, CancellationToken token = default(CancellationToken));

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}