using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Proxy.SyncProxy;
using System.Security.Authentication;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		protected bool DownloadFileInternal(string localPath, string remotePath, Stream outStream, long restartPosition,
			Action<FtpProgress> progress, FtpProgress metaProgress, long knownFileSize, bool isAppend, long stopPosition) {

			Stream downStream = null;
			var disposeOutStream = false;

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
					SetWorkingDirectory(remoteDir);
				}
			}

			try {
				// get file size if progress requested
				long fileLen = 0;

				if (progress != null) {
					fileLen = knownFileSize > 0 ? knownFileSize : GetFileSize(remotePath);
				}

				// #690: honor the "stop position" if provided and if less than the file length
				if (stopPosition > 0 && (fileLen <= 0 || stopPosition < fileLen)) {
					fileLen = stopPosition;
				}

				// open the file for reading
				downStream = OpenReadInternal(remotePath, Config.DownloadDataType, fileLen, restartPosition, false);

				// Fix: workaround for SOCKS4 and SOCKS4a proxies
				if (restartPosition == 0) {
					if (this is FtpClientSocks4Proxy || this is FtpClientSocks4aProxy) {

						// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
						// we need to skip them otherwise they will be downloaded to the file
						// moreover, these bytes cause "Failed to get the EPSV port" error
						downStream.Read(new byte[6], 0, 6);
					}
				}

				// if the server has not provided a length for this file or
				// if the mode is ASCII or
				// if the server is IBM z/OS
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = (fileLen <= 0) ||
								(Config.DownloadDataType == FtpDataType.ASCII && stopPosition == 0) ||
								(ServerHandler != null && ServerHandler.AlwaysReadToEnd(remotePath));

				const int rateControlResolution = 100;
				var rateLimitBytes = Config.DownloadRateLimit != 0 ? (long)Config.DownloadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				var buffer = new byte[chunkSize];
				var offset = restartPosition;
				long bytesProcessed = 0;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var earlySuccess = false;

				Status.DaemonAnyNoops = false;

				// Fix #554: ability to download zero-byte files
				if (Config.DownloadZeroByteFiles && outStream == null && localPath != null) {
					outStream = FtpFileStream.GetFileWriteStream(this, localPath, false, 0, knownFileSize, isAppend, restartPosition);
					disposeOutStream = true;
				}

				// loop till entire file downloaded
				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						int bytesToReadInBuffer = fileLen > 0 && buffer.Length > fileLen - offset ? (int)(fileLen - offset) : buffer.Length;

						sw.Start();
						while (bytesToReadInBuffer > 0 && (readBytes = downStream.Read(buffer, 0, bytesToReadInBuffer)) > 0 && (offset < fileLen || readToEnd)) {

							// Fix #552: only create outstream when first bytes downloaded
							if (outStream == null && localPath != null) {
								outStream = FtpFileStream.GetFileWriteStream(this, localPath, false, 0, knownFileSize, isAppend, restartPosition);
								disposeOutStream = true;
							}

							// write chunk to output stream
							outStream.Write(buffer, 0, readBytes);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;
							bytesToReadInBuffer = fileLen > 0 && buffer.Length > fileLen - offset ? (int)(fileLen - offset) : buffer.Length;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen - restartPosition, offset, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
							}

							// honor the rate limit
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

						if (stopPosition != 0 && offset >= fileLen && readToEnd != true) {
							earlySuccess = true; // We should stop here
							break;
						}
						// if we reach here means EOF encountered
						// stop if we are in "read until EOF" mode
						if (offset == fileLen || readToEnd) {
							break;
						}

						// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
						throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
					}
					catch (IOException ex) {
						LogWithPrefix(FtpTraceLevel.Verbose, "IOException in DownloadFileInternal", ex);

						FtpReply exStatus = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted + ", after IOException", Status.DaemonAnyNoops, 10000);
						if (exStatus.Code == "226") {
							sw.Stop();
							earlySuccess = true;
							break;
						}

						// resume if server disconnected midway, or throw if there is an exception doing that as well
						if (!ResumeDownload(remotePath, ref downStream, offset, ex)) {
							sw.Stop();
							throw;
						}
					}
					catch (TimeoutException) {

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

				string bps;
				try {
					bps = (bytesProcessed / sw.ElapsedMilliseconds * 1000L).FileSizeToString();
				}
				catch {
					bps = "0";
				}
				LogWithPrefix(FtpTraceLevel.Verbose, "Downloaded " + bytesProcessed + " bytes (" + sw.Elapsed.ToShortString() + ", " + bps + "/s)");

				// disconnect FTP streams before exiting
				outStream?.Flush();

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					outStream?.Dispose();
					disposeOutStream = false;
				}

				((FtpDataStream)downStream).Dispose();

				if (earlySuccess) {
					return true;
				}

				// listen for a success/failure reply or out of band data (like NOOP responses)
				// GetReply(true) means: Exhaust any NOOP responses
				FtpReply status = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted, Status.DaemonAnyNoops);

				// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
				// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
				if (status.Code != null && !status.Success) {
					return false;
				}

				if (autoRestore) {
					if (pwdSave != GetWorkingDirectory()) {
						LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
						SetWorkingDirectory(pwdSave);
					}
				}

				return true;
			}
			catch (AuthenticationException) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Authentication error encountered downloading file");

				FtpReply reply = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted, false, -1); // no exhaustNoop, but non-blocking
				if (!reply.Success) {
					throw new FtpCommandException(reply);
				}
				throw;
			}
			catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (downStream != null) {
						((FtpDataStream)downStream).Dispose();
					}
				}
				catch (Exception) {
				}

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					try {
						outStream?.Dispose();
						disposeOutStream = false;
					}
					catch (Exception) {
					}
				}

				LogWithPrefix(FtpTraceLevel.Verbose, "Error encountered downloading file");

				if (ex1 is IOException) {
					LogWithPrefix(FtpTraceLevel.Verbose, "IOException for file " + localPath, ex1);
					return false;
				}

				// Fix #1121: detect "file does not exist" exceptions and throw FtpMissingObjectException
				if (ex1.Message.ContainsAnyCI(ServerStringModule.fileNotFound)) {
					LogWithPrefix(FtpTraceLevel.Error, "File does not exist", ex1);
					throw new FtpMissingObjectException("Cannot download non-existant file: " + remotePath, ex1, remotePath, FtpObjectType.File);
				}

				// catch errors during download
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

		/// <summary>
		/// Setup a resume on failure of download
		/// </summary>
		protected bool ResumeDownload(string remotePath, ref Stream downStream, long offset, IOException ex) {
			try {
				// if resume possible
				if (ex.IsResumeAllowed()) {
					// dispose the old bugged out stream
					((FtpDataStream)downStream).Dispose();
					LogWithPrefix(FtpTraceLevel.Info, "Attempting download resume from offset " + offset);

					// create and return a new stream starting at the current remotePosition
					downStream = OpenReadInternal(remotePath, Config.DownloadDataType, 0, offset, false);

					// resume not allowed
					return true;
				}
			}
			catch (Exception resumeEx) {
				throw new AggregateException("Additional error occured while trying to resume downloading the file '" + remotePath + "' from offset " + offset, new Exception[] { ex, resumeEx });
			}

			return false;
		}
	}
}
