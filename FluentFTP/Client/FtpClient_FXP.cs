using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
using FluentFTP.Servers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Rules;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif
namespace FluentFTP
{
	/// <summary>
	/// A connection to a single FTP server. Interacts with any FTP/FTPS server and provides a high-level and low-level API to work with files and folders.
	/// 
	/// Debugging problems with FTP is much easier when you enable logging. See the FAQ on our Github project page for more info.
	/// </summary>
	public partial class FtpClient : IDisposable
	{

		/// <summary>
		/// Opens a FXP PASV connection between the source and the remote (aka destination) ftp server
		/// </summary>
		/// <param name="remoteClient">FtpClient instance of the remote / destination FTP Server</param>
		/// <returns>A data stream ready to be used</returns>
		private FtpFxpSession OpenPassiveFXPConnection(FtpClient remoteClient)
		{
			FtpReply reply;
			Match m;
			FtpClient sourceClient = null;
			FtpClient destinationClient = null;

			if (m_threadSafeDataChannels)
			{
				sourceClient = CloneConnection();
				sourceClient.CopyStateFlags(this);
				sourceClient.Connect();
				sourceClient.SetWorkingDirectory(GetWorkingDirectory());
			}
			else
			{
				sourceClient = this;
			}

			if (remoteClient.EnableThreadSafeDataConnections)
			{
				destinationClient = remoteClient.CloneConnection();
				destinationClient.CopyStateFlags(remoteClient);
				destinationClient.Connect();
				destinationClient.SetWorkingDirectory(destinationClient.GetWorkingDirectory());
			}
			else
			{
				destinationClient = remoteClient;
			}

			sourceClient.SetDataType(sourceClient.FXPDataType);
			destinationClient.SetDataType(destinationClient.FXPDataType);

			// send PASV command to destination FTP server to get passive port to be used from source FTP server
			if (!(reply = destinationClient.Execute("PASV")).Success)
			{
				throw new FtpCommandException(reply);
			}

			m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

			if (!m.Success || m.Groups.Count != 7)
			{
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// Instruct source server to open a connection to the destination Server

			if (!(reply = sourceClient.Execute($"PORT {m.Value}")).Success)
			{
				throw new FtpCommandException(reply);
			}

			return new FtpFxpSession { sourceFtpClient = sourceClient, destinationFtpClient = destinationClient };
		}

		/// <summary>
		/// 'Copys' a file from the source FTP Server to the remote / destination FTP Server via the FXP protocol
		/// </summary>
		public bool FXPFileCopyInternal(string sourcePath, FtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			Action<FtpProgress> progress, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;
			long fileSize = 0;

			var ftpFxpSession = OpenPassiveFXPConnection(remoteClient);

			if (ftpFxpSession != null)
			{

				ftpFxpSession.sourceFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;
				ftpFxpSession.destinationFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;


				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.AppendNoCheck)
				{
					offset = remoteClient.GetFileSize(remotePath);
					if (offset == -1)
					{
						offset = 0; // start from the beginning
					}
				}
				else
				{
					fileExists = remoteClient.FileExists(remotePath);

					switch (existsMode)
					{
						case FtpRemoteExists.Skip:

							if (fileExists)
							{
								LogStatus(FtpTraceLevel.Info, "Skip is selected => Destination file exists => skipping");

								//Fix #413 - progress callback isn't called if the file has already been uploaded to the server
								//send progress reports
								if (progress != null)
								{
									progress(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), sourcePath, remotePath, metaProgress));
								}

								return true;
							}

							break;

						case FtpRemoteExists.Overwrite:

							if (fileExists)
							{
								remoteClient.DeleteFile(remotePath);
							}

							break;

						case FtpRemoteExists.Append:

							if (fileExists)
							{
								offset = remoteClient.GetFileSize(remotePath);
								if (offset == -1)
								{
									offset = 0; // start from the beginning
								}
							}

							break;
					}

				}

				fileSize = GetFileSize(sourcePath);

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists)
				{
					var dirname = remotePath.GetFtpDirectoryName();
					if (!remoteClient.DirectoryExists(dirname))
					{
						 CreateDirectory(dirname);
					}
				}

