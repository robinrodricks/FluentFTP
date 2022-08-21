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
using FluentFTP.Streams;
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Exceptions;
using FluentFTP.Client.Modules;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region Download Multiple Files

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
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			}

			LogFunc(nameof(DownloadFiles), new object[] { localDir, remotePaths, existsMode, verifyOptions });

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
					LogStatus(FtpTraceLevel.Error, "Failed to download " + remotePath + ". Error: " + ex);
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
		

		private void PurgeSuccessfulDownloads(IEnumerable<string> localFiles) {
			foreach (var localFile in localFiles) {
				// absorb any errors because we don't want this to throw more errors!
				try {
					File.Delete(localFile);
				}
				catch (Exception ex) {
					LogStatus(FtpTraceLevel.Warn, "FtpClient : Exception caught and discarded while attempting to delete file '" + localFile + "' : " + ex.ToString());
				}
			}
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it doesn't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken), IProgress<FtpProgress> progress = null) {
			
			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (localDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			}

			LogFunc(nameof(DownloadFilesAsync), new object[] { localDir, remotePaths, existsMode, verifyOptions });

			//check if cancellation was requested and throw to set TaskStatus state to Canceled
			token.ThrowIfCancellationRequested();
			var errorEncountered = false;
			var successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			// per remote file
			var r = -1;
			foreach (var remotePath in remotePaths) {
				r++;

				//check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();

				// calc local path
				var localPath = localDir + remotePath.GetFtpFileName();

				// create meta progress to store the file progress
				var metaProgress = new FtpProgress(remotePaths.Count(), r);

				// try to download it
				try {
					var ok = await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, progress, token, metaProgress);
					if (ok.IsSuccess()) {
						successfulDownloads.Add(localPath);
					}
					else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				}
				catch (Exception ex) {
					if (ex is OperationCanceledException) {
						LogStatus(FtpTraceLevel.Info, "Download cancellation requested");

						//DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
						throw;
					}

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
#endif

		#endregion

		#region Download File

		/// <summary>
		/// Downloads the specified file onto the local file system.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>FtpStatus flag indicating if the file was downloaded, skipped or failed to transfer.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) {
			
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			return DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, progress, new FtpProgress(1, 0));
		}

		private FtpStatus DownloadFileToFile(string localPath, string remotePath, FtpLocalExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpProgress metaProgress) {
			bool isAppend = false;

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadFile), new object[] { localPath, remotePath, existsMode, verifyOptions });

			// skip downloading if the localPath is a folder
			if (LocalPaths.IsLocalFolderPath(localPath)) {
				throw new ArgumentException("Local path must specify a file path and not a folder path.", "localPath");
			}

			// skip downloading if local file size matches
			long knownFileSize = 0;
			long restartPos = 0;
			if (existsMode == FtpLocalExists.Resume && File.Exists(localPath)) {
				knownFileSize = GetFileSize(remotePath);
				restartPos = FtpFileStream.GetFileSize(localPath, false);
				if (knownFileSize.Equals(restartPos)) {
					LogStatus(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully downloaded (Remote: " + remotePath + ", Local: " + localPath + ")");
					return FtpStatus.Skipped;
				}
				else {
					isAppend = true;
				}
			}
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
				LogStatus(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists locally (Remote: " + remotePath + ", Local: " + localPath + ")");
				return FtpStatus.Skipped;
			}

			try {
				// create the folders
				var dirPath = Path.GetDirectoryName(localPath);
				if (!Strings.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
					Directory.CreateDirectory(dirPath);
				}
			}
			catch (Exception ex1) {
				// catch errors creating directory
				throw new FtpException("Error while creating directories. See InnerException for more info.", ex1);
			}

			// if not appending then fetch remote file size since mode is determined by that
			/*if (knownFileSize == 0 && !isAppend) {
				knownFileSize = GetFileSize(remotePath);
			}*/

			bool downloadSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {

				// download the file from the server to a file stream or memory stream
				downloadSuccess = DownloadFileInternal(localPath, remotePath, null, restartPos, progress, metaProgress, knownFileSize, isAppend);
				attemptsLeft--;

				if (!downloadSuccess) {
					LogStatus(FtpTraceLevel.Info, "Failed to download file.");

					if (attemptsLeft > 0)
						LogStatus(FtpTraceLevel.Info, "Retrying to download file.");
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyTransfer(localPath, remotePath);
					LogLine(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
					if (!verified && attemptsLeft > 0) {
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
						// Force overwrite if a retry is required
						existsMode = FtpLocalExists.Overwrite;
					}
				}
			} while ((!downloadSuccess || !verified) && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified file onto the local file system asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it doesn't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>FtpStatus flag indicating if the file was downloaded, skipped or failed to transfer.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public async Task<FtpStatus> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Resume, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}
			
			return await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, progress, token, new FtpProgress(1, 0));
		}

		private async Task<FtpStatus> DownloadFileToFileAsync(string localPath, string remotePath, FtpLocalExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress) {
			
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			// skip downloading if the localPath is a folder
			if (LocalPaths.IsLocalFolderPath(localPath)) {
				throw new ArgumentException("Local path must specify a file path and not a folder path.", "localPath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadFileAsync), new object[] { localPath, remotePath, existsMode, verifyOptions });


			bool isAppend = false;

			// skip downloading if the local file exists
			long knownFileSize = 0;
			long restartPos = 0;
#if CORE
			if (existsMode == FtpLocalExists.Resume && await Task.Run(() => File.Exists(localPath), token)) {
				knownFileSize = (await GetFileSizeAsync(remotePath, -1, token));
				restartPos = await FtpFileStream.GetFileSizeAsync(localPath, false, token);
				if (knownFileSize.Equals(restartPos)) {
#else
			if (existsMode == FtpLocalExists.Resume && File.Exists(localPath)) {
				knownFileSize = (await GetFileSizeAsync(remotePath, -1, token));
				restartPos = FtpFileStream.GetFileSize(localPath, false);
				if (knownFileSize.Equals(restartPos)) {
#endif
					LogStatus(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully downloaded (Remote: " + remotePath + ", Local: " + localPath + ")");
					return FtpStatus.Skipped;
				}
				else {
					isAppend = true;
				}
			}
#if CORE
			else if (existsMode == FtpLocalExists.Skip && await Task.Run(() => File.Exists(localPath), token)) {
#else
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
#endif
				LogStatus(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists locally (Remote: " + remotePath + ", Local: " + localPath + ")");
				return FtpStatus.Skipped;
			}

			try {
				// create the folders
				var dirPath = Path.GetDirectoryName(localPath);
#if CORE
				if (!string.IsNullOrWhiteSpace(dirPath) && !await Task.Run(() => Directory.Exists(dirPath), token)) {
#else
				if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
#endif
					Directory.CreateDirectory(dirPath);
				}
			}
			catch (Exception ex1) {
				// catch errors creating directory
				throw new FtpException("Error while crated directories. See InnerException for more info.", ex1);
			}

			// if not appending then fetch remote file size since mode is determined by that
			/*if (knownFileSize == 0 && !isAppend) {
				knownFileSize = GetFileSize(remotePath);
			}*/

			bool downloadSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {

				// download the file from the server to a file stream or memory stream
				downloadSuccess = await DownloadFileInternalAsync(localPath, remotePath, null, restartPos, progress, token, metaProgress, knownFileSize, isAppend);
				attemptsLeft--;

				if (!downloadSuccess) {
					LogStatus(FtpTraceLevel.Info, "Failed to download file.");

					if (attemptsLeft > 0)
						LogStatus(FtpTraceLevel.Info, "Retrying to download file.");
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = await VerifyTransferAsync(localPath, remotePath, token);
					LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
					if (!verified && attemptsLeft > 0) {
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Resume ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
						// Force overwrite if a retry is required
						existsMode = FtpLocalExists.Overwrite;
					}
				}
			} while ((!downloadSuccess || !verified) && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;
		}

#endif

		#endregion

		#region	Download Bytes/Stream

		/// <summary>
		/// Downloads the specified file into the specified stream.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null) {
			// verify args
			if (outStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadStream), new object[] { remotePath });

			// download the file from the server
			return DownloadFileInternal(null, remotePath, outStream, restartPosition, progress, new FtpProgress(1, 0), 0, false);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadBytes), new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (var outStream = new MemoryStream()) {
				ok = DownloadFileInternal(null, remotePath, outStream, restartPosition, progress, new FtpProgress(1, 0), 0, false);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}

			return ok;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public async Task<bool> DownloadStreamAsync(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (outStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadStreamAsync), new object[] { remotePath });

			// download the file from the server
			return await DownloadFileInternalAsync(null, remotePath, outStream, restartPosition, progress, token, new FtpProgress(1, 0), 0, false);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadBytesAsync(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadBytesAsync), new object[] { remotePath });

			// download the file from the server
			using (var outStream = new MemoryStream()) {
				var ok = await DownloadFileInternalAsync(null, remotePath, outStream, restartPosition, progress, token, new FtpProgress(1, 0), 0, false);
				return ok ? outStream.ToArray() : null;
			}
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadBytesAsync(string remotePath, CancellationToken token = default(CancellationToken)) {
			// download the file from the server
			return await DownloadBytesAsync(remotePath, 0, null, token);
		}
#endif

		#endregion

		#region Download File Internal

		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool DownloadFileInternal(string localPath, string remotePath, Stream outStream, long restartPosition,
			Action<FtpProgress> progress, FtpProgress metaProgress, long knownFileSize, bool isAppend) {

			Stream downStream = null;
			var disposeOutStream = false;

			try {
				// get file size if progress requested
				long fileLen = 0;

				if (progress != null) {
					fileLen = knownFileSize > 0 ? knownFileSize : GetFileSize(remotePath);
				}

				// open the file for reading
				downStream = OpenRead(remotePath, DownloadDataType, restartPosition, fileLen);

				// if the server has not provided a length for this file or
				// if the mode is ASCII or
				// if the server is IBM z/OS
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = (fileLen <= 0) || 
								(DownloadDataType == FtpDataType.ASCII) || 
								(ServerHandler != null && ServerHandler.AlwaysReadToEnd(remotePath));

				const int rateControlResolution = 100;
				var rateLimitBytes = DownloadRateLimit != 0 ? (long)DownloadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// loop till entire file downloaded
				var buffer = new byte[chunkSize];
				var offset = restartPosition;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var anyNoop = false;

				// Fix #554: ability to download zero-byte files
				if (DownloadZeroByteFiles && outStream == null && localPath != null){
					outStream = FtpFileStream.GetFileWriteStream(this, localPath, false, QuickTransferLimit, knownFileSize, isAppend, restartPosition);
					disposeOutStream = true;
				}
							
				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {

							// Fix #552: only create outstream when first bytes downloaded
							if (outStream == null && localPath != null){
								outStream = FtpFileStream.GetFileWriteStream(this, localPath, false, QuickTransferLimit, knownFileSize, isAppend, restartPosition);
								disposeOutStream = true;
							}
							
							// write chunk to output stream
							outStream.Write(buffer, 0, readBytes);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen, offset, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
							}

							// Fix #387: keep alive with NOOP as configured and needed
							if (!m_threadSafeDataChannels) {
								anyNoop = Noop() || anyNoop;
							}

							// honor the rate limit
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

						// if we reach here means EOF encountered
						// stop if we are in "read until EOF" mode
						if (readToEnd || offset == fileLen) {
							break;
						}

						// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
						throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
					}
					catch (IOException ex) {

						// resume if server disconnected midway, or throw if there is an exception doing that as well
						if (!ResumeDownload(remotePath, ref downStream, offset, ex)) {
							sw.Stop();
							throw;
						}
					}
					catch (TimeoutException ex) {

						// fix: attempting to download data after we reached the end of the stream
						// often throws a timeout exception, so we silently absorb that here
						if (offset >= fileLen && !readToEnd) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();

				// disconnect FTP stream before exiting
				if (outStream != null) {
					outStream.Flush();
				}
				downStream.Dispose();

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					outStream.Dispose();
					disposeOutStream = false;
				}

				// send progress reports
				if (progress != null) {
					progress(new FtpProgress(100.0, offset, 0, TimeSpan.Zero, localPath, remotePath, metaProgress));
				}

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
							return false;
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

				return true;
			}
			catch (Exception ex1) {

				// close stream before throwing error
				try {
					downStream.Dispose();
				}
				catch (Exception) {
				}

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					try {
						outStream.Dispose();
						disposeOutStream = false;
					}
					catch (Exception) {
					}
				}

				if (ex1 is IOException) {
					LogStatus(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return false;
				}

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.IsKnownError(ServerStringModule.fileNotFound)) {
					LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1.Message);
					return false;
				}

				// catch errors during download
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

		/// <summary>
		/// Calculate transfer chunk size taking rate control into account
		/// </summary>
		private int CalculateTransferChunkSize(Int64 rateLimitBytes, int rateControlResolution) {
			int chunkSize = TransferChunkSize;

			// if user has not specified a TransferChunkSize and rate limiting is enabled
			if (m_transferChunkSize == null && rateLimitBytes > 0) {

				// reduce chunk size to optimize rate control
				const int chunkSizeMin = 64;
				while (chunkSize > chunkSizeMin) {
					var chunkLenInMs = 1000L * chunkSize / rateLimitBytes;
					if (chunkLenInMs <= rateControlResolution) {
						break;
					}

					chunkSize = Math.Max(chunkSize >> 1, chunkSizeMin);
				}
			}
			return chunkSize;
		}

#if ASYNC
		/// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> DownloadFileInternalAsync(string localPath, string remotePath, Stream outStream, long restartPosition,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress, long knownFileSize, bool isAppend) {

			Stream downStream = null;
			var disposeOutStream = false;

			try {
				// get file size if progress requested
				long fileLen = 0;

				if (progress != null) {
					fileLen = knownFileSize > 0 ? knownFileSize : await GetFileSizeAsync(remotePath, -1, token);
				}

				// open the file for reading
				downStream = await OpenReadAsync(remotePath, DownloadDataType, restartPosition, fileLen, token);

				// if the server has not provided a length for this file or
				// if the mode is ASCII or
				// if the server is IBM z/OS
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = (fileLen <= 0) || 
								(DownloadDataType == FtpDataType.ASCII) ||
								(ServerHandler != null && ServerHandler.AlwaysReadToEnd(remotePath));

				const int rateControlResolution = 100;
				var rateLimitBytes = DownloadRateLimit != 0 ? (long)DownloadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// loop till entire file downloaded
				var buffer = new byte[chunkSize];
				var offset = restartPosition;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var anyNoop = false;

				// Fix #554: ability to download zero-byte files
				if (DownloadZeroByteFiles && outStream == null && localPath != null) {
					outStream = FtpFileStream.GetFileWriteStream(this, localPath, true, QuickTransferLimit, knownFileSize, isAppend, restartPosition);
					disposeOutStream = true;
				}

				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
							
							// Fix #552: only create outstream when first bytes downloaded
							if (outStream == null && localPath != null) {
								outStream = FtpFileStream.GetFileWriteStream(this, localPath, true, QuickTransferLimit, knownFileSize, isAppend, restartPosition);
								disposeOutStream = true;
							}

							// write chunk to output stream
							await outStream.WriteAsync(buffer, 0, readBytes, token);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen, offset, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
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

						// if we reach here means EOF encountered
						// stop if we are in "read until EOF" mode
						if (readToEnd || offset == fileLen) {
							break;
						}

						// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
						throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
					}
					catch (IOException ex) {

						// resume if server disconnected midway, or throw if there is an exception doing that as well
						var resumeResult = await ResumeDownloadAsync(remotePath, downStream, offset, ex);
						if (resumeResult.Item1) {
							downStream = resumeResult.Item2;
						}
						else {
							sw.Stop();
							throw;
						}
					}
					catch (TimeoutException ex) {

						// fix: attempting to download data after we reached the end of the stream
						// often throws a timeout exception, so we silently absorb that here
						if (offset >= fileLen && !readToEnd) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();

				// disconnect FTP stream before exiting
				if (outStream != null) {
					await outStream.FlushAsync(token);
				}
				downStream.Dispose();

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					outStream.Dispose();
					disposeOutStream = false;
				}

				// send progress reports
				if (progress != null) {
					progress.Report(new FtpProgress(100.0, offset, 0, TimeSpan.Zero, localPath, remotePath, metaProgress));
				}

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
							return false;
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

				return true;
			}
			catch (Exception ex1) {

				// close stream before throwing error
				try {
					downStream.Dispose();
				}
				catch (Exception) {
				}

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					try {
						outStream.Dispose();
						disposeOutStream = false;
					}
					catch (Exception) {
					}
				}

				if (ex1 is IOException) {
					LogStatus(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return false;
				}

				if (ex1 is OperationCanceledException) {
					LogStatus(FtpTraceLevel.Info, "Download cancellation requested");
					throw;
				}

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.IsKnownError(ServerStringModule.fileNotFound)) {
					LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1.Message);
					return false;
				}

				// catch errors during download
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}
#endif

		private bool ResumeDownload(string remotePath, ref Stream downStream, long offset, IOException ex) {
			if (ex.IsResumeAllowed())
			{
				downStream.Dispose();
				downStream = OpenRead(remotePath, DownloadDataType, offset);

				return true;
			}

			return false;
		}

#if ASYNC
		private async Task<Tuple<bool, Stream>> ResumeDownloadAsync(string remotePath, Stream downStream, long offset, IOException ex) {
			if (ex.IsResumeAllowed())
			{
				downStream.Dispose();

				return Tuple.Create(true, await OpenReadAsync(remotePath, DownloadDataType, offset));
			}

			return Tuple.Create(false, (Stream)null);
		}
#endif

		#endregion
	}
}