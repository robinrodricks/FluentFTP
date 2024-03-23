using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Transfer the specified file from the source FTP Server to the destination FTP Server using the FXP protocol.
		/// High-level API that takes care of various edge cases internally.
		/// </summary>
		/// <param name="sourcePath">The full or relative path to the file on the source FTP Server</param>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="remotePath">The full or relative path to destination file on the remote FTP Server</param>
		/// <param name="createRemoteDir">Indicates if the folder should be created on the remote FTP Server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions"> Sets verification type and what to do if verification fails (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <param name="metaProgress"></param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the file will be verified against the source using the verification methods specified by <see cref="FtpVerifyMethod"/> in the client config.
		/// <br/> If only <see cref="FtpVerify.OnlyVerify"/> is set then the return of this method depends on both a successful transfer &amp; verification.
		/// <br/> Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception to propagate from this method.
		/// </remarks>
		/// <returns>Returns a FtpStatus indicating if the file was transferred.</returns>
		public FtpStatus TransferFile(string sourcePath, FtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Resume, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null, FtpProgress metaProgress = null) {

			sourcePath = sourcePath.GetFtpPath();
			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(TransferFile), new object[] { sourcePath, remoteClient, remotePath, Config.FXPDataType, createRemoteDir, existsMode, verifyOptions });

			// verify input params
			VerifyTransferFileParams(sourcePath, remoteClient, remotePath, existsMode);

			// ensure source file exists
			if (!FileExists(sourcePath)) {
				throw new FtpException("Source File " + sourcePath + " cannot be found or does not exists!");
			}

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? Config.RetryAttempts : 1;
			do {

				fxpSuccess = TransferFileFXPInternal(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, metaProgress is null ? new FtpProgress(1, 0) : metaProgress);
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyFXPTransfer(sourcePath, remoteClient, remotePath);
					LogWithPrefix(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
					if (!verified && attemptsLeft > 0) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpRemoteExists.Resume ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
						// Force overwrite if a retry is required
						existsMode = FtpRemoteExists.Overwrite;
					}
				}
			} while (!verified && attemptsLeft > 0);

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				remoteClient.DeleteFile(remotePath);
			}

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return fxpSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;

		}

		/// <summary>
		/// Transfers a file from the source FTP Server to the destination FTP Server via the FXP protocol
		/// </summary>
		protected bool TransferFileFXPInternal(string sourcePath, FtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			Action<FtpProgress> progress, FtpProgress metaProgress) {

			FtpReply reply;
			long offset = 0;
			bool fileExists = false;
			long fileSize = 0;

			var ftpFxpSession = OpenPassiveFXPConnection(remoteClient, progress != null);

			if (ftpFxpSession != null) {
				try {

					ftpFxpSession.SourceServer.Config.ReadTimeout = (int)TimeSpan.FromMinutes(30.0).TotalMilliseconds;
					ftpFxpSession.TargetServer.Config.ReadTimeout = (int)TimeSpan.FromMinutes(30.0).TotalMilliseconds;


					// check if the file exists, and skip, overwrite or append
					if (existsMode == FtpRemoteExists.ResumeNoCheck) {
						offset = remoteClient.GetFileSize(remotePath);
						if (offset == -1) {
							offset = 0; // start from the beginning
						}
					}
					else {
						fileExists = remoteClient.FileExists(remotePath);

						switch (existsMode) {
							case FtpRemoteExists.Skip:

								if (fileExists) {
									LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists (Source: " + sourcePath + ", Dest: " + remotePath + ")");

									//Fix #413 - progress callback isn't called if the file has already been uploaded to the server
									//send progress reports
									progress?.Invoke(new FtpProgress(100.0, 0, 0, TimeSpan.FromSeconds(0), sourcePath, remotePath, metaProgress));

									return true;
								}

								break;

							case FtpRemoteExists.Overwrite:

								if (fileExists) {
									remoteClient.DeleteFile(remotePath);
								}

								break;

							case FtpRemoteExists.Resume:

								if (fileExists) {
									offset = remoteClient.GetFileSize(remotePath);
									if (offset == -1) {
										offset = 0; // start from the beginning
									}
								}

								break;
						}

					}

					fileSize = GetFileSize(sourcePath);

					// ensure the remote dir exists .. only if the file does not already exist!
					if (createRemoteDir && !fileExists) {
						var dirname = remotePath.GetFtpDirectoryName();
						if (!remoteClient.DirectoryExists(dirname)) {
							remoteClient.CreateDirectory(dirname);
						}
					}

					if (offset == 0 && existsMode != FtpRemoteExists.ResumeNoCheck) {
						// send command to tell the source server to 'send' the file to the destination server
						if (!(reply = ftpFxpSession.SourceServer.Execute($"RETR {sourcePath}")).Success) {
							throw new FtpCommandException(reply);
						}

						//Instruct destination server to store the file
						if (!(reply = ftpFxpSession.TargetServer.Execute($"STOR {remotePath}")).Success) {
							throw new FtpCommandException(reply);
						}
					}
					else {
						//tell source server to restart / resume
						if (!(reply = ftpFxpSession.SourceServer.Execute($"REST {offset}")).Success) {
							throw new FtpCommandException(reply);
						}

						// send command to tell the source server to 'send' the file to the destination server
						if (!(reply = ftpFxpSession.SourceServer.Execute($"RETR {sourcePath}")).Success) {
							throw new FtpCommandException(reply);
						}

						//Instruct destination server to append the file
						if (!(reply = ftpFxpSession.TargetServer.Execute($"APPE {remotePath}")).Success) {
							throw new FtpCommandException(reply);
						}
					}

					var transferStarted = DateTime.Now;
					long lastSize = 0;

					var sourceFXPTransferReply = ftpFxpSession.SourceServer.GetReply();
					var destinationFXPTransferReply = ftpFxpSession.TargetServer.GetReply();

					// while the transfer is not complete
					while (!sourceFXPTransferReply.Success || !destinationFXPTransferReply.Success) {

						// send progress reports every 1 second
						if (ftpFxpSession.ProgressServer != null) {

							// send progress reports
							if (progress != null && fileSize != -1) {
								offset = ftpFxpSession.ProgressServer.GetFileSize(remotePath);

								if (offset != -1 && lastSize <= offset) {
									long bytesProcessed = offset - lastSize;
									lastSize = offset;
									ReportProgress(progress, fileSize, offset, bytesProcessed, DateTime.Now - transferStarted, sourcePath, remotePath, metaProgress);
								}
							}
						}
						Thread.Sleep(Config.FXPProgressInterval);
					}

					LogWithPrefix(FtpTraceLevel.Info, $"FXP transfer of file {sourcePath} has completed");

					Noop(true);
					remoteClient.Noop(true);

					ftpFxpSession.Dispose();

					return true;

				}

				// Fix: catch all exceptions and dispose off the FTP clients if one occurs
				catch {
					ftpFxpSession.Dispose();
					throw;
				}
			}
			else {
				LogWithPrefix(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}
		}


	}
}
