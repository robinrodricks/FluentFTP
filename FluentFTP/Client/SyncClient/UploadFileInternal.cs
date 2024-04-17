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

			string remoteDir;
			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(remotePath);
			var autoRestore = Config.ShouldAutoRestore(remotePath);

			if (autoNav) {
				var temp = GetAbsolutePath(remotePath);
				remoteDir = temp.GetFtpDirectoryName();
				remotePath = Path.GetFileName(remotePath);

				pwdSave = GetWorkingDirectory();
				if (pwdSave != remoteDir) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + remoteDir + "\"");

					if (createRemoteDir) {
						if (!DirectoryExists(remoteDir)) {
							CreateDirectory(remoteDir);
						}
					}

					SetWorkingDirectory(remoteDir);
				}
			}

			try {
				long localPosition = 0, remotePosition = 0, remoteFileLen = -1;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.NoCheck) {
				}
				else if (existsMode is FtpRemoteExists.ResumeNoCheck or FtpRemoteExists.AddToEndNoCheck) {

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
					else if (existsMode is FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd) {
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
					progress?.Invoke(new FtpProgress(100.0, localPosition, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

					return FtpStatus.Skipped;
				}

				// open a file connection
				if (remotePosition == 0 && existsMode is not (FtpRemoteExists.ResumeNoCheck or FtpRemoteExists.AddToEndNoCheck)) {
					upStream = OpenWriteInternal(remotePath, Config.UploadDataType, remoteFileLen, false);
				}
				else {
					upStream = OpenAppendInternal(remotePath, Config.UploadDataType, remoteFileLen, false);
				}

				// calculate chunk size and rate limiting
				const int rateControlResolution = 100;
				var rateLimitBytes = Config.UploadRateLimit != 0 ? (long)Config.UploadRateLimit * 1024 : 0;
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
				progress?.Invoke(new FtpProgress(100.0, upStream.Length, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));

				// disconnect FTP stream before exiting
				((FtpDataStream)upStream).Dispose();

				// listen for a success/failure reply or out of band data (like NOOP responses)
				// GetReply(true) means: Exhaust any NOOP responses
				FtpReply status = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted, Status.DaemonAnyNoops);

				// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
				// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
				if (status.Code != null && !status.Success) {
					return FtpStatus.Failed;
				}

				if (autoRestore) {
					if (pwdSave != GetWorkingDirectory()) {
						LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
						SetWorkingDirectory(pwdSave);
					}
				}

				return FtpStatus.Success;
			}
			catch (AuthenticationException) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Authentication error encountered uploading file");

				FtpReply reply = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted, false, -1); // no exhaustNoop, but non-blocking
				if (!reply.Success) {
					throw new FtpCommandException(reply);
				}
				throw;
			}
			catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null) {
						((FtpDataStream)upStream).Dispose();
					}
				}
				catch (Exception) {
				}

				LogWithPrefix(FtpTraceLevel.Verbose, "Error encountered uploading file");

				if (ex1 is IOException) {
					LogWithPrefix(FtpTraceLevel.Verbose, "IOException for file " + localPath, ex1);
					return FtpStatus.Failed;
				}

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

		/// <summary>
		/// Setup a resume on an upload failure
		/// </summary>
		protected bool ResumeUpload(string remotePath, ref Stream upStream, long remotePosition, IOException ex) {
			try {
				// if resume possible
				if (ex.IsResumeAllowed()) {
					// dispose the old bugged out stream
					((FtpDataStream)upStream).Dispose();
					LogWithPrefix(FtpTraceLevel.Info, "Attempting upload resume at position " + remotePosition);

					// create and return a new stream starting at the current remotePosition
					upStream = OpenAppendInternal(remotePath, Config.UploadDataType, 0, false);
					upStream.Position = remotePosition;
					return true;
				}

				// resume not allowed
				return false;
			}
			catch (Exception resumeEx) {
				throw new AggregateException("Additional error occured while trying to resume uploading the file '" + remotePath + "' at position " + remotePosition, new Exception[] { ex, resumeEx });
			}
		}


	}
}
