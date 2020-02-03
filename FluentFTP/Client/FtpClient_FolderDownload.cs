using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
using FluentFTP.Rules;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;

#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif

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
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc("DownloadDirectory", new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

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

			// loop thru each file and transfer it
			var toDownload = GetFilesToDownload(localFolder, remoteFolder, rules, results, listing, shouldExist);
			DownloadServerFiles(toDownload, existsMode, verifyOptions, progress);

			// delete the extra local files if in mirror mode
			DeleteExtraLocalFiles(localFolder, mode, shouldExist);

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
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> DownloadDirectoryAsync(string localFolder, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc("DownloadDirectoryAsync", new object[] { localFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			localFolder = localFolder.EnsurePostfix(Path.DirectorySeparatorChar.ToString());

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the dir does not exist, fail fast
			if (!await DirectoryExistsAsync(remoteFolder, token)) {
				return results;
			}

			// ensure the local dir exists
			localFolder.EnsureDirectory();

			// get all the files in the remote directory
			var listing = await GetListingAsync(remoteFolder, FtpListOption.Recursive | FtpListOption.Size, token);

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// loop thru each file and transfer it
			var toDownload = GetFilesToDownload(localFolder, remoteFolder, rules, results, listing, shouldExist);
			await DownloadServerFilesAsync(toDownload, existsMode, verifyOptions, progress, token);

			// delete the extra local files if in mirror mode
			DeleteExtraLocalFiles(localFolder, mode, shouldExist);

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
				var relativePath = remoteFile.FullName.Replace(remoteFolder, "").Replace('/', Path.DirectorySeparatorChar);
				var localFile = Path.Combine(localFolder, relativePath);

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
				if (remoteFile.Type == FtpFileSystemObjectType.File ||
					remoteFile.Type == FtpFileSystemObjectType.Directory) {


					// record the file
					results.Add(result);

					// if the file passes all rules
					if (rules != null && rules.Count > 0) {
						var passes = FtpRule.IsAllAllowed(rules, remoteFile);
						if (!passes) {

							LogStatus(FtpTraceLevel.Info, "Skipped file due to rule: " + result.RemotePath);

							// mark that the file was skipped due to a rule
							result.IsSkipped = true;
							result.IsSkippedByRule = true;

							// skip downloading the file
							continue;
						}
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

			LogFunc("DownloadServerFiles", new object[] { toDownload.Count + " files" });

			foreach (var result in toDownload) {

				if (result.Type == FtpFileSystemObjectType.File) {

					// absorb errors
					try {

						// download the file
						var transferred = this.DownloadFile(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress);
						result.IsSuccess = true;
						result.IsSkipped = !transferred;
					}
					catch (Exception ex) {

						LogStatus(FtpTraceLevel.Warn, "File failed to download: " + result.RemotePath);

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
				else if (result.Type == FtpFileSystemObjectType.Directory) {

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

			LogFunc("DownloadServerFilesAsync", new object[] { toDownload.Count + " files" });

			foreach (var result in toDownload) {

				if (result.Type == FtpFileSystemObjectType.File) {

					// absorb errors
					try {

						// download the file
						var transferred = await this.DownloadFileAsync(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress, token);
						result.IsSuccess = true;
						result.IsSkipped = !transferred;
					}
					catch (Exception ex) {

						LogStatus(FtpTraceLevel.Warn, "File failed to download: " + result.RemotePath);

						// mark that the file failed to download
						result.IsFailed = true;
						result.Exception = ex;
					}

				}
				else if (result.Type == FtpFileSystemObjectType.Directory) {

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
		private void DeleteExtraLocalFiles(string localFolder, FtpFolderSyncMode mode, Dictionary<string, bool> shouldExist) {
			if (mode == FtpFolderSyncMode.Mirror) {

				LogFunc("DeleteExtraLocalFiles");

				// get all the local files
				var localListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

				// delete files that are not in listed in shouldExist
				foreach (var existingLocalFile in localListing) {

					if (!shouldExist.ContainsKey(existingLocalFile.ToLower())) {

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
}