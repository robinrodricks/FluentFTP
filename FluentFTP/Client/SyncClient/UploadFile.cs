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

		protected FtpStatus UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpProgress metaProgress) {

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(UploadFile), new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				LogWithPrefix(FtpTraceLevel.Error, "File does not exist: " + localPath);
				return FtpStatus.Failed;
			}

			// If retries are allowed set the retry counter to the allowed count
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? Config.RetryAttempts : 1;

			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			var verified = true;
			FtpStatus uploadStatus;
			bool uploadSuccess;
			do {
				// write the file onto the server
				using (var fileStream = FtpFileStream.GetFileReadStream(this, localPath, false, 0)) {
					// Upload file
					uploadStatus = UploadFileInternal(fileStream, localPath, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress, metaProgress);
					uploadSuccess = uploadStatus.IsSuccess();
					attemptsLeft--;

					if (!uploadSuccess) {
						LogWithPrefix(FtpTraceLevel.Info, "Failed to upload file.");

						if (attemptsLeft > 0)
							LogWithPrefix(FtpTraceLevel.Info, "Retrying to upload file.");
					}

					// If verification is needed, update the validated flag
					if (uploadSuccess && verifyOptions != FtpVerify.None) {
						verified = VerifyTransfer(localPath, remotePath);
						LogWithPrefix(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpRemoteExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
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

	}
}
