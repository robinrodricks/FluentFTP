using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Exceptions;

#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region Upload Multiple Files

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

		private void PurgeSuccessfulUploads(IEnumerable<string> remotePaths) {
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

#if ASYNC
		/// <summary>
		/// Uploads the given file paths to a single folder on the server asynchronously.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null) {
			
			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (remoteDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteDir");
			}

			remoteDir = remoteDir.GetFtpPath();

			LogFunc(nameof(UploadFilesAsync), new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

			//check if cancellation was requested and throw to set TaskStatus state to Canceled
			token.ThrowIfCancellationRequested();

			//int count = 0;
			var errorEncountered = false;
			var successfulUploads = new List<string>();

			remoteDir = await GetAbsolutePathAsync(remoteDir, token); // a dir is just like a path

			//flag to determine if existence checks are required
			var checkFileExistence = true;

			// create remote dir if wanted
			if (createRemoteDir) {
				if (!await DirectoryExistsAsync(remoteDir, token)) {
					await CreateDirectoryAsync(remoteDir, token);
					checkFileExistence = false;
				}
			}

			// get all the already existing files (if directory was created just create an empty array)
			var existingFiles = checkFileExistence ? await GetNameListingAsync(remoteDir, token) : new string[0];

			// per local file
			var r = -1;
			foreach (var localPath in localPaths) {
				r++;

				// check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();

				// calc remote path
				var fileName = Path.GetFileName(localPath);
				var remoteFilePath = "";

				remoteFilePath = await GetAbsoluteFilePathAsync(remoteDir, fileName, token);

				// create meta progress to store the file progress
				var metaProgress = new FtpProgress(localPaths.Count(), r);

				// try to upload it
				try {
					var ok = await UploadFileFromFileAsync(localPath, remoteFilePath, false, existsMode, FileListings.FileExistsInNameListing(existingFiles, remoteFilePath), true, verifyOptions, token, progress, metaProgress);
					if (ok.IsSuccess()) {
						successfulUploads.Add(remoteFilePath);
					}
					else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				}
				catch (Exception ex) {
					if (ex is OperationCanceledException) {
						//DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
						LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
						throw;
					}

					//suppress all other upload exceptions (errors are still written to FtpTrace)
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
					await PurgeSuccessfulUploadsAsync(successfulUploads);
					successfulUploads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred uploading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulUploads.Count;
		}

		private async Task PurgeSuccessfulUploadsAsync(IEnumerable<string> remotePaths) {
			foreach (var remotePath in remotePaths) {
				await DeleteFileAsync(remotePath);
			}
		}
#endif

		#endregion

		#region Upload File

		/// <summary>
		/// Uploads the specified file directly onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to  <see cref="FtpRemoteExists.NoCheck"/> for fastest performance 
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// </remarks>
		public FtpStatus UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false,
			FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			return UploadFileFromFile(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, progress, new FtpProgress(1, 0));
		}

		private FtpStatus UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpProgress metaProgress) {

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(UploadFile), new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				LogStatus(FtpTraceLevel.Error, "File does not exist: " + localPath);
				return FtpStatus.Failed;
			}
			
			// If retries are allowed set the retry counter to the allowed count
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;

			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			var verified = true;
			FtpStatus uploadStatus;
			bool uploadSuccess;
			do {
					// write the file onto the server
					using (var fileStream = FtpFileStream.GetFileReadStream(this, localPath, false, QuickTransferLimit)) {
					// Upload file
					uploadStatus = UploadFileInternal(fileStream, localPath, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress, metaProgress);
					uploadSuccess = uploadStatus.IsSuccess();
					attemptsLeft--;

					if (!uploadSuccess) {
						LogStatus(FtpTraceLevel.Info, "Failed to upload file.");

						if (attemptsLeft > 0)
							LogStatus(FtpTraceLevel.Info, "Retrying to upload file.");
					}

					// If verification is needed, update the validated flag
					if (uploadSuccess && verifyOptions != FtpVerify.None) {
						verified = VerifyTransfer(localPath, remotePath);
						LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpRemoteExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							// Force overwrite if a retry is required
							existsMode = FtpRemoteExists.Overwrite;
						}
					}
				}
			} while ((!uploadSuccess || !verified) && attemptsLeft > 0); //Loop if attempts are available and the transfer or validation failed

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				DeleteFile(remotePath);
			}

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Uploaded file checksum value does not match local file");
			}

			// if uploaded OK then correctly return Skipped or Success, else return Failed
			return uploadSuccess && verified ? uploadStatus : FtpStatus.Failed;
		}

