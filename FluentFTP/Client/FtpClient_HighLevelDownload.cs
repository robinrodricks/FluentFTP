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
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None,
			FtpError errorHandling = FtpError.None) {
			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (localDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			}

			LogFunc("DownloadFiles", new object[] { localDir, remotePaths, existsMode, verifyOptions });

			var errorEncountered = false;
			var successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (var remotePath in remotePaths) {
				// calc local path
				var localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
					var ok = DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, null);
					if (ok) {
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

		/*
		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		public int DownloadFiles(string localDir, List<string> remotePaths, bool overwrite = true, FtpError errorHandling = FtpError.None) {
			return DownloadFiles(localDir, remotePaths.ToArray(), overwrite);
		}*/

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
		/// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it dosen't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="token">The token to monitor for cancellation requests</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (!errorHandling.IsValidCombination()) {
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			}

			if (localDir.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			}

			LogFunc("DownloadFilesAsync", new object[] { localDir, remotePaths, existsMode, verifyOptions });

			//check if cancellation was requested and throw to set TaskStatus state to Canceled
			token.ThrowIfCancellationRequested();
			var errorEncountered = false;
			var successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (var remotePath in remotePaths) {
				//check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();

				// calc local path
				var localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
					bool ok = await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, token: token);
					if (ok) {
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
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public bool DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("DownloadFile", new object[] { localPath, remotePath, existsMode, verifyOptions });

			return DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, progress);
		}

		private bool DownloadFileToFile(string localPath, string remotePath, FtpLocalExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress) {
			var outStreamFileMode = FileMode.Create;

			// skip downloading if local file size matches
			if (existsMode == FtpLocalExists.Append && File.Exists(localPath)) {
				if (GetFileSize(remotePath).Equals(new FileInfo(localPath).Length)) {
					LogStatus(FtpTraceLevel.Info, "Append is selected => Local file size matches size on server => skipping");
					return false;
				}
				else {
					outStreamFileMode = FileMode.Append;
				}
			}
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
				LogStatus(FtpTraceLevel.Info, "Skip is selected => Local file exists => skipping");
				return false;
			}

			try {
				// create the folders
				var dirPath = Path.GetDirectoryName(localPath);
				if (!FtpExtensions.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
					Directory.CreateDirectory(dirPath);
				}
			}
			catch (Exception ex1) {
				// catch errors creating directory
				throw new FtpException("Error while creating directories. See InnerException for more info.", ex1);
			}

			bool downloadSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {
				// download the file from server
				using (var outStream = new FileStream(localPath, outStreamFileMode, FileAccess.Write, FileShare.None)) {
					// download the file straight to a file stream
					downloadSuccess = DownloadFileInternal(remotePath, outStream, File.Exists(localPath) ? new FileInfo(localPath).Length : 0, progress);
					attemptsLeft--;
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyTransfer(localPath, remotePath);
					LogLine(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}

#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified file onto the local file system asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it dosen't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public async Task<bool> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("DownloadFileAsync", new object[] { localPath, remotePath, existsMode, verifyOptions });

			return await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, progress, token);
		}

		private async Task<bool> DownloadFileToFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("DownloadFileAsync", new object[] { localPath, remotePath, existsMode, verifyOptions });


			var outStreamFileMode = FileMode.Create;

			// skip downloading if the local file exists
