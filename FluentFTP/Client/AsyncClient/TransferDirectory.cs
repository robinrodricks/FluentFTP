using System;
using System.Collections.Generic;
using System.Linq;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Transfer the specified directory from the source FTP Server onto the remote FTP Server asynchronously using the FXP protocol.
		/// You will need to create a valid connection to your remote FTP Server before calling this method.
		/// In Update mode, we will only transfer missing files and preserve any extra files on the remote FTP Server. This is useful when you want to simply transfer missing files from an FTP directory.
		/// Currently Mirror mode is not implemented.
		/// Only transfers the files and folders matching all the rules provided, if any.
		/// All exceptions during transfer are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="sourceFolder">The full or relative path to the folder on the source FTP Server. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="remoteFolder">The full or relative path to destination folder on the remote FTP Server</param>
		/// <param name="mode">Only Update mode is currently implemented</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets verification behaviour and what to do if verification fails (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
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
		public async Task<List<FtpResult>> TransferDirectory(string sourceFolder, AsyncFtpClient remoteClient, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (sourceFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(sourceFolder));
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remoteFolder));
			}

			// cleanup the FTP paths
			sourceFolder = sourceFolder.GetFtpPath().EnsurePostfix("/");
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			LogFunction(nameof(TransferDirectory), new object[] { sourceFolder, remoteClient, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// if the source dir does not exist, fail fast
			if (!await DirectoryExists(sourceFolder, token)) {
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!await remoteClient.DirectoryExists(remoteFolder, token)) {
				await remoteClient.CreateDirectory(remoteFolder, token);
				checkFileExistence = false;
			}

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = (await GetListing(sourceFolder, FtpListOption.Recursive, token)).Where(x => x.Type == FtpObjectType.Directory).Select(x => x.FullName).ToArray();

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			// get all the already existing files
			var remoteListing = checkFileExistence ? await remoteClient.GetListing(remoteFolder, FtpListOption.Recursive, token) : null;

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			// loop through each folder and ensure it exists #1
			var dirsToUpload = GetSubDirectoriesToTransfer(sourceFolder, remoteFolder, rules, results, dirListing);

			// break if task is cancelled
			token.ThrowIfCancellationRequested();

			/*-------------------------------------------------------------------------------------/
			 *   Cancelling after this point would leave the FTP server in an inconsistent state   *
			 *-------------------------------------------------------------------------------------*/

			// loop through each folder and ensure it exists #2
			await CreateSubDirectories(remoteClient, dirsToUpload, token);

			// get all the files in the local directory
			var fileListing = (await GetListing(sourceFolder, FtpListOption.Recursive, token)).Where(x => x.Type == FtpObjectType.File).Select(x => x.FullName).ToArray();

			// loop through each file and transfer it
			var filesToUpload = await GetFilesToTransfer(sourceFolder, remoteFolder, rules, results, shouldExist, fileListing, token);
			await TransferServerFiles(filesToUpload, remoteClient, existsMode, verifyOptions, progress, remoteListing, token);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			// DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}

		/// <summary>
		/// Make a list of files to transfer
		/// </summary>
		protected async Task<List<FtpResult>> GetFilesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing, CancellationToken token = default) {

			var filesToTransfer = new List<FtpResult>();

			foreach (var sourceFile in fileListing) {

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult {
					Type = FtpObjectType.File,
					Size = await GetFileSize(sourceFile, token: token),
					Name = sourceFile.GetFtpFileName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile
				};

				// record the file
				results.Add(result);

				// skip transferring the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				// record that this file should exist
				shouldExist.Add(remoteFile.ToLowerInvariant(), true);

				// absorb errors
				filesToTransfer.Add(result);
			}

			return filesToTransfer;
		}

		/// <summary>
		/// Transfer the files
		/// </summary>
		protected async Task TransferServerFiles(List<FtpResult> filesToTransfer, AsyncFtpClient remoteClient, FtpRemoteExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, FtpListItem[] remoteListing, CancellationToken token) {

			LogFunction(nameof(TransferServerFiles), new object[] { filesToTransfer.Count + " files" });

			int r = -1;
			foreach (var result in filesToTransfer) {
				r++;

				// absorb errors
				try {

					// skip uploading if the file already exists on the server
					FtpRemoteExists existsModeToUse;
					if (!CanUploadFile(result, remoteListing, existsMode, out existsModeToUse)) {
						continue;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToTransfer.Count, r);

					// transfer the file
					var transferred = await TransferFile(result.LocalPath, remoteClient, result.RemotePath, false, existsModeToUse, verifyOptions, progress, metaProgress, token);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogWithPrefix(FtpTraceLevel.Warn, "File failed to transfer: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

	}
}