#if ASYNC
		/// <summary>
		/// Uploads the specified file directly onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to  <see cref="FtpRemoteExists.NoCheck"/> for fastest performance
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// </remarks>
		public async Task<FtpStatus> UploadFileAsync(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			return await UploadFileFromFileAsync(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, token, progress, new FtpProgress(1, 0));
		}

		private async Task<FtpStatus> UploadFileFromFileAsync(string localPath, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, CancellationToken token, IProgress<FtpProgress> progress, FtpProgress metaProgress) {


			// skip uploading if the local file does not exist
#if CORE
			if (!await Task.Run(() => File.Exists(localPath), token)) {
#else
			if (!File.Exists(localPath)) {
#endif
				LogStatus(FtpTraceLevel.Error, "File does not exist: " + localPath);
				return FtpStatus.Failed;
			}

			LogFunc(nameof(UploadFileAsync), new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });
			
			// If retries are allowed set the retry counter to the allowed count
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;

			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			var verified = true;
			FtpStatus uploadStatus;
			bool uploadSuccess;
			do {
				// write the file onto the server
				using (var fileStream = FtpFileStream.GetFileReadStream(this, localPath, true, QuickTransferLimit)) {
					uploadStatus = await UploadFileInternalAsync(fileStream, localPath, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress, token, metaProgress);
					uploadSuccess = uploadStatus.IsSuccess();
					attemptsLeft--;

					if (!uploadSuccess) {
						LogStatus(FtpTraceLevel.Info, "Failed to upload file.");

						if (attemptsLeft > 0)
							LogStatus(FtpTraceLevel.Info, "Retrying to upload file.");
					}

					// If verification is needed, update the validated flag
					if (verifyOptions != FtpVerify.None) {
						verified = await VerifyTransferAsync(localPath, remotePath, token);
						LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpRemoteExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							// Force overwrite if a retry is required
							existsMode = FtpRemoteExists.Overwrite;
						}
					}
				}
			} while ((!uploadSuccess || !verified) && attemptsLeft > 0);

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				await DeleteFileAsync(remotePath, token);
			}

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Uploaded file checksum value does not match local file");
			}

			// if uploaded OK then correctly return Skipped or Success, else return Failed
			return uploadSuccess && verified ? uploadStatus : FtpStatus.Failed;
		}

