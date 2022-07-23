using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;
#if (CORE || NETFX)
using System.Threading;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;
#endif
using System.Linq;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		/// <summary>
		/// Downloads the specified directory onto the local file system.
		/// In Mirror mode, we will download missing files, and delete any extra files from disk that are not present on the server. This is very useful when creating an exact local backup of an FTP directory.
		/// In Update mode, we will only download missing files and preserve any extra files on disk. This is useful when you want to simply download missing files from an FTP directory.
		/// Only downloads the files and folders matching all the rules provided, if any.
		/// All exceptions during downloading are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="localFolder">The full path of the local folder on disk to download into. It is created if it does not exist.</param>
		/// <param name="remoteFolder">The full path of the remote FTP folder that you want to download. If it does not exist, an empty result list is returned.</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transferred. Never returns null.
		/// </returns>
		public List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			LogFunc(nameof(DownloadDirectory), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// if the dir does not exist, fail fast
			if (!DirectoryExists(remoteFolder)) {
				return results;
			}

			// ensure the local dir exists
			localFolder.EnsureDirectory();

			// get all the files in the remote directory
			var listing = GetListing(remoteFolder, FtpListOption.Recursive | FtpListOption.Size);

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// loop through each file and transfer it
			var toDownload = GetFilesToDownload(localFolder, remoteFolder, rules, results, listing, shouldExist);
			DownloadServerFiles(toDownload, existsMode, verifyOptions, progress);

			// delete the extra local files if in mirror mode
			DeleteExtraLocalFiles(localFolder, mode, shouldExist, rules);

			return results;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified directory onto the local file system.
		/// In Mirror mode, we will download missing files, and delete any extra files from disk that are not present on the server. This is very useful when creating an exact local backup of an FTP directory.
		/// In Update mode, we will only download missing files and preserve any extra files on disk. This is useful when you want to simply download missing files from an FTP directory.
		/// Only downloads the files and folders matching all the rules provided, if any.
		/// All exceptions during downloading are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="localFolder">The full path of the local folder on disk to download into. It is created if it does not exist.</param>
		/// <param name="remoteFolder">The full path of the remote FTP folder that you want to download. If it does not exist, an empty result list is returned.</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
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
		/// Returns a blank list if nothing was transferred. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> DownloadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			LogFunc(nameof(DownloadDirectoryAsync), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// if the dir does not exist, fail fast
			if (!await DirectoryExistsAsync(remoteFolder, token)) {
				return results;
			}

			// ensure the local dir exists
			localFolder.EnsureDirectory();

			// get all the files in the remote directory
			var listing = await GetListingAsync(remoteFolder, FtpListOption.Recursive | FtpListOption.Size, token);

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// loop through each file and transfer it #1
			var toDownload = GetFilesToDownload(localFolder, remoteFolder, rules, results, listing, shouldExist);

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			/*-------------------------------------------------------------------------------------/
			 *   Cancelling after this point would leave the FTP server in an inconsistent state   *
			 *-------------------------------------------------------------------------------------*/

			// loop through each file and transfer it #2
			await DownloadServerFilesAsync(toDownload, existsMode, verifyOptions, progress, token);

			// delete the extra local files if in mirror mode
			DeleteExtraLocalFiles(localFolder, mode, shouldExist, rules);

			return results;
		}
