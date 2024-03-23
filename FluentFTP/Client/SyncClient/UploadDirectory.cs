using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

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
		/// <param name="verifyOptions"> Sets verification type and what to do if verification fails (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
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
		public List<FtpResult> UploadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
			FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

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

			LogFunction(nameof(UploadDirectory), new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

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

			// loop through each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToUpload(localFolder, remoteFolder, rules, results, dirListing);
			CreateSubDirectories(this, dirsToUpload);

			// get all the files in the local directory
			var fileListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

			// loop through each file and transfer it
			var filesToUpload = GetFilesToUpload(localFolder, remoteFolder, rules, results, shouldExist, fileListing);
			UploadDirectoryFiles(filesToUpload, existsMode, verifyOptions, progress, remoteListing);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			DeleteExtraServerFiles(mode, remoteFolder, shouldExist, remoteListing, rules);

			return results;
		}

		/// <summary>
		/// Create all the sub directories within the main directory
		/// </summary>
		protected void CreateSubDirectories(FtpClient client, List<FtpResult> dirsToUpload) {
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

		/// <summary>
		/// Upload all the files within the main directory
		/// </summary>
		protected void UploadDirectoryFiles(List<FtpResult> filesToUpload, FtpRemoteExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpListItem[] remoteListing) {

			LogFunction(nameof(UploadDirectoryFiles), new object[] { filesToUpload.Count + " files" });

			int r = -1;
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
					var transferred = UploadFileFromFile(result.LocalPath, result.RemotePath, false, existsModeToUse, false, false, verifyOptions, progress, metaProgress);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex) {

					LogWithPrefix(FtpTraceLevel.Warn, "File failed to upload: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

		/// <summary>
		/// Delete the extra remote files if in mirror mode and the directory was pre-existing
		/// </summary>
		protected void DeleteExtraServerFiles(FtpFolderSyncMode mode, string remoteFolder, Dictionary<string, bool> shouldExist, FtpListItem[] remoteListing, List<FtpRule> rules) {
			if (mode == FtpFolderSyncMode.Mirror && remoteListing != null) {

				LogFunction(nameof(DeleteExtraServerFiles));

				// delete files that are not in listed in shouldExist
				foreach (var existingServerFile in remoteListing) {

					if (existingServerFile.Type == FtpObjectType.File) {

						if (!shouldExist.ContainsKey(existingServerFile.FullName.ToLower())) {

							// only delete the remote file if its permitted by the configuration
							if (CanDeleteRemoteFile(rules, existingServerFile)) {
								LogWithPrefix(FtpTraceLevel.Info, "Delete extra file from server: " + existingServerFile.FullName);

								// delete the file from the server
								try {
									DeleteFile(existingServerFile.FullName);
								}
								catch {
								}
							}
						}

					}

				}

			}
		}

	}
}
