using System;
using System.IO;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

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
		/// <param name="verifyOptions">Sets verification behaviour and what to do if verification fails (See Remarks)</param>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the file will be verified against the source using the verification methods specified by <see cref="FtpVerifyMethod"/> in the client config.
		/// <br/> If only <see cref="FtpVerify.OnlyVerify"/> is set then the return of this method depends on both a successful transfer &amp; verification.
		/// <br/> Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpRemoteExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception to propagate from this method.
		/// </remarks>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>

		public async Task<FtpStatus> UploadFile(string localPath, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			return await UploadFileFromFile(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, token, progress, new FtpProgress(1, 0));
		}

		/// <summary>
		/// Upload from a local file to a remote file
		/// </summary>
		protected async Task<FtpStatus> UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, CancellationToken token, IProgress<FtpProgress> progress, FtpProgress metaProgress) {


			// skip uploading if the local file does not exist
#if NETSTANDARD || NET5_0_OR_GREATER
			if (!await Task.Run(() => File.Exists(localPath), token)) {
#else
			if (!File.Exists(localPath)) {
#endif
				LogWithPrefix(FtpTraceLevel.Error, "File does not exist: " + localPath);
				return FtpStatus.Failed;
			}

			LogFunction(nameof(UploadFile), new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			// If retries are allowed set the retry counter to the allowed count
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? Config.RetryAttempts : 1;

			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			var verified = true;
			FtpStatus uploadStatus;
			bool uploadSuccess;
			do {
				// write the file onto the server
				using (var fileStream = FtpFileStream.GetFileReadStream(this, localPath, true, 0)) {
					uploadStatus = await UploadFileInternalAsync(fileStream, localPath, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress, token, metaProgress);
					uploadSuccess = uploadStatus.IsSuccess();
					attemptsLeft--;

					if (!uploadSuccess) {
						LogWithPrefix(FtpTraceLevel.Info, "Failed to upload file.");

						if (attemptsLeft > 0)
							LogWithPrefix(FtpTraceLevel.Info, "Retrying to upload file.");
					}

					// If verification is needed, update the validated flag
					if (verifyOptions != FtpVerify.None) {
						verified = await VerifyTransferAsync(localPath, remotePath, token);
						LogWithPrefix(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpRemoteExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							// Force overwrite if a retry is required
							existsMode = FtpRemoteExists.Overwrite;
						}
					}
				}
			} while ((!uploadSuccess || !verified) && attemptsLeft > 0);

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				await DeleteFile(remotePath, token);
			}

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Uploaded file checksum value does not match local file");
			}

			// if uploaded OK then correctly return Skipped or Success, else return Failed
			return uploadSuccess && verified ? uploadStatus : FtpStatus.Failed;
		}

	}
}
