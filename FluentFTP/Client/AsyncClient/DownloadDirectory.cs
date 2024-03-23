using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

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
		/// <param name="verifyOptions">Sets verification behaviour and what to do if verification fails (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the file will be verified against the source using the verification methods specified by <see cref="FtpVerifyMethod"/> in the client config.
		/// <br/> If only <see cref="FtpVerify.OnlyVerify"/> is set then the return of this method depends on both a successful transfer &amp; verification.
		/// <br/> Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transferred. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localFolder));
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remoteFolder));
			}

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			LogFunction(nameof(DownloadDirectory), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// Fix #1121: check if dir is missing and throw FtpMissingObjectException
			if (!await DirectoryExists(remoteFolder, token)) {
				throw new FtpMissingObjectException("Cannot download non-existant directory: " + remoteFolder, null, remoteFolder, FtpObjectType.Directory);
			}

			// ensure the local dir exists
			localFolder.EnsureDirectory();

			// get all the files in the remote directory
			var listing = await GetListing(remoteFolder, FtpListOption.Recursive | FtpListOption.Size, token);

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

		/// <summary>
		/// Download all the listed files and folders from the main directory
		/// </summary>
		protected async Task DownloadServerFilesAsync(List<FtpResult> toDownload, FtpLocalExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, CancellationToken token) {

			LogFunction(nameof(DownloadServerFilesAsync), new object[] { toDownload.Count + " files" });

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
						var ok = await DownloadFileToFileAsync(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress, token, metaProgress);
						result.IsSuccess = ok.IsSuccess();
						result.IsSkipped = ok == FtpStatus.Skipped;
					}
					catch (Exception ex) {

						LogWithPrefix(FtpTraceLevel.Warn, "File failed to download: " + result.RemotePath);

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

	}
}
