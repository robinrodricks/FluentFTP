using System;
using System.IO;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Downloads the specified file onto the local file system.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions"> Sets verification type and what to do if verification fails (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the file will be verified against the source using the verification methods specified by <see cref="FtpVerifyMethod"/> in the client config.
		/// <br/> If only <see cref="FtpVerify.OnlyVerify"/> is set then the return of this method depends on both a successful transfer &amp; verification.
		/// <br/> Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception to propagate from this method.
		/// </remarks>
		/// <returns>FtpStatus flag indicating if the file was downloaded, skipped or failed to transfer.</returns>
		public FtpStatus DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			return DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, progress, new FtpProgress(1, 0));
		}

		/// <summary>
		/// Download from a remote file to a local file
		/// </summary>
		/// <param name="localPath"></param>
		/// <param name="remotePath"></param>
		/// <param name="existsMode"></param>
		/// <param name="verifyOptions"></param>
		/// <param name="progress"></param>
		/// <param name="metaProgress"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="FtpException"></exception>
		protected FtpStatus DownloadFileToFile(string localPath, string remotePath, FtpLocalExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpProgress metaProgress) {
			bool isAppend = false;

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(DownloadFile), new object[] { localPath, remotePath, existsMode, verifyOptions });

			// skip downloading if the localPath is a folder
			if (LocalPaths.IsLocalFolderPath(localPath)) {
				throw new ArgumentException("Local path must specify a file path and not a folder path.", nameof(localPath));
			}

			// skip downloading if local file size matches
			long knownFileSize = 0;
			long restartPos = 0;
			if (existsMode == FtpLocalExists.Resume && File.Exists(localPath)) {
				knownFileSize = GetFileSize(remotePath);
				restartPos = FtpFileStream.GetFileSize(localPath, false);
				if (knownFileSize.Equals(restartPos)) {
					LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully downloaded (Remote: " + remotePath + ", Local: " + localPath + ")");
					if (progress != null) {
						ReportProgress(progress, 0, 0, 0, TimeSpan.Zero, localPath, remotePath, metaProgress);
					}

					return FtpStatus.Skipped;
				}
				else {
					isAppend = true;
				}
			}
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
				LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists locally (Remote: " + remotePath + ", Local: " + localPath + ")");
				if (progress != null) {
					ReportProgress(progress, 0, 0, 0, TimeSpan.Zero, localPath, remotePath, metaProgress);
				}

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
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? Config.RetryAttempts : 1;
			do {

				// download the file from the server to a file stream or memory stream
				downloadSuccess = DownloadFileInternal(localPath, remotePath, null, restartPos, progress, metaProgress, knownFileSize, isAppend, 0);
				attemptsLeft--;

				if (!downloadSuccess) {
					LogWithPrefix(FtpTraceLevel.Info, "Failed to download file.");

					if (attemptsLeft > 0)
						LogWithPrefix(FtpTraceLevel.Info, "Retrying to download file.");
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyTransfer(localPath, remotePath);
					Log(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
					if (!verified && attemptsLeft > 0) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
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

	}
}
