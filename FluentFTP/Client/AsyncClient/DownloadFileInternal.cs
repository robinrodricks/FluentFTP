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
using FluentFTP.Proxy.AsyncProxy;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		protected async Task<bool> DownloadFileInternalAsync(string localPath, string remotePath, Stream outStream, long restartPosition,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress, long knownFileSize, bool isAppend) {

			Stream downStream = null;
			var disposeOutStream = false;

			try {
				// get file size if progress requested
				long fileLen = 0;

				if (progress != null) {
					fileLen = knownFileSize > 0 ? knownFileSize : await GetFileSize(remotePath, -1, token);
				}

				// open the file for reading
				downStream = await OpenRead(remotePath, Config.DownloadDataType, restartPosition, fileLen, token);
				// workaround for SOCKS4 and SOCKS4a proxies
				if (restartPosition == 0) {
					if (this is AsyncFtpClientSocks4Proxy || this is AsyncFtpClientSocks4aProxy) {
						// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
						// we need to skip them otherwise they will be downloaded to the file
						// moreover, these bytes cause "Failed to get the EPSV port" error
						await downStream.ReadAsync(new byte[6], 0, 6, token);
					}
				}

				// if the server has not provided a length for this file or
				// if the mode is ASCII or
				// if the server is IBM z/OS
				// we read until EOF instead of reading a specific number of bytes
				var readToEnd = (fileLen <= 0) ||
								(Config.DownloadDataType == FtpDataType.ASCII) ||
								(ServerHandler != null && ServerHandler.AlwaysReadToEnd(remotePath));

				const int rateControlResolution = 100;
				var rateLimitBytes = Config.DownloadRateLimit != 0 ? (long)Config.DownloadRateLimit * 1024 : 0;
				var chunkSize = CalculateTransferChunkSize(rateLimitBytes, rateControlResolution);

				// loop till entire file downloaded
				var buffer = new byte[chunkSize];
				var offset = restartPosition;

				var transferStarted = DateTime.Now;
				var sw = new Stopwatch();

				var anyNoop = false;

				// Fix #554: ability to download zero-byte files
				if (Config.DownloadZeroByteFiles && outStream == null && localPath != null) {
					outStream = FtpFileStream.GetFileWriteStream(this, localPath, true, 0, knownFileSize, isAppend, restartPosition);
					disposeOutStream = true;
				}

				while (offset < fileLen || readToEnd) {
					try {
						// read a chunk of bytes from the FTP stream
						var readBytes = 1;
						long limitCheckBytes = 0;
						long bytesProcessed = 0;

						sw.Start();
						while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {

							// Fix #552: only create outstream when first bytes downloaded
							if (outStream == null && localPath != null) {
								outStream = FtpFileStream.GetFileWriteStream(this, localPath, true, 0, knownFileSize, isAppend, restartPosition);
								disposeOutStream = true;
							}

							// write chunk to output stream
							await outStream.WriteAsync(buffer, 0, readBytes, token);
							offset += readBytes;
							bytesProcessed += readBytes;
							limitCheckBytes += readBytes;

							// send progress reports
							if (progress != null) {
								ReportProgress(progress, fileLen, offset, bytesProcessed, DateTime.Now - transferStarted, localPath, remotePath, metaProgress);
							}

							// Fix #387: keep alive with NOOP as configured and needed
							anyNoop = await NoopAsync(token) || anyNoop;

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
						var resumeResult = await ResumeDownloadAsync(remotePath, downStream, offset, ex, token);
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

				// disconnect FTP stream before exiting
				if (outStream != null) {
					await outStream.FlushAsync(token);
				}
				downStream.Dispose();

				// Fix #552: close the filestream if it was created in this method
				if (disposeOutStream) {
					outStream?.Dispose();
					disposeOutStream = false;
				}

				// send progress reports
				progress?.Report(new FtpProgress(100.0, offset, 0, TimeSpan.Zero, localPath, remotePath, metaProgress));

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply

				// Quick dirty fix for recalcitrant servers. This will cause them to hickup the rest of the responses
				m_stream.WriteLine(Encoding, "NOOP");

				try {
					while (true) {
						FtpReply status = await GetReply(token);

						// Fix #387: exhaust any NOOP responses (not guaranteed during file transfers)
						if (anyNoop && status.Message != null && status.Message.Contains("NOOP")) {
							continue;
						}

						// Fix #353: if server sends 550 or 5xx the transfer was received but could not be confirmed by the server
						// Fix #509: if server sends 450 or 4xx the transfer was aborted or failed midway
						if (status.Code != null && !status.Success) {
							return false;
						}

						// Fix #387: exhaust any NOOP responses also after "226 Transfer complete."
						if (anyNoop) {
							await ReadStaleDataAsync(false, true, "after download", token);
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
					downStream?.Dispose();
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

				if (ex1 is IOException) {
					LogWithPrefix(FtpTraceLevel.Verbose, "IOException for file " + localPath + " : " + ex1.Message);
					return false;
				}

				if (ex1 is OperationCanceledException) {
					LogWithPrefix(FtpTraceLevel.Info, "Download cancellation requested");
					throw;
				}

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.IsKnownError(ServerStringModule.fileNotFound)) {
					LogWithPrefix(FtpTraceLevel.Error, "File does not exist: " + ex1.Message);
					return false;
				}

				// catch errors during download
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}
		protected async Task<Tuple<bool, Stream>> ResumeDownloadAsync(string remotePath, Stream downStream, long offset, IOException ex, CancellationToken token = default) {
			if (ex.IsResumeAllowed()) {
				downStream.Dispose();

				return Tuple.Create(true, await OpenRead(remotePath, Config.DownloadDataType, offset, token: token));
			}

			return Tuple.Create(false, (Stream)null);
		}

	}
}