#if CORE
			if (existsMode == FtpLocalExists.Append && await Task.Run(() => File.Exists(localPath), token)) {
				if ((await GetFileSizeAsync(remotePath, token)).Equals((await Task.Run(() => new FileInfo(localPath), token)).Length)) {
#else
			if (existsMode == FtpLocalExists.Append && File.Exists(localPath)) {
				if ((await GetFileSizeAsync(remotePath)).Equals(new FileInfo(localPath).Length)) {
#endif
					LogStatus(FtpTraceLevel.Info, "Append is enabled => Local file size matches size on server => skipping");
					return false;
				}
				else {
					outStreamFileMode = FileMode.Append;
				}
			}
#if CORE
			else if (existsMode == FtpLocalExists.Skip && await Task.Run(() => File.Exists(localPath), token)) {
#else
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
#endif
				LogStatus(FtpTraceLevel.Info, "Skip is selected => Local file exists => skipping");
				return false;
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

			bool downloadSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {
				// download the file from server
				using (var outStream = new FileStream(localPath, outStreamFileMode, FileAccess.Write, FileShare.None, 4096, true)) {
					// download the file straight to a file stream
					downloadSuccess = await DownloadFileInternalAsync(remotePath, outStream, await Task.Run(() => File.Exists(localPath), token) ? (await Task.Run(() => new FileInfo(localPath), token)).Length : 0, progress, token);
					attemptsLeft--;
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = await VerifyTransferAsync(localPath, remotePath, token);
					LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Append ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}

#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified;
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
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(Stream outStream, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null) {
			// verify args
			if (outStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("Download", new object[] { remotePath });

			// download the file from the server
			return DownloadFileInternal(remotePath, outStream, restartPosition, progress);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("Download", new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (var outStream = new MemoryStream()) {
				ok = DownloadFileInternal(remotePath, outStream, restartPosition, progress);
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
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public async Task<bool> DownloadAsync(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (outStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("DownloadAsync", new object[] { remotePath });

			// download the file from the server
			return await DownloadFileInternalAsync(remotePath, outStream, restartPosition, progress, token);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadAsync(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			LogFunc("DownloadAsync", new object[] { remotePath });

			// download the file from the server
			using (var outStream = new MemoryStream()) {
				bool ok = await DownloadFileInternalAsync(remotePath, outStream, restartPosition, progress, token);
				return ok ? outStream.ToArray() : null;
			}
		}

		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadAsync(string remotePath, CancellationToken token = default(CancellationToken)) {
			// download the file from the server
			return await DownloadAsync(remotePath, 0, null, token);
		}
#endif

		#endregion

		#region Download File Internal

		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool DownloadFileInternal(string remotePath, Stream outStream, long restartPosition, Action<FtpProgress> progress) {
			Stream downStream = null;

			try {
				// get file size if downloading in binary mode (in ASCII mode we read until EOF)
				long fileLen = 0;
				if (DownloadDataType == FtpDataType.Binary && progress != null) {
					fileLen = GetFileSize(remotePath);
				}

				// open the file for reading
				downStream = OpenRead(remotePath, DownloadDataType, restartPosition, fileLen > 0);

				// if the server has not provided a length for this file
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = fileLen <= 0;

				const int rateControlResolution = 100;
				var rateLimitBytes = DownloadRateLimit != 0 ? (long)DownloadRateLimit * 1024 : 0;
				var chunkSize = TransferChunkSize;
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

				// loop till entire file downloaded
				var buffer = new byte[chunkSize];
				var offset = restartPosition;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var anyNoop = false;

				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {
							// write chunk to output stream
							outStream.Write(buffer, 0, readBytes);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen, offset, bytesProcessed, DateTime.Now - transferStarted);
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
						// often throws a timeout execption, so we silently absorb that here
						if (offset >= fileLen) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();

				// send progress reports
				if (progress != null) {
					progress(new FtpProgress(100.0, 0, TimeSpan.Zero));
				}

				// disconnect FTP stream before exiting
				outStream.Flush();
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				try {
					while (!m_threadSafeDataChannels) {
						var status = GetReply();

						// Fix #387: exhaust any NOOP responses (not guaranteed during file transfers)
						if (anyNoop && status.Message != null && status.Message.Contains("NOOP")) {
							continue;
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

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

#if ASYNC
		/// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> DownloadFileInternalAsync(string remotePath, Stream outStream, long restartPosition, IProgress<FtpProgress> progress, CancellationToken token = default(CancellationToken)) {
			Stream downStream = null;
			try {
				// get file size if downloading in binary mode (in ASCII mode we read until EOF)
				long fileLen = 0;

				if (DownloadDataType == FtpDataType.Binary && progress != null) {
					fileLen = await GetFileSizeAsync(remotePath, token);
				}

				// open the file for reading
				downStream = await OpenReadAsync(remotePath, DownloadDataType, restartPosition, fileLen > 0, token);

				// if the server has not provided a length for this file
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = fileLen <= 0;

				const int rateControlResolution = 100;
				var rateLimitBytes = DownloadRateLimit != 0 ? (long)DownloadRateLimit * 1024 : 0;
				var chunkSize = TransferChunkSize;
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

				// loop till entire file downloaded
				var buffer = new byte[chunkSize];
				var offset = restartPosition;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var anyNoop = false;

				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
							// write chunk to output stream
							await outStream.WriteAsync(buffer, 0, readBytes, token);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen, offset, bytesProcessed, DateTime.Now - transferStarted);
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
						// often throws a timeout execption, so we silently absorb that here
						if (offset >= fileLen) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				sw.Stop();

				// send progress reports
				if (progress != null) {
					progress.Report(new FtpProgress(100.0, 0, TimeSpan.Zero));
				}

				// disconnect FTP stream before exiting
				await outStream.FlushAsync(token);
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				try {
					while (!m_threadSafeDataChannels) {
						FtpReply status = await GetReplyAsync(token);

						// Fix #387: exhaust any NOOP responses (not guaranteed during file transfers)
						if (anyNoop && status.Message != null && status.Message.Contains("NOOP")) {
							continue;
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

				if (ex1 is OperationCanceledException) {
					LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
					throw;
				}

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}
#endif

		private bool ResumeDownload(string remotePath, ref Stream downStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39 and #410)
			if (ex.InnerException != null || ex.Message.IsKnownError(unexpectedEOFStrings)) {
				var ie = ex.InnerException as SocketException;
#if CORE
								if (ie == null || ie != null && (int) ie.SocketErrorCode == 10054) {
#else
				if (ie == null || ie != null && ie.ErrorCode == 10054) {
#endif
					downStream.Dispose();
					downStream = OpenRead(remotePath, DownloadDataType, offset);
					return true;
				}
			}

			return false;
		}

#if ASYNC
		private async Task<Tuple<bool, Stream>> ResumeDownloadAsync(string remotePath, Stream downStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39 and #410)
			if (ex.InnerException != null || ex.Message.IsKnownError(unexpectedEOFStrings)) {
				var ie = ex.InnerException as SocketException;
#if CORE
				if (ie == null || ie != null && (int) ie.SocketErrorCode == 10054) {
#else
				if (ie == null || ie != null && ie.ErrorCode == 10054) {
#endif
					downStream.Dispose();
					return Tuple.Create(true, await OpenReadAsync(remotePath, DownloadDataType, offset));
				}
			}

			return Tuple.Create(false, (Stream)null);
		}
#endif

		#endregion
	}
}