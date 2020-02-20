using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
#if (CORE || NETFX)
using System.Threading;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		/// <summary>
		/// Uploads the specified directory onto the server.
		/// In Mirror mode, we will upload missing files, and delete any extra files from the server that are not present on disk. This is very useful when publishing an exact copy of a local folder onto an FTP server.
		/// In Update mode, we will only upload missing files and preserve any extra files on the server. This is useful when you want to simply upload missing files to a server.
		/// Only uploads the files and folders matching all the rules provided, if any.
		/// All exceptions during uploading are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="localFolder">The full path of the local folder on disk that you want to upload. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteFolder">The full path of the remote FTP folder to upload into. It is created if it does not exist.</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the upload or restart the upload?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(UploadDirectory), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the dir does not exist, fail fast
			if (!Directory.Exists(localFolder)) {
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!DirectoryExists(remoteFolder)) {
				CreateDirectory(remoteFolder);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = Directory.GetDirectories(localFolder, "*.*", SearchOption.AllDirectories);

			// get all the already existing files
			var remoteListing = checkFileExistence ? GetListing(remoteFolder, FtpListOption.Recursive) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToUpload(localFolder, remoteFolder, rules, results, dirListing);
			CreateSubDirectories(this, dirsToUpload);

			// get all the files in the local directory
			var fileListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToUpload(localFolder, remoteFolder, rules, results, shouldExist, fileListing);
			UploadDirectoryFiles(filesToUpload, existsMode, verifyOptions, progress, remoteListing);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}

#if ASYNC
		/// <summary>
		/// Uploads the specified directory onto the server.
		/// In Mirror mode, we will upload missing files, and delete any extra files from the server that are not present on disk. This is very useful when publishing an exact copy of a local folder onto an FTP server.
		/// In Update mode, we will only upload missing files and preserve any extra files on the server. This is useful when you want to simply upload missing files to a server.
		/// Only uploads the files and folders matching all the rules provided, if any.
		/// All exceptions during uploading are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="localFolder">The full path of the local folder on disk that you want to upload. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteFolder">The full path of the remote FTP folder to upload into. It is created if it does not exist.</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the upload or restart the upload?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> UploadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(UploadDirectoryAsync), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the dir does not exist, fail fast
			if (!Directory.Exists(localFolder)) {
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!await DirectoryExistsAsync(remoteFolder, token)) {
				await CreateDirectoryAsync(remoteFolder, token);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = Directory.GetDirectories(localFolder, "*.*", SearchOption.AllDirectories);

			// get all the already existing files
			var remoteListing = checkFileExistence ? GetListing(remoteFolder, FtpListOption.Recursive) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToUpload(localFolder, remoteFolder, rules, results, dirListing);
			await CreateSubDirectoriesAsync(this, dirsToUpload, token);

			// get all the files in the local directory
			var fileListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToUpload(localFolder, remoteFolder, rules, results, shouldExist, fileListing);
			await UploadDirectoryFilesAsync(filesToUpload, existsMode, verifyOptions, progress, remoteListing, token);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			await DeleteExtraServerFilesAsync(mode, shouldExist, remoteListing, token);

			return results;
		}
