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
		/// Upload the given stream to the server as a new file. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		protected FtpStatus UploadFileInternal(Stream fileData, string localPath, string remotePath, bool createRemoteDir,
			FtpRemoteExists existsMode, bool fileExists, bool fileExistsKnown, Action<FtpProgress> progress, FtpProgress metaProgress) {

			Stream upStream = null;

			// throw an error if need to resume uploading and cannot seek the local file stream
			if (!fileData.CanSeek && existsMode == FtpRemoteExists.Resume) {
				throw new ArgumentException("You have requested resuming file upload with FtpRemoteExists.Resume, but the local file stream cannot be seeked. Use another type of Stream or another existsMode.", nameof(fileData));
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
							LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Skip is enabled and file already exists on server (Remote: " + remotePath + ", Local: " + localPath + ")");

							// Fix #413 - progress callback isn't called if the file has already been uploaded to the server
							// send progress reports for skipped files
							progress?.Invoke(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

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
					LogWithPrefix(FtpTraceLevel.Info, "Skipping file because Resume is enabled and file is fully uploaded (Remote: " + remotePath + ", Local: " + localPath + ")");

					// send progress reports for skipped files
					progress?.Invoke(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

					return FtpStatus.Skipped;
				}

				// open a file connection
				if (remotePosition == 0 && existsMode != FtpRemoteExists.ResumeNoCheck && existsMode != FtpRemoteExists.AddToEndNoCheck) {
					upStream = OpenWrite(remotePath, Config.UploadDataType, remoteFileLen);
				}
				else {
					upStream = OpenAppend(remotePath, Config.UploadDataType, remoteFileLen);
				}

				// calculate chunk size and rate limiting
				const int rateControlResolution = 100;
				var rateLimitBytes = Config.UploadRateLimit != 0 ? (long)Config.UploadRateLimit * 1024 : 0;
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
							anyNoop = Noop() || anyNoop;

							// honor the speed limit
							var swTime = sw.ElapsedMilliseconds;
							if (rateLimitBytes > 0) {
								var timeShouldTake = limitCheckBytes * 1000 / rateLimitBytes;
								if (timeShouldTake > swTime) {
									Thread.Sleep((int)(timeShouldTake - swTime));
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
				progress?.Invoke(new FtpProgress(100.0, upStream.Length, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				try {
					while (true) {
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
					upStream?.Dispose();
				}
				catch (Exception) {
				}

				if (ex1 is IOException) {
					LogWithPrefix(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return FtpStatus.Failed;
				}

				// catch errors during upload, 
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

		protected bool ResumeUpload(string remotePath, ref Stream upStream, long remotePosition, IOException ex) {

			// resume not allowed
			return false;
		}


	}
}
