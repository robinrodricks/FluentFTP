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

			return new FtpFxpSession() { sourceFtpClient = sourceClient, destinationFtpClient = destinationClient };
		}

		public bool FXPFileCopyInternal(string sourcePath, FtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;

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
									progress.Report(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), sourcePath, remotePath, metaProgress));
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
				long fileSize = GetFileSize(sourcePath);

				var sourceFXPTransferReply = ftpFxpSession.sourceFtpClient.GetReply();
				var destinationFXPTransferReply = ftpFxpSession.destinationFtpClient.GetReply();

				while (!sourceFXPTransferReply.Success || !destinationFXPTransferReply.Success)
				{

					if (remoteClient.EnableThreadSafeDataConnections)
					{
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

		public FtpStatus FXPFileCopy(string sourcePath, FtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null)
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

				fxpSuccess = FXPFileCopyInternal(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, new FtpProgress(1, 0));
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

	
#if ASYNC

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

		private async Task<bool> FXPFileCopyInternalAsync(string sourcePath, FtpClient remoteClient, string remotePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;

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
				long fileSize = await GetFileSizeAsync(sourcePath, token);

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

		public async Task<FtpStatus> FXPFileCopyAsync(string sourcePath, FtpClient remoteClient, string remotePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken))
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

				fxpSuccess = await FXPFileCopyInternalAsync(sourcePath, remoteClient, remotePath, createRemoteDir, existsMode, progress, token, new FtpProgress(1, 0));
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
#endif
	}
}