#endif

		/// <summary>
		/// Get a list of all the sub directories that need to be created within the main directory
		/// </summary>
		private List<FtpResult> GetSubDirectoriesToUpload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, string[] dirListing) {

			var dirsToUpload = new List<FtpResult>();

			foreach (var localFile in dirListing) {

				// calculate the local path
				var relativePath = localFile.Replace(localFolder, "").EnsurePostfix(Path.DirectorySeparatorChar.ToString());
				var remoteFile = remoteFolder + relativePath.Replace('\\', '/');

				// create the result object
				var result = new FtpResult() {
					Type = FtpFileSystemObjectType.Directory,
					Size = 0,
					Name = Path.GetDirectoryName(localFile),
					RemotePath = remoteFile,
					LocalPath = localFile,
					IsDownload = false,
				};

				// record the folder
				results.Add(result);

				// skip uploading the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}
				
				dirsToUpload.Add(result);
			}

			return dirsToUpload;
		}

		/// <summary>
		/// Create all the sub directories within the main directory
		/// </summary>
		private void CreateSubDirectories(FtpClient client, List<FtpResult> dirsToUpload) {
			foreach (var result in dirsToUpload) {

				// absorb errors
				try {

					// create directory on the server
					// to ensure we upload the blank remote dirs as well
					if (client.CreateDirectory(result.RemotePath)) {
						result.IsSuccess = true;
						result.IsSkipped = false;
					}
					else {
						result.IsSkipped = true;
					}

				}
				catch (Exception ex) {

					// mark that the folder failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}
		}

#if ASYNC
		/// <summary>
		/// Create all the sub directories within the main directory
		/// </summary>
		private async Task CreateSubDirectoriesAsync(FtpClient client, List<FtpResult> dirsToUpload, CancellationToken token) {
			foreach (var result in dirsToUpload) {

				// absorb errors
				try {

					// create directory on the server
					// to ensure we upload the blank remote dirs as well
					if (await client.CreateDirectoryAsync(result.RemotePath, token)) {
						result.IsSuccess = true;
						result.IsSkipped = false;
					}
					else {
						result.IsSkipped = true;
					}

				}
				catch (Exception ex) {

					// mark that the folder failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}
		}
#endif

		/// <summary>
		/// Get a list of all the files that need to be uploaded within the main directory
		/// </summary>
		private List<FtpResult> GetFilesToUpload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing) {

			var filesToUpload = new List<FtpResult>();

			foreach (var localFile in fileListing) {

				// calculate the local path
				var relativePath = localFile.Replace(localFolder, "").Replace(Path.DirectorySeparatorChar, '/');
				var remoteFile = remoteFolder + relativePath.Replace('\\', '/');

				// create the result object
				var result = new FtpResult() {
					Type = FtpFileSystemObjectType.File,
					Size = new FileInfo(localFile).Length,
					Name = Path.GetFileName(localFile),
					RemotePath = remoteFile,
					LocalPath = localFile
				};

				// record the file
				results.Add(result);

				// skip uploading the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}
				
				// record that this file should exist
				shouldExist.Add(remoteFile.ToLower(), true);

				// absorb errors
				filesToUpload.Add(result);
			}

			return filesToUpload;
		}

		/// <summary>
		/// Upload all the files within the main directory
		/// </summary>
		private void UploadDirectoryFiles(List<FtpResult> filesToUpload, FtpRemoteExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpListItem[] remoteListing) {

			LogFunc(nameof(UploadDirectoryFiles), new object[] { filesToUpload.Count + " files" });

			int r = -1;
			foreach (var result in filesToUpload) {
				r++;

				// absorb errors
				try {

					// skip uploading if the file already exists on the server
					FtpRemoteExists existsModeToUse;
					if (!CanUploadFile(result, remoteListing, existsMode, out existsModeToUse)){
						continue;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToUpload.Count, r);

					// upload the file
					var transferred = UploadFileFromFile(result.LocalPath, result.RemotePath, false, existsModeToUse, false, false, verifyOptions, progress, metaProgress);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogStatus(FtpTraceLevel.Warn, "File failed to upload: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

		/// <summary>
		/// Check if the file is cleared to be uploaded, taking its existance/filesize and existsMode options into account.
		/// </summary>
		private bool CanUploadFile(FtpResult result, FtpListItem[] remoteListing, FtpRemoteExists existsMode, out FtpRemoteExists existsModeToUse) {

			// check if the file already exists on the server
			existsModeToUse = existsMode;
			var fileExists = FtpExtensions.FileExistsInListing(remoteListing, result.RemotePath);

			// if we want to skip uploaded files and the file already exists, mark its skipped
			if (existsMode == FtpRemoteExists.Skip && fileExists) {

				LogStatus(FtpTraceLevel.Info, "Skipped file that already exists: " + result.LocalPath);

				result.IsSuccess = true;
				result.IsSkipped = true;
				return false;
			}

			// in any mode if the file does not exist, mark that exists check is not required
			if (!fileExists) {
				existsModeToUse = existsMode == FtpRemoteExists.Append ? FtpRemoteExists.AppendNoCheck : FtpRemoteExists.NoCheck;
			}
			return true;
		}

#if ASYNC
		/// <summary>
		/// Upload all the files within the main directory
		/// </summary>
		private async Task UploadDirectoryFilesAsync(List<FtpResult> filesToUpload, FtpRemoteExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, FtpListItem[] remoteListing, CancellationToken token) {

			LogFunc(nameof(UploadDirectoryFilesAsync), new object[] { filesToUpload.Count + " files" });

			var r = -1;
			foreach (var result in filesToUpload) {
				r++;

				// absorb errors
				try {

					// skip uploading if the file already exists on the server
					FtpRemoteExists existsModeToUse;
					if (!CanUploadFile(result, remoteListing, existsMode, out existsModeToUse)) {
						continue;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToUpload.Count, r);

					// upload the file
					var transferred = await UploadFileFromFileAsync(result.LocalPath, result.RemotePath, false, existsModeToUse, false, false, verifyOptions, token, progress, metaProgress);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogStatus(FtpTraceLevel.Warn, "File failed to upload: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}
#endif

		/// <summary>
		/// Delete the extra remote files if in mirror mode and the directory was pre-existing
		/// </summary>
		private void DeleteExtraServerFiles(FtpFolderSyncMode mode, Dictionary<string, bool> shouldExist, FtpListItem[] remoteListing) {
			if (mode == FtpFolderSyncMode.Mirror && remoteListing != null) {

				LogFunc(nameof(DeleteExtraServerFiles));

				// delete files that are not in listed in shouldExist
				foreach (var existingServerFile in remoteListing) {

					if (existingServerFile.Type == FtpFileSystemObjectType.File) {

						if (!shouldExist.ContainsKey(existingServerFile.FullName.ToLower())) {

							LogStatus(FtpTraceLevel.Info, "Delete extra file from server: " + existingServerFile.FullName);

							// delete the file from the server
							try {
								DeleteFile(existingServerFile.FullName);
							}
							catch (Exception ex) { }

						}

					}

				}

			}
		}

#if ASYNC
		/// <summary>
		/// Delete the extra remote files if in mirror mode and the directory was pre-existing
		/// </summary>
		private async Task DeleteExtraServerFilesAsync(FtpFolderSyncMode mode, Dictionary<string, bool> shouldExist, FtpListItem[] remoteListing, CancellationToken token) {
			if (mode == FtpFolderSyncMode.Mirror && remoteListing != null) {

				LogFunc(nameof(DeleteExtraServerFilesAsync));

				// delete files that are not in listed in shouldExist
				foreach (var existingServerFile in remoteListing) {

					if (existingServerFile.Type == FtpFileSystemObjectType.File) {

						if (!shouldExist.ContainsKey(existingServerFile.FullName.ToLower())) {

							LogStatus(FtpTraceLevel.Info, "Delete extra file from server: " + existingServerFile.FullName);

							// delete the file from the server
							try {
								await DeleteFileAsync(existingServerFile.FullName, token);
							}
							catch (Exception ex) { }

						}

					}

				}

			}
		}
#endif


	}
}