using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

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
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None,
			FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null) {

			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (localDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localDir));
			}

			LogFunction(nameof(DownloadFiles), new object[] { localDir, remotePaths, existsMode, verifyOptions });

			var errorEncountered = false;
			var successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			// per remote file
			var r = -1;
			foreach (var remotePath in remotePaths) {
				r++;

				// calc local path
				var localPath = localDir + remotePath.GetFtpFileName();

				// create meta progress to store the file progress
				var metaProgress = new FtpProgress(remotePaths.Count(), r);

				// try to download it
				try {
					var ok = DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, progress, metaProgress);
					if (ok.IsSuccess()) {
						successfulDownloads.Add(localPath);
					}
					else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				}
				catch (Exception ex) {
					LogWithPrefix(FtpTraceLevel.Error, "Failed to download " + remotePath, ex);
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

			return successfulDownloads.Count;
		}

		/// <summary>
		/// Remove successfully downloaded files
		/// </summary>
		/// <param name="localFiles"></param>
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
