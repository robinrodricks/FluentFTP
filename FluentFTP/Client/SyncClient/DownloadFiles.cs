using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using FluentFTP.Rules;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions"> Sets verification type and what to do if verification fails (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <param name="rules">Only files that pass all these rules are downloaded, and the files that don't pass are skipped.</param>
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
		public List<FtpResult> DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None,
			FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null, List<FtpRule> rules = null) {

			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (localDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localDir));
			}

			LogFunction(nameof(DownloadFiles), new object[] { localDir, remotePaths, existsMode, verifyOptions });


			// result vars
			var errorEncountered = false;
			var successfulDownloads = new List<string>();
			var results = new List<FtpResult>();
			var shouldExist = new Dictionary<string, bool>(); // paths of the files that should exist (lowercase for CI checks)

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			// check which files should be downloaded or filtered out based on rules
			var filesToDownload = GetFilesToDownload2(localDir, remotePaths, rules, results, shouldExist);

			// per remote file
			var r = -1;
			foreach (var result in filesToDownload) {
				r++;

				// create meta progress to store the file progress
				var metaProgress = new FtpProgress(remotePaths.Count(), r);

				// try to download it
				try {
					var ok = DownloadFileToFile(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress, metaProgress);
					
					// mark that the file succeeded
					result.IsSuccess = ok.IsSuccess();
					result.IsSkipped = ok == FtpStatus.Skipped;

					if (ok.IsSuccess()) {
						successfulDownloads.Add(result.LocalPath);
					}
					else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				}
				catch (Exception ex) {
				
					// mark that the file failed
					result.IsFailed = true;
					result.Exception = ex;

					LogWithPrefix(FtpTraceLevel.Error, "Failed to download " + result.RemotePath, ex);

					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulDownloads(successfulDownloads);
						}

						throw new FtpException("An error occurred downloading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					PurgeSuccessfulDownloads(successfulDownloads);
					successfulDownloads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred downloading one or more files.  Refer to trace output if available.");
				}
			}

			return results;
		}

		/// <summary>
		/// Remove successfully downloaded files.
		/// </summary>
		protected void PurgeSuccessfulDownloads(IEnumerable<string> localFiles) {
			foreach (var localFile in localFiles) {
				// absorb any errors because we don't want this to throw more errors!
				try {
					File.Delete(localFile);
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Warn, "FtpClient : Exception caught and discarded while attempting to delete file '" + localFile + "'", ex);
				}
			}
		}

	}
}
