using System;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Transfer the specified file from the source FTP Server to the destination FTP Server asynchronously using the FXP protocol.
		/// High-level API that takes care of various edge cases internally.
		/// </summary>
		/// <param name="sourcePath">The full or relative path to the file on the source FTP Server</param>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="remotePath">The full or relative path to destination file on the remote FTP Server</param>
		/// <param name="createRemoteDir">Indicates if the folder should be created on the remote FTP Server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets verification behaviour and what to do if verification fails (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <param name="metaProgress"></param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the file will be verified against the source using the verification methods specified by <see cref="FtpVerifyMethod"/> in the client config.
		/// <br/> If only <see cref="FtpVerify.OnlyVerify"/> is set then the return of this method depends on both a successful transfer &amp; verification.
		/// <br/> Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception to propagate from this method.
		/// </remarks>
		/// <returns> Returns a FtpStatus indicating if the file was transferred. </returns>
		public async Task<FtpStatus> TransferFile(string sourcePath, AsyncFtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Resume, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, FtpProgress metaProgress = null, CancellationToken token = default(CancellationToken)) {

			sourcePath = sourcePath.GetFtpPath();
			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(TransferFile), new object[] { sourcePath, remoteClient, remotePath, Config.FXPDataType, createRemoteDir, existsMode, verifyOptions });

			// verify input params
			VerifyTransferFileParams(sourcePath, remoteClient, remotePath, existsMode);

			// ensure source file exists
			if (!await FileExists(sourcePath, token)) {
				throw new FtpException("Source File " + sourcePath + " cannot be found or does not exists!");
			}

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? Config.RetryAttempts : 1;
			do {

				fxpSuccess = await TransferFileFXPInternal(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, token, metaProgress is null ? new FtpProgress(1, 0) : metaProgress);
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None) {
					verified = await VerifyFXPTransferAsync(sourcePath, remoteClient, remotePath, token);
					LogWithPrefix(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
					if (!verified && attemptsLeft > 0) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpRemoteExists.Resume ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
						// Force overwrite if a retry is required
						existsMode = FtpRemoteExists.Overwrite;
					}
				}
			} while (!verified && attemptsLeft > 0);

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				await remoteClient.DeleteFile(remotePath, token);
			}

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return fxpSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;

		}

		/// <summary>
		/// Transfers a file from the source FTP Server to the destination FTP Server via the FXP protocol asynchronously.
		/// </summary>
		protected async Task<bool> TransferFileFXPInternal(string sourcePath, AsyncFtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress) {
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;
			long fileSize = 0;

			var ftpFxpSession = await OpenPassiveFXPConnectionAsync(remoteClient, progress != null, token);

			if (ftpFxpSession != null) {

				try {

					ftpFxpSession.SourceServer.Config.ReadTimeout = (int)TimeSpan.FromMinutes(30.0).TotalMilliseconds;
					ftpFxpSession.TargetServer.Config.ReadTimeout = (int)TimeSpan.FromMinutes(30.0).TotalMilliseconds;


					// check if the file exists, and skip, overwrite or append
					if (existsMode == FtpRemoteExists.ResumeNoCheck) {
						offset = await remoteClient.GetFileSize(remotePath, -1, token);
						if (offset == -1) {
							offset = 0; // start from the beginning
						}
					}
					else {
						fileExists = await remoteClient.FileExists(remotePath, token);

						switch (existsMode) {
							case FtpRemoteExists.Skip:

								if (fileExists) {
									LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists (Source: " + sourcePath + ", Dest: " + remotePath + ")");

									//Fix #413 - progress callback isn't called if the file has already been uploaded to the server
									//send progress reports
									progress?.Report(new FtpProgress(100.0, 0, 0, TimeSpan.FromSeconds(0), sourcePath, remotePath, metaProgress));

									return true;
								}

								break;

							case FtpRemoteExists.Overwrite:

								if (fileExists) {
									await remoteClient.DeleteFile(remotePath, token);
								}

								break;

							case FtpRemoteExists.Resume:

								if (fileExists) {
									offset = await remoteClient.GetFileSize(remotePath, 0, token);
								}

								break;
						}

					}

					fileSize = await GetFileSize(sourcePath, -1, token);

					// ensure the remote dir exists .. only if the file does not already exist!
					if (createRemoteDir && !fileExists) {
						var dirname = remotePath.GetFtpDirectoryName();
						if (!await remoteClient.DirectoryExists(dirname, token)) {
							await remoteClient.CreateDirectory(dirname, token);
						}
					}

					if (offset == 0 && existsMode != FtpRemoteExists.ResumeNoCheck) {
						// send command to tell the source server to 'send' the file to the destination server
						if (!(reply = await ftpFxpSession.SourceServer.Execute($"RETR {sourcePath}", token)).Success) {
							throw new FtpCommandException(reply);
						}

						//Instruct destination server to store the file
						if (!(reply = await ftpFxpSession.TargetServer.Execute($"STOR {remotePath}", token)).Success) {
							throw new FtpCommandException(reply);
						}
					}
					else {
						//tell source server to restart / resume
						if (!(reply = await ftpFxpSession.SourceServer.Execute($"REST {offset}", token)).Success) {
							throw new FtpCommandException(reply);
						}

						// send command to tell the source server to 'send' the file to the destination server
						if (!(reply = await ftpFxpSession.SourceServer.Execute($"RETR {sourcePath}", token)).Success) {
							throw new FtpCommandException(reply);
						}

						//Instruct destination server to append the file
						if (!(reply = await ftpFxpSession.TargetServer.Execute($"APPE {remotePath}", token)).Success) {
							throw new FtpCommandException(reply);
						}
					}

					var transferStarted = DateTime.Now;
					long lastSize = 0;


					var sourceFXPTransferReply = ftpFxpSession.SourceServer.GetReply(token);
					var destinationFXPTransferReply = ftpFxpSession.TargetServer.GetReply(token);

					// while the transfer is not complete
					while (!sourceFXPTransferReply.IsCompleted || !destinationFXPTransferReply.IsCompleted) {

						// send progress reports every 1 second
						if (ftpFxpSession.ProgressServer != null) {

							// send progress reports
							if (progress != null && fileSize != -1) {
								offset = await ftpFxpSession.ProgressServer.GetFileSize(remotePath, -1, token);

								if (offset != -1 && lastSize <= offset) {
									long bytesProcessed = offset - lastSize;
									lastSize = offset;
									ReportProgress(progress, fileSize, offset, bytesProcessed, DateTime.Now - transferStarted, sourcePath, remotePath, metaProgress);
								}
							}
						}

						await Task.Delay(Config.FXPProgressInterval, token);
					}

					LogWithPrefix(FtpTraceLevel.Info, $"FXP transfer of file {sourcePath} has completed");

					await Noop(true, token);
					await remoteClient.Noop(true, token);

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