#endif

		#endregion

		#region	Upload Bytes/Stream

		/// <summary>
		/// Uploads the specified stream as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileStream">The full data of the file, as a stream</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		public FtpStatus UploadStream(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null) {
			// verify args
			if (fileStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(UploadStream), new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return UploadFileInternal(fileStream, null, remotePath, createRemoteDir, existsMode, false, false, progress, new FtpProgress(1, 0));
		}

		/// <summary>
		/// Uploads the specified byte array as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance 
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="progress">Provide a callback to track upload progress.</param>
		public FtpStatus UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, Action<FtpProgress> progress = null) {
			// verify args
			if (fileData == null) {
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(UploadBytes), new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (var ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return UploadFileInternal(ms, null, remotePath, createRemoteDir, existsMode, false, false, progress, new FtpProgress(1, 0));
			}
		}


#if ASYNC
		/// <summary>
		/// Uploads the specified stream as a file onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileStream">The full data of the file, as a stream</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>
		public async Task<FtpStatus> UploadStreamAsync(Stream fileStream, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (fileStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(UploadStreamAsync), new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return await UploadFileInternalAsync(fileStream, null, remotePath, createRemoteDir, existsMode, false, false, progress, token, new FtpProgress(1, 0));
		}

		/// <summary>
		/// Uploads the specified byte array as a file onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>
		public async Task<FtpStatus> UploadBytesAsync(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (fileData == null) {
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(UploadBytesAsync), new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (var ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return await UploadFileInternalAsync(ms, null, remotePath, createRemoteDir, existsMode, false, false, progress, token, new FtpProgress(1, 0));
			}
		}
#endif

		#endregion

		#region Upload File Internal

		/// <summary>
		/// Upload the given stream to the server as a new file. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private FtpStatus UploadFileInternal(Stream fileData, string localPath, string remotePath, bool createRemoteDir,
			FtpRemoteExists existsMode, bool fileExists, bool fileExistsKnown, Action<FtpProgress> progress, FtpProgress metaProgress) {

			Stream upStream = null;

			// throw an error if need to resume uploading and cannot seek the local file stream
			if (!fileData.CanSeek && existsMode == FtpRemoteExists.Resume) {
				throw new ArgumentException("You have requested resuming file upload with FtpRemoteExists.Resume, but the local file stream cannot be seeked. Use another type of Stream or another existsMode.", "fileData");
			}

			try {
				long localPosition = 0, remotePosition = 0, remoteFileLen = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.NoCheck) {
				}
				else if (existsMode == FtpRemoteExists.ResumeNoCheck || existsMode == FtpRemoteExists.AddToEndNoCheck) {

					// start from the end of the remote file, or if failed to read the length then start from the beginning
					remoteFileLen = remotePosition = GetFileSize(remotePath, 0);

					// calculate the local position for appending / resuming
					localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);

				}
				else {

					// check if the remote file exists
					if (!fileExistsKnown) {
						fileExists = FileExists(remotePath);
					}

					if (existsMode == FtpRemoteExists.Skip) {

						if (fileExists) {
							LogStatus(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists on server (Remote: " + remotePath + ", Local: " + localPath + ")");

							// Fix #413 - progress callback isn't called if the file has already been uploaded to the server
							// send progress reports for skipped files
							if (progress != null) {
								progress(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
							}

							return FtpStatus.Skipped;
						}
					}
					else if (existsMode == FtpRemoteExists.Overwrite) {

						// delete the remote file if it exists and we need to overwrite
						if (fileExists) {
							DeleteFile(remotePath);
						}
					}
					else if (existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.AddToEnd) {
						if (fileExists) {

							// start from the end of the remote file, or if failed to read the length then start from the beginning
							remoteFileLen = remotePosition = GetFileSize(remotePath, 0);

							// calculate the local position for appending / resuming
							localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);
						}
					}
				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					var dirname = remotePath.GetFtpDirectoryName();
					if (!DirectoryExists(dirname)) {
						CreateDirectory(dirname);
					}
				}

				// FIX #213 : Do not change Stream.Position if not supported
				if (fileData.CanSeek) {
					try {
						// seek to required offset
						fileData.Position = localPosition;
					}
					catch (Exception ex2) {
					}
				}

				// calc local file len
				var localFileLen = fileData.Length;

				// skip uploading if the mode is resume and the local and remote file have the same length
				if ((existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.ResumeNoCheck) &&
					(localFileLen == remoteFileLen)) {
					LogStatus(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully uploaded (Remote: " + remotePath + ", Local: " + localPath + ")");

					// send progress reports for skipped files
					if (progress != null) {
						progress(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
					}
					return FtpStatus.Skipped;
				}

				// open a file connection
				if (remotePosition == 0 && existsMode != FtpRemoteExists.ResumeNoCheck && existsMode != FtpRemoteExists.AddToEndNoCheck) {
					upStream = OpenWrite(remotePath, UploadDataType, remoteFileLen);
				}
				else {
					upStream = OpenAppend(remotePath, UploadDataType, remoteFileLen);
				}

				// calculate chunk size and rate limiting
				const int rateControlResolution = 100;
				var rateLimitBytes = UploadRateLimit != 0 ? (long)UploadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// calc desired length based on the mode (if need to append to the end of remote file, length is sum of local+remote)
				var remoteFileDesiredLen = (existsMode == FtpRemoteExists.AddToEnd || existsMode == FtpRemoteExists.AddToEndNoCheck) ?
					(upStream.Length + localFileLen)
					: localFileLen;

				var buffer = new byte[chunkSize];

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				// always set the length of the remote file based on the desired size
				// also fixes #288 - Upload hangs with only a few bytes left
				try {
					upStream.SetLength(remoteFileDesiredLen);
				}
				catch (Exception ex2) {
				}

				var anyNoop = false;

				// loop till entire file uploaded
				while (localPosition < localFileLen) {
					try {
						// read a chunk of bytes from the file
						int readBytes;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = fileData.Read(buffer, 0, buffer.Length)) > 0) {

							// write chunk to the FTP stream
							upStream.Write(buffer, 0, readBytes);
							upStream.Flush();

							// move file pointers ahead
							localPosition += readBytes;
							remotePosition += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, localFileLen, localPosition, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
							}

							// Fix #387: keep alive with NOOP as configured and needed
							if (!m_threadSafeDataChannels) {
								anyNoop = Noop() || anyNoop;
							}

							// honor the speed limit
							var swTime = sw.ElapsedMilliseconds;
							if (rateLimitBytes > 0) {
								var timeShouldTake = limitCheckBytes * 1000 / rateLimitBytes;
								if (timeShouldTake > swTime) {
#if CORE14
										Task.Delay((int) (timeShouldTake - swTime)).Wait();
#else
									Thread.Sleep((int)(timeShouldTake - swTime));
#endif
								}
								else if (swTime > timeShouldTake + rateControlResolution) {
									limitCheckBytes = 0;
									sw.Restart();
								}
							}
						}

						// zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
						break;
					}
					catch (IOException ex) {

						// resume if server disconnected midway, or throw if there is an exception doing that as well
						if (!ResumeUpload(remotePath, ref upStream, remotePosition, ex)) {
							sw.Stop();
							throw;
						}

						// since the remote stream has been seeked, we need to reposition the local stream too
						if (fileData.CanSeek) {
							fileData.Seek(localPosition, SeekOrigin.Begin);
						}
						else {
							sw.Stop();
							throw;
						}

					} catch (TimeoutException ex) {
						// fix: attempting to upload data after we reached the end of the stream
						// often throws a timeout exception, so we silently absorb that here
						if (localPosition >= localFileLen) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();


				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// send progress reports
				if (progress != null) {
					progress(new FtpProgress(100.0, upStream.Length, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				try {
					while (!m_threadSafeDataChannels) {
						var status = GetReply();

						// Fix #387: exhaust any NOOP responses (not guaranteed during file transfers)
						if (anyNoop && status.Message != null && status.Message.Contains("NOOP")) {
							continue;
						}

						// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
						// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
						if (status.Code != null && !status.Success) {
							return FtpStatus.Failed;
						}

						// Fix #387: exhaust any NOOP responses also after "226 Transfer complete."
						if (anyNoop) {
							ReadStaleData(false, true, true);
						}

						break;
					}
				}

				// absorb "System.TimeoutException: Timed out trying to read data from the socket stream!" at GetReply()
				catch (Exception) { }

				return FtpStatus.Success;
			}
			catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null) {
						upStream.Dispose();
					}
				}
				catch (Exception) {
				}

				if (ex1 is IOException) {
					LogStatus(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return FtpStatus.Failed;
				}

				// catch errors during upload, 
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

#if ASYNC
		/// <summary>
		/// Upload the given stream to the server as a new file asynchronously. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<FtpStatus> UploadFileInternalAsync(Stream fileData, string localPath, string remotePath, bool createRemoteDir,
			FtpRemoteExists existsMode, bool fileExists, bool fileExistsKnown, IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress) {

			Stream upStream = null;

			// throw an error if need to resume uploading and cannot seek the local file stream
			if (!fileData.CanSeek && existsMode == FtpRemoteExists.Resume) {
				throw new ArgumentException("You have requested resuming file upload with FtpRemoteExists.Resume, but the local file stream cannot be seeked. Use another type of Stream or another existsMode.", "fileData");
			}

			try {
				long localPosition = 0, remotePosition = 0, remoteFileLen = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.NoCheck) {
				}
				else if (existsMode == FtpRemoteExists.ResumeNoCheck || existsMode == FtpRemoteExists.AddToEndNoCheck) {

					// start from the end of the remote file, or if failed to read the length then start from the beginning
					remoteFileLen = remotePosition = await GetFileSizeAsync(remotePath, 0, token);

					// calculate the local position for appending / resuming
					localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);

				}
				else {

					// check if the remote file exists
					if (!fileExistsKnown) {
						fileExists = await FileExistsAsync(remotePath, token);
					}

					if (existsMode == FtpRemoteExists.Skip) {

						if (fileExists) {
							LogStatus(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists on server (Remote: " + remotePath + ", Local: " + localPath + ")");

							// Fix #413 - progress callback isn't called if the file has already been uploaded to the server
							// send progress reports for skipped files
							if (progress != null) {
								progress.Report(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
							}

							return FtpStatus.Skipped;
						}

					}
					else if (existsMode == FtpRemoteExists.Overwrite) {

						// delete the remote file if it exists and we need to overwrite
						if (fileExists) {
							await DeleteFileAsync(remotePath, token);
						}

					}
					else if (existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.AddToEnd) {
						if (fileExists) {

							// start from the end of the remote file, or if failed to read the length then start from the beginning
							remoteFileLen = remotePosition = await GetFileSizeAsync(remotePath, 0, token);

							// calculate the local position for appending / resuming
							localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);
						}

					}

				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					var dirname = remotePath.GetFtpDirectoryName();
					if (!await DirectoryExistsAsync(dirname, token)) {
						await CreateDirectoryAsync(dirname, token);
					}
				}

				// FIX #213 : Do not change Stream.Position if not supported
				if (fileData.CanSeek) {
					try {
						// seek to required offset
						fileData.Position = localPosition;
					}
					catch (Exception ex2) {
					}
				}

				// calc local file len
				var localFileLen = fileData.Length;

				// skip uploading if the mode is resume and the local and remote file have the same length
				if ((existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.ResumeNoCheck) &&
					(localFileLen == remoteFileLen)) {
					LogStatus(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully uploaded (Remote: " + remotePath + ", Local: " + localPath + ")");

					// send progress reports for skipped files
					if (progress != null) {
						progress.Report(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
					}
					return FtpStatus.Skipped;
				}

				// open a file connection
				if (remotePosition == 0 && existsMode != FtpRemoteExists.ResumeNoCheck && existsMode != FtpRemoteExists.AddToEndNoCheck) {
					upStream = await OpenWriteAsync(remotePath, UploadDataType, remoteFileLen, token);
				}
				else {
					upStream = await OpenAppendAsync(remotePath, UploadDataType, remoteFileLen, token);
				}

				// calculate chunk size and rate limiting
				const int rateControlResolution = 100;
				var rateLimitBytes = UploadRateLimit != 0 ? (long)UploadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// calc desired length based on the mode (if need to append to the end of remote file, length is sum of local+remote)
				var remoteFileDesiredLen = (existsMode == FtpRemoteExists.AddToEnd || existsMode == FtpRemoteExists.AddToEndNoCheck) ?
					(upStream.Length + localFileLen)
					: localFileLen;

				var buffer = new byte[chunkSize];

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				// always set the length of the remote file based on the desired size
				// also fixes #288 - Upload hangs with only a few bytes left
				try {
					upStream.SetLength(remoteFileDesiredLen);
				}
				catch (Exception ex2) {
				}

				var anyNoop = false;

				// loop till entire file uploaded
				while (localPosition < localFileLen) {
					try {
						// read a chunk of bytes from the file
						int readBytes;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = await fileData.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
							// write chunk to the FTP stream
							await upStream.WriteAsync(buffer, 0, readBytes, token);
							await upStream.FlushAsync(token);


							// move file pointers ahead
							localPosition += readBytes;
							remotePosition += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, localFileLen, localPosition, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
							}

							// Fix #387: keep alive with NOOP as configured and needed
							if (!m_threadSafeDataChannels) {
								anyNoop = await NoopAsync(token) || anyNoop;
							}

							// honor the rate limit
							var swTime = sw.ElapsedMilliseconds;
							if (rateLimitBytes > 0) {
								var timeShouldTake = limitCheckBytes * 1000 / rateLimitBytes;
								if (timeShouldTake > swTime) {
									await Task.Delay((int)(timeShouldTake - swTime), token);
									token.ThrowIfCancellationRequested();
								}
								else if (swTime > timeShouldTake + rateControlResolution) {
									limitCheckBytes = 0;
									sw.Restart();
								}
							}
						}

						// zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
						break;
					}
					catch (IOException ex) {

						// resume if server disconnected midway, or throw if there is an exception doing that as well
						var resumeResult = await ResumeUploadAsync(remotePath, upStream, remotePosition, ex);
						if (resumeResult.Item1) {
							upStream = resumeResult.Item2;

							// since the remote stream has been seeked, we need to reposition the local stream too
							if (fileData.CanSeek) {
								fileData.Seek(localPosition, SeekOrigin.Begin);
							}
							else {
								sw.Stop();
								throw;
							}

						}
						else {
							sw.Stop();
							throw;
						}
					}
					catch (TimeoutException ex) {
						// fix: attempting to upload data after we reached the end of the stream
						// often throws a timeout exception, so we silently absorb that here
						if (localPosition >= localFileLen) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();

				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// send progress reports
				if (progress != null) {
					progress.Report(new FtpProgress(100.0, upStream.Length, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				try {
					while (!m_threadSafeDataChannels) {
						FtpReply status = await GetReplyAsync(token);

						// Fix #387: exhaust any NOOP responses (not guaranteed during file transfers)
						if (anyNoop && status.Message != null && status.Message.Contains("NOOP")) {
							continue;
						}

						// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
						// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
						if (status.Code != null && !status.Success) {
							return FtpStatus.Failed;
						}

						// Fix #387: exhaust any NOOP responses also after "226 Transfer complete."
						if (anyNoop) {
							await ReadStaleDataAsync(false, true, true, token);
						}

						break;
					}
				}

				// absorb "System.TimeoutException: Timed out trying to read data from the socket stream!" at GetReply()
				catch (Exception) { }

				return FtpStatus.Success;
			}
			catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null) {
						upStream.Dispose();
					}
				}
				catch (Exception) {
				}
				
				if (ex1 is IOException ) {
					LogStatus(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return FtpStatus.Failed;
				}

				if (ex1 is OperationCanceledException) {
					LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
					throw;
				}

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

#endif

		private bool ResumeUpload(string remotePath, ref Stream upStream, long remotePosition, IOException ex) {

#if ASYNC
			try {
#endif

			// if resume possible
			if (ex.IsResumeAllowed()) {

				// dispose the old bugged out stream
				upStream.Dispose();

				// create and return a new stream starting at the current remotePosition
				upStream = OpenAppend(remotePath, UploadDataType, 0);
				upStream.Position = remotePosition;
				return true;
			}

			// resume not allowed
			return false;

#if ASYNC
			}
			catch (Exception resumeEx) {

				throw new AggregateException("Additional error occured while trying to resume uploading the file '" + remotePath + "' at position " + remotePosition, new Exception[] { ex, resumeEx });
			}
#endif

		}

#if ASYNC
		private async Task<Tuple<bool, Stream>> ResumeUploadAsync(string remotePath, Stream upStream, long remotePosition, IOException ex) {

#if ASYNC
			try {
#endif

			// if resume possible
			if (ex.IsResumeAllowed()) {

				// dispose the old bugged out stream
				upStream.Dispose();

				// create and return a new stream starting at the current remotePosition
				var returnStream = await OpenAppendAsync(remotePath, UploadDataType, 0);
				returnStream.Position = remotePosition;
				return Tuple.Create(true, returnStream);
			}

			// resume not allowed
			return Tuple.Create(false, (Stream)null);

#if ASYNC
			}
			catch (Exception resumeEx) {

				throw new AggregateException("Additional error occured while trying to resume uploading the file '" + remotePath + "' at position " + remotePosition, new Exception[] { ex, resumeEx });
			}
#endif
		}
#endif
		private long CalculateAppendLocalPosition(string remotePath, FtpRemoteExists existsMode, long remotePosition) {

			long localPosition = 0;

			// resume - start the local file from the same position as the remote file
			if (existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.ResumeNoCheck) {
				localPosition = remotePosition;
			}

			// append to end - start from the beginning of the local file
			else if (existsMode == FtpRemoteExists.AddToEnd || existsMode == FtpRemoteExists.AddToEndNoCheck) {
				localPosition = 0;
			}

			return localPosition;
		}

#endregion
	}
}
