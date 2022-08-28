using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null) {

			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (remoteDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteDir");
			}

			remoteDir = remoteDir.GetFtpPath();

			LogFunc(nameof(UploadFiles), new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

			//int count = 0;
			var errorEncountered = false;
			var successfulUploads = new List<string>();

			remoteDir = GetAbsolutePath(remoteDir); // a dir is just like a path

			//flag to determine if existence checks are required
			var checkFileExistence = true;

			// create remote dir if wanted
			if (createRemoteDir) {
				if (!DirectoryExists(remoteDir)) {
					CreateDirectory(remoteDir);
					checkFileExistence = false;
				}
			}

			// get all the already existing files
			var existingFiles = checkFileExistence ? GetNameListing(GetAbsoluteDir(remoteDir)) : new string[0];

			// per local file
			var r = -1;
			foreach (var localPath in localPaths) {
				r++;

				// calc remote path
				var fileName = Path.GetFileName(localPath);
				var remoteFilePath = "";

				remoteFilePath = GetAbsoluteFilePath(remoteDir, fileName);

				// create meta progress to store the file progress
				var metaProgress = new FtpProgress(localPaths.Count(), r);

				// try to upload it
				try {
					var ok = UploadFileFromFile(localPath, remoteFilePath, false, existsMode, FileListings.FileExistsInNameListing(existingFiles, remoteFilePath), true, verifyOptions, progress, metaProgress);
					if (ok.IsSuccess()) {
						successfulUploads.Add(remoteFilePath);

						//count++;
					}
					else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				}
				catch (Exception ex) {
					LogStatus(FtpTraceLevel.Error, "Upload Failure for " + localPath + ": " + ex);
					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulUploads(successfulUploads);
						}

						throw new FtpException("An error occurred uploading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					PurgeSuccessfulUploads(successfulUploads);
					successfulUploads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred uploading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulUploads.Count;
		}

		protected void PurgeSuccessfulUploads(IEnumerable<string> remotePaths) {
			foreach (var remotePath in remotePaths) {
				DeleteFile(remotePath);
			}
		}

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localFiles">Files to be uploaded</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, Action<FtpProgress> progress = null) {
			return UploadFiles(localFiles.Select(f => f.FullName), remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling, progress);
		}

	}
}
