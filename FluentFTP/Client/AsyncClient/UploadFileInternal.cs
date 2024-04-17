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
using System.Security.Authentication;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Upload the given stream to the server as a new file asynchronously. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		protected async Task<FtpStatus> UploadFileInternalAsync(Stream fileData, string localPath, string remotePath, bool createRemoteDir,
			FtpRemoteExists existsMode, bool fileExists, bool fileExistsKnown, IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress) {

			Stream upStream = null;

			// throw an error if need to resume uploading and cannot seek the local file stream
			if (!fileData.CanSeek && existsMode == FtpRemoteExists.Resume) {
				throw new ArgumentException("You have requested resuming file upload with FtpRemoteExists.Resume, but the local file stream cannot be seeked. Use another type of Stream or another existsMode.", nameof(fileData));
			}

			string remoteDir;
			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(remotePath);
			var autoRestore = Config.ShouldAutoRestore(remotePath);

			if (autoNav) {
				var temp = await GetAbsolutePathAsync(remotePath, token);
				remoteDir = temp.GetFtpDirectoryName();
				remotePath = Path.GetFileName(remotePath);

				pwdSave = await GetWorkingDirectory(token);
				if (pwdSave != remoteDir) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + remoteDir + "\"");

					if (createRemoteDir) {
						if (!await DirectoryExists(remoteDir, token)) {
							await CreateDirectory(remoteDir, token);
						}
					}

					await SetWorkingDirectory(remoteDir, token);
				}
			}

			try {
				long localPosition = 0, remotePosition = 0, remoteFileLen = -1;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.NoCheck) {
				}
				else if (existsMode is FtpRemoteExists.ResumeNoCheck or FtpRemoteExists.AddToEndNoCheck) {

					// start from the end of the remote file, or if failed to read the length then start from the beginning
					remoteFileLen = remotePosition = await GetFileSize(remotePath, 0, token);

					// calculate the local position for appending / resuming
					localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);

				}
				else {

					// check if the remote file exists
					if (!fileExistsKnown) {
						fileExists = await FileExists(remotePath, token);
					}

					if (existsMode == FtpRemoteExists.Skip) {

						if (fileExists) {
							LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists on server (Remote: " + remotePath + ", Local: " + localPath + ")");

							// Fix #413 - progress callback isn't called if the file has already been uploaded to the server
							// send progress reports for skipped files
							progress?.Report(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

							return FtpStatus.Skipped;
						}

					}
					else if (existsMode == FtpRemoteExists.Overwrite) {

						// delete the remote file if it exists and we need to overwrite
						if (fileExists) {
							await DeleteFile(remotePath, token);
						}

					}
					else if (existsMode is FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd) {
						if (fileExists) {

							// start from the end of the remote file, or if failed to read the length then start from the beginning
							remoteFileLen = remotePosition = await GetFileSize(remotePath, 0, token);

							// calculate the local position for appending / resuming
							localPosition = CalculateAppendLocalPosition(remotePath, existsMode, remotePosition);
						}

					}

				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					var dirname = remotePath.GetFtpDirectoryName();
					if (!await DirectoryExists(dirname, token)) {
						await CreateDirectory(dirname, token);
					}
				}

				// FIX #213 : Do not change Stream.Position if not supported
				if (fileData.CanSeek) {
					try {
						// seek to required offset
						fileData.Position = localPosition;
					}
					catch {
					}
				}

				long localFileLen;
				// calc local file len - local file might be a stream kind that cannot get length
				try {
					localFileLen = fileData.Length;
				}
				catch (NotSupportedException) {
					localFileLen = 0;
				}

				// skip uploading if the mode is resume and the local and remote file have the same length
				if (existsMode is FtpRemoteExists.Resume or FtpRemoteExists.ResumeNoCheck &&
					(localFileLen == remoteFileLen)) {
					LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully uploaded (Remote: " + remotePath + ", Local: " + localPath + ")");

					// send progress reports for skipped files
					progress?.Report(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

					return FtpStatus.Skipped;
				}

				// open a file connection
				if (remotePosition == 0 && existsMode is not (FtpRemoteExists.ResumeNoCheck or FtpRemoteExists.AddToEndNoCheck)) {
					upStream = await OpenWriteInternal(remotePath, Config.UploadDataType, remoteFileLen, false, token);
				}
				else {
					upStream = await OpenAppendInternal(remotePath, Config.UploadDataType, remoteFileLen, false, token);
				}

				// calculate chunk size and rate limiting
				const int rateControlResolution = 100;
				long rateLimitBytes = Config.UploadRateLimit != 0 ? (long)Config.UploadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// calc desired length based on the mode (if need to append to the end of remote file, length is sum of local+remote)
				var remoteFileDesiredLen = (existsMode is FtpRemoteExists.AddToEnd or FtpRemoteExists.AddToEndNoCheck) ?
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
				catch {
				}

				Status.DaemonAnyNoops = false;

				// loop till entire file uploaded
				while (localFileLen == 0 || localPosition < localFileLen) {
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
						var resumeResult = await ResumeUploadAsync(remotePath, upStream, remotePosition, ex, token);
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
					catch (TimeoutException) {
						// fix: attempting to upload data after we reached the end of the stream
						// often throws a timeout exception, so we silently absorb that here
						if (localFileLen > 0 && localPosition >= localFileLen) {
							break;
						}
						else {
							sw.Stop();
							throw;
						}
					}
				}

				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				sw.Stop();

				string bps;
				try {
					bps = (upStream.Position / sw.ElapsedMilliseconds * 1000L).FileSizeToString();
				}
				catch {
					bps = "0";
				}
				LogWithPrefix(FtpTraceLevel.Verbose, "Uploaded " + upStream.Position + " bytes (" + sw.Elapsed.ToShortString() + ", " + bps + "/s)");

				// send progress reports
				progress?.Report(new FtpProgress(100.0, upStream.Length, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

				// disconnect FTP stream before exiting
				await ((FtpDataStream)upStream).DisposeAsync();

				// listen for a success/failure reply or out of band data (like NOOP responses)
				// GetReply(true) means: Exhaust any NOOP responses
				FtpReply status = await ((IInternalFtpClient)this).GetReplyInternal(token, LastCommandExecuted, Status.DaemonAnyNoops);

				// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
				// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
				if (status.Code != null && !status.Success) {
					return FtpStatus.Failed;
				}

				if (autoRestore) {
					if (pwdSave != await GetWorkingDirectory(token)) {
						LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
						await SetWorkingDirectory(pwdSave, token);
					}
				}

				return FtpStatus.Success;
			}
			catch (AuthenticationException) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Authentication error encountered uploading file");

				FtpReply reply = await ((IInternalFtpClient)this).GetReplyInternal(token, LastCommandExecuted, false, -1); // no exhaustNoop, but non-blocking
				if (!reply.Success) {
					throw new FtpCommandException(reply);
				}
				throw;
			}
			catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null) {
						await ((FtpDataStream)upStream).DisposeAsync();
					}
				}
				catch (Exception) {
				}

				LogWithPrefix(FtpTraceLevel.Verbose, "Error encountered uploading file");

				if (ex1 is IOException) {
					LogWithPrefix(FtpTraceLevel.Verbose, "IOException for file " + localPath, ex1);
					return FtpStatus.Failed;
				}

				if (ex1 is OperationCanceledException) {
					LogWithPrefix(FtpTraceLevel.Info, "Upload cancellation requested");
					throw;
				}

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

		/// <summary>
		/// Setup a resume on failure of upload
		/// </summary>
		protected async Task<Tuple<bool, Stream>> ResumeUploadAsync(string remotePath, Stream upStream, long remotePosition, IOException ex, CancellationToken token = default) {
			try {
				// if resume possible
				if (ex.IsResumeAllowed()) {
					// dispose the old bugged out stream
					await ((FtpDataStream)upStream).DisposeAsync();
					LogWithPrefix(FtpTraceLevel.Info, "Attempting upload resume at position " + remotePosition);

					// create and return a new stream starting at the current remotePosition
					var returnStream = await OpenWriteInternal(remotePath, Config.UploadDataType, 0, false, token);
					returnStream.Position = remotePosition;
					return Tuple.Create(true, returnStream);
				}

				// resume not allowed
				return Tuple.Create(false, (Stream)null);
			}
			catch (Exception resumeEx) {
				throw new AggregateException("Additional error occured while trying to resume uploading the file '" + remotePath + "' at position " + remotePosition, new Exception[] { ex, resumeEx });
			}
		}

	}
}