				if (offset == 0 && existsMode != FtpRemoteExists.AppendNoCheck)
				{
					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = ftpFxpSession.sourceFtpClient.Execute($"RETR {sourcePath}")).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to store the file
					if (!(reply = ftpFxpSession.destinationFtpClient.Execute($"STOR {remotePath}")).Success)
					{
						throw new FtpCommandException(reply);
					}
				}
				else
				{
					//tell source server to restart / resume
					if (!(reply = ftpFxpSession.sourceFtpClient.Execute($"REST {offset}")).Success)
					{
						throw new FtpCommandException(reply);
					}

					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = ftpFxpSession.sourceFtpClient.Execute($"RETR {sourcePath}")).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to append the file
					if (!(reply = ftpFxpSession.destinationFtpClient.Execute($"APPE {remotePath}")).Success)
					{
						throw new FtpCommandException(reply);
					}
				}

				var transferStarted = DateTime.Now;
				long lastSize = 0;

				var sourceFXPTransferReply = ftpFxpSession.sourceFtpClient.GetReply();
				var destinationFXPTransferReply = ftpFxpSession.destinationFtpClient.GetReply();

				while (!sourceFXPTransferReply.Success || !destinationFXPTransferReply.Success)
				{

					if (remoteClient.EnableThreadSafeDataConnections)
					{
						FtpTrace.Write(FtpTraceLevel.Info, "reporting progress");
						// send progress reports
						if (progress != null && fileSize != -1)
						{
							offset = remoteClient.GetFileSize(remotePath);

							if (offset != -1 && lastSize <= offset)
							{
								long bytesProcessed = offset - lastSize;
								lastSize = offset;
								ReportProgress(progress, fileSize, offset, bytesProcessed, DateTime.Now - transferStarted, sourcePath, remotePath, metaProgress);
							}
						}
					}
#if CORE14
					Task.Delay(1000);
#else
					Thread.Sleep(1000);
#endif
				}

				FtpTrace.WriteLine(FtpTraceLevel.Info, $"FXP transfer of file {sourcePath} has completed");

				Noop();
				remoteClient.Noop();

				return true;
			}
			else
			{
				FtpTrace.WriteLine(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}
		}

		/// <summary>
		/// Transfer the specified file from the srouce FTP Server to the remote / destination FTP Server using the FXP protocol.
		/// High-level API that takes care of various edge cases internally.
		/// </summary>
		/// <param name="sourcePath">The full or relative path to the file on the source FTP Server</param>
		/// <param name="remotePath">The full or relative path to destination file on the remote FTP Server</param>
		/// <param name="createRemoteDir">Indicates if the folder should be created on the remote FTP Server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// Returns a FtpStatus indicating if the file was transfered.
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>

		public FtpStatus TransferFile(string sourcePath, FtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, Action<FtpProgress> progress = null, FtpProgress metaProgress = null)
		{

			LogFunc("FXPFileCopy", new object[] { sourcePath, remoteClient, remotePath, FXPDataType });

			#region "Verify and Check vars and prequisites"

			if (remoteClient is null)
			{
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "remoteClient");
			}

			if (sourcePath.IsBlank())
			{
				throw new ArgumentNullException("FtpListItem must be specified!", "sourceFtpFileItem");
			}

			if (remotePath.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			if (!remoteClient.IsConnected)
			{
				throw new FluentFTP.FtpException("The connection must be open before a transfer between servers can be intitiated");
			}

			if (!this.IsConnected)
			{
				throw new FluentFTP.FtpException("The source FXP FtpClient must be open and connected before a transfer between servers can be intitiated");
			}

			if (!FileExists(sourcePath))
			{
				throw new FluentFTP.FtpException(string.Format("Source File {0} cannot be found or does not exists!", sourcePath));
			}

			#endregion

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do
			{

				fxpSuccess = FXPFileCopyInternal(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, metaProgress is null ? new FtpProgress(1, 0) : metaProgress);
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None)
				{
					verified = VerifyFXPTransfer(sourcePath, remoteClient, remotePath);
					LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0)
					{
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpRemoteExists.Append ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}

#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete))
			{
				remoteClient.DeleteFile(remotePath);
			}

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw))
			{
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return fxpSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;

		}

		/// <summary>
		/// Transfer the specified directory from the source FTP Server onto the remote FTP Server using the FXP protocol.
		/// In Mirror mode, we will transfer missing files, and delete any extra files from the remote server if not present on the soruce FTP Server. This is very useful when creating an exact local backup of an FTP directory.
		/// In Update mode, we will only transfer missing files and preserve any extra files on the remote FTP Server. This is useful when you want to simply transfer missing files from an FTP directory.
		/// Only transfer the files and folders matching all the rules provided, if any.
		/// All exceptions during transfer are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="sourceFolder">The full or relative path to the folder on the source FTP Server. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteClient">FtpClient instance of the remote / destination FTP Server</param>
		/// <param name="remoteFolder">The full or relative path to destination folder on the remote FTP Server</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public List<FtpResult> TransferDirectory(string sourceFolder, FtpClient remoteClient, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
	FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null)
		{

			if (sourceFolder.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "sourceFolder");
			}

			if (remoteFolder.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(TransferDirectory), new object[] { sourceFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			sourceFolder = sourceFolder.EnsurePostfix("/");

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the dir does not exist, fail fast
			if (!DirectoryExists(sourceFolder))
			{
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!remoteClient.DirectoryExists(remoteFolder))
			{
				remoteClient.CreateDirectory(remoteFolder);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = GetListing(sourceFolder, FtpListOption.Recursive).Where(x => x.Type == FtpFileSystemObjectType.Directory).Select(x => x.FullName).ToArray();

			// get all the already existing files
			var remoteListing = checkFileExistence ? remoteClient.GetListing(remoteFolder, FtpListOption.Recursive) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload = GetSubDirectoriesToTransfer(sourceFolder, remoteFolder, rules, results, dirListing);
			TransferSubDirectories(dirsToUpload,remoteClient);

			// get all the files in the local directory
			var fileListing = GetListing(sourceFolder, FtpListOption.Recursive).Where(x => x.Type == FtpFileSystemObjectType.File).Select(x => x.FullName).ToArray();

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToTransfer(sourceFolder, remoteFolder, rules, results, shouldExist, fileListing);
			TransferServerFiles(filesToUpload, remoteClient, existsMode, verifyOptions, progress, remoteListing);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			// DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}

		private List<FtpResult> GetSubDirectoriesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, string[] dirListing)
		{

			var dirsToTransfer = new List<FtpResult>();

			foreach (var sourceFile in dirListing)
			{

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "").EnsurePostfix("/");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult
				{
					Type = FtpFileSystemObjectType.Directory,
					Size = 0,
					Name = sourceFile.GetFtpDirectoryName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile,
					IsDownload = false,
				};

				// record the folder
				results.Add(result);

				// if the folder passes all rules
				if (rules != null && rules.Count > 0)
				{
					var passes = FtpRule.IsAllAllowed(rules, result.ToListItem(true));
					if (!passes)
					{

						// mark that the file was skipped due to a rule
						result.IsSkipped = true;
						result.IsSkippedByRule = true;

						// skip uploading the file
						continue;
					}
				}

				dirsToTransfer.Add(result);
			}

			return dirsToTransfer;
		}

		private List<FtpResult> GetFilesToTransfer(string sourceFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing)
		{

			var filesToTransfer = new List<FtpResult>();

			foreach (var sourceFile in fileListing)
			{

				// calculate the local path
				var relativePath = sourceFile.Replace(sourceFolder, "");
				var remoteFile = remoteFolder + relativePath;

				// create the result object
				var result = new FtpResult
				{
					Type = FtpFileSystemObjectType.File,
					Size = GetFileSize(sourceFile),
					Name = sourceFile.GetFtpFileName(),
					RemotePath = remoteFile,
					LocalPath = sourceFile
				};

				// record the file
				results.Add(result);

				// if the file passes all rules
				if (rules != null && rules.Count > 0)
				{
					var passes = FtpRule.IsAllAllowed(rules, result.ToListItem(true));
					if (!passes)
					{

						LogStatus(FtpTraceLevel.Info, "Skipped file due to rule: " + result.LocalPath);

						// mark that the file was skipped due to a rule
						result.IsSkipped = true;
						result.IsSkippedByRule = true;

						// skip uploading the file
						continue;
					}
				}

				// record that this file should exist
				shouldExist.Add(remoteFile.ToLowerInvariant(), true);

				// absorb errors
				filesToTransfer.Add(result);
			}

			return filesToTransfer;
		}

		private void TransferServerFiles(List<FtpResult> filesToTransfer, FtpClient remoteClient, FtpRemoteExists existsMode, FtpVerify verifyOptions, Action<FtpProgress> progress, FtpListItem[] remoteListing)
		{

			LogFunc(nameof(TransferServerFiles), new object[] { filesToTransfer.Count + " files" });

			int r = -1;
			foreach (var result in filesToTransfer)
			{
				r++;

				// absorb errors
				try
				{

					// check if the file already exists on the server
					var existsModeToUse = existsMode;
					var fileExists = FtpExtensions.FileExistsInListing(remoteListing, result.RemotePath);

					// if we want to skip uploaded files and the file already exists, mark its skipped
					if (existsMode == FtpRemoteExists.Skip && fileExists)
					{

						LogStatus(FtpTraceLevel.Info, "Skipped file that already exists: " + result.LocalPath);

						result.IsSuccess = true;
						result.IsSkipped = true;
						continue;
					}

					// in any mode if the file does not exist, mark that exists check is not required
					if (!fileExists)
					{
						existsModeToUse = existsMode == FtpRemoteExists.Append ? FtpRemoteExists.AppendNoCheck : FtpRemoteExists.NoCheck;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToTransfer.Count, r);

					// upload the file
					//var transferred = UploadFileFromFile(result.LocalPath, result.RemotePath, false, existsModeToUse, false, false, verifyOptions, progress, metaProgress);
					var transferred = TransferFile(result.LocalPath, remoteClient, result.RemotePath, false, existsModeToUse, verifyOptions, progress, metaProgress);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex)
				{

					LogStatus(FtpTraceLevel.Warn, "File failed to transfer: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

		private void TransferSubDirectories(List<FtpResult> dirsToUpload, FtpClient remoteClient)
		{
			foreach (var result in dirsToUpload)
			{

				// absorb errors
				try
				{

					// create directory on the server
					// to ensure we upload the blank remote dirs as well
					if (remoteClient.CreateDirectory(result.RemotePath))
					{
						result.IsSuccess = true;
						result.IsSkipped = false;
					}
					else
					{
						result.IsSkipped = true;
					}

				}
				catch (Exception ex)
				{

					// mark that the folder failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}
		}

#if ASYNC

		/// <summary>
		/// Opens a FXP PASV connection between the source and the remote (aka destination) ftp server asynchronously.
		/// </summary>
		/// <param name="remoteClient">FtpClient instance of the remote / destination FTP Server</param>
		/// <returns>A data stream ready to be used</returns>
		private async Task<FtpFxpSession> OpenPassiveFXPConnectionAsync(FtpClient remoteClient, CancellationToken token)
		{
			FtpReply reply;
			Match m;
			FtpClient sourceClient = null;
			FtpClient destinationClient = null;

			if (m_threadSafeDataChannels)
			{
				sourceClient = CloneConnection();
				sourceClient.CopyStateFlags(this);
				await sourceClient.ConnectAsync(token);
				await sourceClient.SetWorkingDirectoryAsync(await GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				sourceClient = this;
			}

			if (remoteClient.EnableThreadSafeDataConnections)
			{
				destinationClient = remoteClient.CloneConnection();
				destinationClient.CopyStateFlags(remoteClient);
				await destinationClient.ConnectAsync(token);
				await destinationClient.SetWorkingDirectoryAsync(await destinationClient.GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				destinationClient = remoteClient;
			}

			await sourceClient.SetDataTypeAsync(sourceClient.FXPDataType,token);
			await destinationClient.SetDataTypeAsync(destinationClient.FXPDataType, token);

			// send PASV command to destination FTP server to get passive port to be used from source FTP server
			if (!(reply = await destinationClient.ExecuteAsync("PASV", token)).Success)
			{
				throw new FtpCommandException(reply);
			}

			m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

			if (!m.Success || m.Groups.Count != 7)
			{
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// Instruct source server to open a connection to the destination Server

			if (!(reply = await sourceClient.ExecuteAsync($"PORT {m.Value}", token)).Success)
			{
				throw new FtpCommandException(reply);
			}

			return new FtpFxpSession() { sourceFtpClient = sourceClient, destinationFtpClient = destinationClient };
		}

		/// <summary>
		/// 'Copys' a file from the source FTP Server to the remote / destination FTP Server via the FXP protocol asynchronously.
		/// </summary>
		private async Task<bool> FXPFileCopyInternalAsync(string sourcePath, FtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;
			long fileSize = 0;

			var ftpFxpSession = await OpenPassiveFXPConnectionAsync(remoteClient, token);

			if (ftpFxpSession != null)
			{

				ftpFxpSession.sourceFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;
				ftpFxpSession.destinationFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;


				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.AppendNoCheck)
				{
					offset = await remoteClient.GetFileSizeAsync(remotePath, token);
					if (offset == -1)
					{
						offset = 0; // start from the beginning
					}
				}
				else
				{
					fileExists = await remoteClient.FileExistsAsync(remotePath, token);

					switch (existsMode)
					{
						case FtpRemoteExists.Skip:

							if (fileExists)
							{
								LogStatus(FtpTraceLevel.Info, "Skip is selected => Destination file exists => skipping");

								//Fix #413 - progress callback isn't called if the file has already been uploaded to the server
								//send progress reports
								if (progress != null)
								{
									progress.Report(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), sourcePath, remotePath, metaProgress));
								}

								return true;
							}

							break;

						case FtpRemoteExists.Overwrite:

							if (fileExists)
							{
								await remoteClient.DeleteFileAsync(remotePath,token);
							}

							break;

						case FtpRemoteExists.Append:

							if (fileExists)
							{
								offset = await remoteClient.GetFileSizeAsync(remotePath,token);
								if (offset == -1)
								{
									offset = 0; // start from the beginning
								}
							}

							break;
					}

				}

				fileSize = await GetFileSizeAsync(sourcePath, token);

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists)
				{
					var dirname = remotePath.GetFtpDirectoryName();
					if (!await remoteClient.DirectoryExistsAsync(dirname,token))
					{
						await CreateDirectoryAsync(dirname,token);
					}
				}

				if (offset == 0 && existsMode != FtpRemoteExists.AppendNoCheck)
				{
					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = await ftpFxpSession.sourceFtpClient.ExecuteAsync($"RETR {sourcePath}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to store the file
					if (!(reply = await ftpFxpSession.destinationFtpClient.ExecuteAsync($"STOR {remotePath}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}
				}
				else
				{
					//tell source server to restart / resume
					if (!(reply = await ftpFxpSession.sourceFtpClient.ExecuteAsync($"REST {offset}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}

					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = await ftpFxpSession.sourceFtpClient.ExecuteAsync($"RETR {sourcePath}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to append the file
					if (!(reply = await ftpFxpSession.destinationFtpClient.ExecuteAsync($"APPE {remotePath}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}
				}

				var transferStarted = DateTime.Now;
				long lastSize = 0;


				var sourceFXPTransferReply = ftpFxpSession.sourceFtpClient.GetReplyAsync(token);
				var destinationFXPTransferReply = ftpFxpSession.destinationFtpClient.GetReplyAsync(token);

				while (!sourceFXPTransferReply.IsCompleted || !destinationFXPTransferReply.IsCompleted)
				{

					if (remoteClient.EnableThreadSafeDataConnections)
					{
						// send progress reports
						if (progress != null && fileSize != -1)
						{
							offset = await remoteClient.GetFileSizeAsync(remotePath, token);

							if (offset != -1 && lastSize <= offset)
							{
								long bytesProcessed = offset - lastSize;
								lastSize = offset;
								ReportProgress(progress, fileSize, offset, bytesProcessed, DateTime.Now - transferStarted, sourcePath, remotePath, metaProgress);
							}
						}
					}

					await Task.Delay(1000);
				}

				FtpTrace.WriteLine(FtpTraceLevel.Info, $"FXP transfer of file {sourcePath} has completed");

				await NoopAsync(token);
				await remoteClient.NoopAsync(token);

				return true;
			}
			else
			{
				FtpTrace.WriteLine(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}

		}

		/// <summary>
		/// Transfer the specified file from the srouce FTP Server to the remote / destination FTP Server asynchronously using the FXP protocol.
		/// High-level API that takes care of various edge cases internally.
		/// </summary>
		/// <param name="sourcePath">The full or relative path to the file on the source FTP Server</param>
		/// <param name="remoteClient">FtpClient instance of the remote / destination FTP Server</param>
		/// <param name="remotePath">The full or relative path to destination file on the remote FTP Server</param>
		/// <param name="createRemoteDir">Indicates if the folder should be created on the remote FTP Server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// Returns a FtpStatus indicating if the file was transfered.
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public async Task<FtpStatus> TransferFileAsync(string sourcePath, FtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, FtpProgress metaProgress = null, CancellationToken token = default(CancellationToken))
		{

			LogFunc("FXPFileCopyAsync", new object[] { sourcePath, remoteClient, remotePath, FXPDataType });

			#region "Verify and Check vars and prequisites"

			if (remoteClient is null)
			{
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "remoteClient");
			}

			if (sourcePath.IsBlank())
			{
				throw new ArgumentNullException("FtpListItem must be specified!", "sourceFtpFileItem");
			}

			if (remotePath.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			if (!remoteClient.IsConnected)
			{
				throw new FluentFTP.FtpException("The connection must be open before a transfer between servers can be intitiated");
			}

			if (!this.IsConnected)
			{
				throw new FluentFTP.FtpException("The source FXP FtpClient must be open and connected before a transfer between servers can be intitiated");
			}

			if (!await FileExistsAsync(sourcePath, token)){
				throw new FluentFTP.FtpException(string.Format("Source File {0} cannot be found or does not exists!", sourcePath));
			}

			#endregion

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do
			{

				fxpSuccess = await FXPFileCopyInternalAsync(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, token, metaProgress is null ? new FtpProgress(1, 0) : metaProgress);
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None)
				{
					verified = await VerifyFXPTransferAsync(sourcePath, remoteClient, remotePath, token);
					LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0)
					{
						LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpRemoteExists.Append ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}

#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete))
			{
				await remoteClient.DeleteFileAsync(remotePath,token);
			}

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw))
			{
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return fxpSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;

		}

		/// <summary>
		/// Transfer the specified directory from the source FTP Server onto the remote FTP Server asynchronously using the FXP protocol.
		/// In Mirror mode, we will transfer missing files, and delete any extra files from the remote server if not present on the soruce FTP Server. This is very useful when creating an exact local backup of an FTP directory.
		/// In Update mode, we will only transfer missing files and preserve any extra files on the remote FTP Server. This is useful when you want to simply transfer missing files from an FTP directory.
		/// Only transfer the files and folders matching all the rules provided, if any.
		/// All exceptions during transfer are caught, and the exception is stored in the related FtpResult object.
		/// </summary>
		/// <param name="sourceFolder">The full or relative path to the folder on the source FTP Server. If it does not exist, an empty result list is returned.</param>
		/// <param name="remoteClient">FtpClient instance of the remote / destination FTP Server</param>
		/// <param name="remoteFolder">The full or relative path to destination folder on the remote FTP Server</param>
		/// <param name="mode">Mirror or Update mode, as explained above</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="rules">Only files and folders that pass all these rules are downloaded, and the files that don't pass are skipped. In the Mirror mode, the files that fail the rules are also deleted from the local folder.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public async Task<List<FtpResult>> TransferDirectoryAsync(string sourceFolder, FtpClient remoteClient, string remoteFolder, FtpFolderSyncMode mode = FtpFolderSyncMode.Update,
FtpRemoteExists existsMode = FtpRemoteExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken))
		{

			if (sourceFolder.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "sourceFolder");
			}

			if (remoteFolder.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc(nameof(TransferDirectoryAsync), new object[] { sourceFolder, remoteFolder, mode, existsMode, verifyOptions, (rules.IsBlank() ? null : rules.Count + " rules") });

			var results = new List<FtpResult>();

			// ensure the local path ends with slash
			sourceFolder = sourceFolder.EnsurePostfix("/");

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath().EnsurePostfix("/");

			// if the dir does not exist, fail fast
			if (!await DirectoryExistsAsync(sourceFolder,token))
			{
				return results;
			}

			// flag to determine if existence checks are required
			var checkFileExistence = true;

			// ensure the remote dir exists
			if (!await remoteClient.DirectoryExistsAsync(remoteFolder,token))
			{
				await remoteClient.CreateDirectoryAsync(remoteFolder,token);
				checkFileExistence = false;
			}

			// collect paths of the files that should exist (lowercase for CI checks)
			var shouldExist = new Dictionary<string, bool>();

			// get all the folders in the local directory
			var dirListing = (await GetListingAsync(sourceFolder, FtpListOption.Recursive,token)).Where(x => x.Type == FtpFileSystemObjectType.Directory).Select(x => x.FullName).ToArray();

			// get all the already existing files
			var remoteListing = checkFileExistence ? await remoteClient.GetListingAsync(remoteFolder, FtpListOption.Recursive,token) : null;

			// loop thru each folder and ensure it exists
			var dirsToUpload =  GetSubDirectoriesToTransfer(sourceFolder, remoteFolder, rules, results, dirListing);
			await TransferSubDirectoriesAsync(dirsToUpload, remoteClient,token);

			// get all the files in the local directory
			var fileListing = (await GetListingAsync(sourceFolder, FtpListOption.Recursive,token)).Where(x => x.Type == FtpFileSystemObjectType.File).Select(x => x.FullName).ToArray();

			// loop thru each file and transfer it
			var filesToUpload = GetFilesToTransfer(sourceFolder, remoteFolder, rules, results, shouldExist, fileListing);
			await TransferServerFilesAsync(filesToUpload, remoteClient, existsMode, verifyOptions, progress, remoteListing,token);

			// delete the extra remote files if in mirror mode and the directory was pre-existing
			// DeleteExtraServerFiles(mode, shouldExist, remoteListing);

			return results;
		}

		private async Task TransferServerFilesAsync(List<FtpResult> filesToTransfer, FtpClient remoteClient, FtpRemoteExists existsMode, FtpVerify verifyOptions, IProgress<FtpProgress> progress, FtpListItem[] remoteListing, CancellationToken token)
		{

			LogFunc(nameof(TransferServerFiles), new object[] { filesToTransfer.Count + " files" });

			int r = -1;
			foreach (var result in filesToTransfer)
			{
				r++;

				// absorb errors
				try
				{

					// check if the file already exists on the server
					var existsModeToUse = existsMode;
					var fileExists = FtpExtensions.FileExistsInListing(remoteListing, result.RemotePath);

					// if we want to skip uploaded files and the file already exists, mark its skipped
					if (existsMode == FtpRemoteExists.Skip && fileExists)
					{

						LogStatus(FtpTraceLevel.Info, "Skipped file that already exists: " + result.LocalPath);

						result.IsSuccess = true;
						result.IsSkipped = true;
						continue;
					}

					// in any mode if the file does not exist, mark that exists check is not required
					if (!fileExists)
					{
						existsModeToUse = existsMode == FtpRemoteExists.Append ? FtpRemoteExists.AppendNoCheck : FtpRemoteExists.NoCheck;
					}

					// create meta progress to store the file progress
					var metaProgress = new FtpProgress(filesToTransfer.Count, r);

					// upload the file
					var transferred = await TransferFileAsync(result.LocalPath, remoteClient, result.RemotePath, false, existsModeToUse, verifyOptions, progress, metaProgress,token);
					result.IsSuccess = transferred.IsSuccess();
					result.IsSkipped = transferred == FtpStatus.Skipped;

				}
				catch (Exception ex)
				{

					LogStatus(FtpTraceLevel.Warn, "File failed to transfer: " + result.LocalPath);

					// mark that the file failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}

		}

		private async Task TransferSubDirectoriesAsync(List<FtpResult> dirsToUpload, FtpClient remoteClient, CancellationToken token)
		{
			foreach (var result in dirsToUpload)
			{

				// absorb errors
				try
				{

					// create directory on the server
					// to ensure we upload the blank remote dirs as well
					if (await remoteClient.CreateDirectoryAsync(result.RemotePath,token))
					{
						result.IsSuccess = true;
						result.IsSkipped = false;
					}
					else
					{
						result.IsSkipped = true;
					}

				}
				catch (Exception ex)
				{

					// mark that the folder failed to upload
					result.IsFailed = true;
					result.Exception = ex;
				}
			}
		}

#endif
	}
}