#endif

		/// <summary>
		/// Get a list of all the files and folders that need to be downloaded
		/// </summary>
		private List<FtpResult> GetFilesToDownload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, FtpListItem[] listing, Dictionary<string, bool> shouldExist) {

			var toDownload = new List<FtpResult>();

			foreach (var remoteFile in listing) {

				// calculate the local path
				var relativePath = remoteFile.FullName.EnsurePrefix("/").RemovePrefix(remoteFolder).Replace('/', Path.DirectorySeparatorChar);
				var localFile = localFolder.CombineLocalPath(relativePath);

				// create the result object
				var result = new FtpResult() {
					Type = remoteFile.Type,
					Size = remoteFile.Size,
					Name = remoteFile.Name,
					RemotePath = remoteFile.FullName,
					LocalPath = localFile,
					IsDownload = true,
				};

				// only files and folders are processed
				if (remoteFile.Type == FtpObjectType.File ||
					remoteFile.Type == FtpObjectType.Directory) {


					// record the file
					results.Add(result);

					// skip downloading the file if it does not pass all the rules
					if (!FilePassesRules(result, rules, false, remoteFile)) {
						continue;
					}

					// record that this file/folder should exist
					shouldExist.Add(localFile.ToLower(), true);

					// only files are processed
					toDownload.Add(result);


				}
			}

			return toDownload;
		}

		/// <summary>
		/// Download all the listed files and folders from the main directory
		/// </summary>
		private void DownloadServerFiles(List<FtpResult> toDownload, FtpLocalExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress) {

			LogFunc(nameof(DownloadServerFiles), new object[] { toDownload.Count + " files" });

			// per object to download
			var r = -1;
			foreach (var result in toDownload) {
				r++;

				if (result.Type == FtpObjectType.File) {

					// absorb errors
					try {

						// create meta progress to store the file progress
						var metaProgress = new FtpProgress(toDownload.Count, r);

						// download the file
						var transferred = DownloadFileToFile(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress, metaProgress);
						result.IsSuccess = transferred.IsSuccess();
						result.IsSkipped = transferred == FtpStatus.Skipped;
					}
					catch (Exception ex) {

						LogStatus(FtpTraceLevel.Warn, "File failed to download: " + result.RemotePath);

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
				else if (result.Type == FtpObjectType.Directory) {

					// absorb errors
					try {

						// create directory on local filesystem
						// to ensure we download the blank remote dirs as well
						var created = result.LocalPath.EnsureDirectory();
						result.IsSuccess = true;
						result.IsSkipped = !created;

					}
					catch (Exception ex) {

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
			}

		}

#if ASYNC
		/// <summary>
		/// Download all the listed files and folders from the main directory
		/// </summary>
		private async Task DownloadServerFilesAsync(List<FtpResult> toDownload, FtpLocalExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, CancellationToken token) {

			LogFunc(nameof(DownloadServerFilesAsync), new object[] { toDownload.Count + " files" });

			// per object to download
			var r = -1;
			foreach (var result in toDownload) {
				r++;

				if (result.Type == FtpObjectType.File) {

					// absorb errors
					try {

						// create meta progress to store the file progress
						var metaProgress = new FtpProgress(toDownload.Count, r);

						// download the file
						var transferred = await DownloadFileToFileAsync(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress, token, metaProgress);
						result.IsSuccess = transferred.IsSuccess();
						result.IsSkipped = transferred == FtpStatus.Skipped;
					}
					catch (Exception ex) {

						LogStatus(FtpTraceLevel.Warn, "File failed to download: " + result.RemotePath);

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
				else if (result.Type == FtpObjectType.Directory) {

					// absorb errors
					try {

						// create directory on local filesystem
						// to ensure we download the blank remote dirs as well
						var created = result.LocalPath.EnsureDirectory();
						result.IsSuccess = true;
						result.IsSkipped = !created;

					}
					catch (Exception ex) {

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
			}

		}
#endif

		/// <summary>
		/// Delete the extra local files if in mirror mode
		/// </summary>
		private void DeleteExtraLocalFiles(string localFolder, FtpFolderSyncMode mode, Dictionary<string, bool> shouldExist, List<FtpRule> rules) {
			if (mode == FtpFolderSyncMode.Mirror) {

				LogFunc(nameof(DeleteExtraLocalFiles));

				// get all the local files
				var localListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

				// delete files that are not in listed in shouldExist
				foreach (var existingLocalFile in localListing) {

					if (!shouldExist.ContainsKey(existingLocalFile.ToLower())) {

						// only delete the local file if its permitted by the configuration
						if (CanDeleteLocalFile(rules, existingLocalFile)) {
							LogStatus(FtpTraceLevel.Info, "Delete extra file from disk: " + existingLocalFile);

							// delete the file from disk
							try {
								File.Delete(existingLocalFile);
							}
							catch (Exception ex) { }
						}
					}
				}
			}
		}

		/// <summary>
		/// Check if the local file can be deleted, based on the DownloadDirectoryDeleteExcluded property
		/// </summary>
		private bool CanDeleteLocalFile(List<FtpRule> rules, string existingLocalFile) {

			// if we should not delete excluded files
			if (!DownloadDirectoryDeleteExcluded && !rules.IsBlank()) {

				// create the result object to validate rules to ensure that file from excluded
				// directories are not deleted on the local filesystem
				var result = new FtpResult() {
					Type = FtpObjectType.File,
					Size = 0,
					Name = Path.GetFileName(existingLocalFile),
					LocalPath = existingLocalFile,
					IsDownload = false,
				};

				// check if the file passes the rules
				if (FilePassesRules(result, rules, true)) {
					// delete the file because it is included
					return true;
				}
				else {
					// do not delete the file because it is excluded
					return false;
				}
			}

			// always delete the file whether its included or excluded by the rules
			return true;
		}

	}
}