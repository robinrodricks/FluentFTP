using System;
using System.Collections.Generic;
using System.IO;

using FluentFTP.Model.Functions;
using FluentFTP.Rules;

namespace FluentFTP {

	/// <summary>
	/// Interface for the FtpClient class.
	/// For detailed documentation of the methods, please see the FtpClient class or check the Wiki on the FluentFTP Github project.
	/// </summary>
	public interface IFtpClient : IDisposable, IBaseFtpClient {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		// METHODS

		bool HasFeature(FtpCapability cap);
		void DisableUTF8();

		List<FtpProfile> AutoDetect(FtpAutoDetectConfig config);
		List<FtpProfile> AutoDetect(bool firstOnly, bool cloneConnection = true);
		FtpProfile AutoConnect();
		void Connect();
		void Connect(FtpProfile profile);
		void Connect(bool reConnect);
		void Disconnect();
		FtpReply Execute(string command);
		List<string> ExecuteDownloadText(string command);
		FtpReply GetReply();

		// MANAGEMENT

		void DeleteFile(string path);
		void DeleteDirectory(string path);
		void DeleteDirectory(string path, FtpListOption options);
		void EmptyDirectory(string path);
		void EmptyDirectory(string path, FtpListOption options);
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

		List<FtpResult> UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null);
		List<FtpResult> UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null);
		List<FtpResult> DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null);

		FtpStatus UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		FtpStatus UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);
		FtpStatus UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null);

		FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null);
		bool DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null, long stopPosition = 0);
		bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null, long stopPosition = 0);
		bool DownloadUriBytes(out byte[] outBytes, string uri, Action<FtpProgress> progress = null);

		List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);
		List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null);

		// HASH
		FtpHash GetChecksum(string path, FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE);

		// COMPARE
		FtpCompareResult CompareFile(string localPath, string remotePath, FtpCompareOption options = FtpCompareOption.Auto);

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}
}