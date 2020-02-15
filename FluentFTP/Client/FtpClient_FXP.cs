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

		private FtpFxpSession OpenPassiveFXPConnection(FtpClient fxpDestinationClient)
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

			if (fxpDestinationClient.EnableThreadSafeDataConnections)
			{
				destinationClient = fxpDestinationClient.CloneConnection();
				destinationClient.CopyStateFlags(fxpDestinationClient);
				destinationClient.Connect();
				destinationClient.SetWorkingDirectory(destinationClient.GetWorkingDirectory());
			}
			else
			{
				destinationClient = fxpDestinationClient;
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

		public bool FXPFileCopyInternal(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;

			var ftpFxpSession = OpenPassiveFXPConnection(fxpDestinationClient);

			if (ftpFxpSession != null)
			{

				ftpFxpSession.sourceFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;
				ftpFxpSession.destinationFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;


				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.AppendNoCheck)
				{
					offset = fxpDestinationClient.GetFileSize(destinationFilePath);
					if (offset == -1)
					{
						offset = 0; // start from the beginning
					}
				}
				else
				{
					fileExists = fxpDestinationClient.FileExists(destinationFilePath);

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
									progress.Report(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), sourceFtpFileItem.FullName, destinationFilePath, metaProgress));
								}

								return true;
							}

							break;

						case FtpRemoteExists.Overwrite:

							if (fileExists)
							{
								fxpDestinationClient.DeleteFile(destinationFilePath);
							}

							break;

						case FtpRemoteExists.Append:

							if (fileExists)
							{
								offset = fxpDestinationClient.GetFileSize(destinationFilePath);
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
					var dirname = destinationFilePath.GetFtpDirectoryName();
					if (!fxpDestinationClient.DirectoryExists(dirname))
					{
						 CreateDirectory(dirname);
					}
				}

				if (offset == 0 && existsMode != FtpRemoteExists.AppendNoCheck)
				{
					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = ftpFxpSession.sourceFtpClient.Execute($"RETR {sourceFtpFileItem.FullName}")).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to store the file
					if (!(reply = ftpFxpSession.destinationFtpClient.Execute($"STOR {destinationFilePath}")).Success)
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
					if (!(reply = ftpFxpSession.sourceFtpClient.Execute($"RETR {sourceFtpFileItem.FullName}")).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to append the file
					if (!(reply = ftpFxpSession.destinationFtpClient.Execute($"APPE {destinationFilePath}")).Success)
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

					if (fxpDestinationClient.EnableThreadSafeDataConnections)
					{
						// send progress reports
						if (progress != null && sourceFtpFileItem.Size != -1)
						{
							offset = fxpDestinationClient.GetFileSize(destinationFilePath);

							if (offset != -1 && lastSize <= offset)
							{
								long bytesProcessed = offset - lastSize;
								lastSize = offset;
								ReportProgress(progress, sourceFtpFileItem.Size, offset, bytesProcessed, DateTime.Now - transferStarted, sourceFtpFileItem.FullName, destinationFilePath, metaProgress);
							}
						}
					}
#if CORE14
					Task.Delay(1000);
#else
					Thread.Sleep(1000);
#endif
				}

				FtpTrace.WriteLine(FtpTraceLevel.Info, $"FXP transfer of file {sourceFtpFileItem.FullName} has completed");

				Noop();
				fxpDestinationClient.Noop();

				return true;
			}
			else
			{
				FtpTrace.WriteLine(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}
		}

		public FtpStatus FXPFileCopy(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null)
		{

			LogFunc("FXPFileCopy", new object[] { sourceFtpFileItem, fxpDestinationClient, destinationFilePath, FXPDataType });

			#region "Verify and Check vars and prequisites"

			if (fxpDestinationClient is null)
			{
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "fxpDestinationClient");
			}

			if (sourceFtpFileItem is null)
			{
				throw new ArgumentNullException("FtpListItem must be specified!", "sourceFtpFileItem");
			}

			if (destinationFilePath.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "destinationFilePath");
			}

			if (!fxpDestinationClient.IsConnected)
			{
				throw new FluentFTP.FtpException("The connection must be open before a transfer between servers can be intitiated");
			}

			if (!this.IsConnected)
			{
				throw new FluentFTP.FtpException("The source FXP FtpClient must be open and connected before a transfer between servers can be intitiated");
			}

			if (!FileExists(sourceFtpFileItem.FullName))
			{
				throw new FluentFTP.FtpException(string.Format("Source File {0} cannot be found or does not exists!", sourceFtpFileItem.FullName));
			}

			#endregion

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do
			{

				fxpSuccess = FXPFileCopyInternal(sourceFtpFileItem, fxpDestinationClient, destinationFilePath, createRemoteDir, existsMode, progress, new FtpProgress(1, 0));
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None)
				{
					verified = VerifyFXPTransfer(sourceFtpFileItem.FullName, fxpDestinationClient, destinationFilePath);
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
				fxpDestinationClient.DeleteFile(destinationFilePath);
			}

			if (fxpSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw))
			{
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return fxpSuccess && verified ? FtpStatus.Success : FtpStatus.Failed;

		}

#if ASYNC

		private async Task<FtpFxpSession> OpenPassiveFXPConnectionAsync(FtpClient fxpDestinationClient, CancellationToken token)
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

			if (fxpDestinationClient.EnableThreadSafeDataConnections)
			{
				destinationClient = fxpDestinationClient.CloneConnection();
				destinationClient.CopyStateFlags(fxpDestinationClient);
				await destinationClient.ConnectAsync(token);
				await destinationClient.SetWorkingDirectoryAsync(await destinationClient.GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				destinationClient = fxpDestinationClient;
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

		private async Task<bool> FXPFileCopyInternalAsync(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath, bool createRemoteDir, FtpRemoteExists existsMode,
			IProgress<FtpProgress> progress, CancellationToken token, FtpProgress metaProgress)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;

			var ftpFxpSession = await OpenPassiveFXPConnectionAsync(fxpDestinationClient, token);

			if (ftpFxpSession != null)
			{

				ftpFxpSession.sourceFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;
				ftpFxpSession.destinationFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;


				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.AppendNoCheck)
				{
					offset = await fxpDestinationClient.GetFileSizeAsync(destinationFilePath,token);
					if (offset == -1)
					{
						offset = 0; // start from the beginning
					}
				}
				else
				{
					fileExists = await fxpDestinationClient.FileExistsAsync(destinationFilePath,token);

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
									progress.Report(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), sourceFtpFileItem.FullName, destinationFilePath, metaProgress));
								}

								return true;
							}

							break;

						case FtpRemoteExists.Overwrite:

							if (fileExists)
							{
								await fxpDestinationClient.DeleteFileAsync(destinationFilePath,token);
							}

							break;

						case FtpRemoteExists.Append:

							if (fileExists)
							{
								offset = await fxpDestinationClient.GetFileSizeAsync(destinationFilePath,token);
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
					var dirname = destinationFilePath.GetFtpDirectoryName();
					if (!await fxpDestinationClient.DirectoryExistsAsync(dirname,token))
					{
						await CreateDirectoryAsync(dirname,token);
					}
				}

				if (offset == 0 && existsMode != FtpRemoteExists.AppendNoCheck)
				{
					// send command to tell the source server to 'send' the file to the destination server
					if (!(reply = await ftpFxpSession.sourceFtpClient.ExecuteAsync($"RETR {sourceFtpFileItem.FullName}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to store the file
					if (!(reply = await ftpFxpSession.destinationFtpClient.ExecuteAsync($"STOR {destinationFilePath}", token)).Success)
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
					if (!(reply = await ftpFxpSession.sourceFtpClient.ExecuteAsync($"RETR {sourceFtpFileItem.FullName}", token)).Success)
					{
						throw new FtpCommandException(reply);
					}

					//Instruct destination server to append the file
					if (!(reply = await ftpFxpSession.destinationFtpClient.ExecuteAsync($"APPE {destinationFilePath}", token)).Success)
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

					if (fxpDestinationClient.EnableThreadSafeDataConnections)
					{
						// send progress reports
						if (progress != null && sourceFtpFileItem.Size != -1)
						{
							offset = await fxpDestinationClient.GetFileSizeAsync(destinationFilePath, token);

							if (offset != -1 && lastSize <= offset)
							{
								long bytesProcessed = offset - lastSize;
								lastSize = offset;
								ReportProgress(progress, sourceFtpFileItem.Size, offset, bytesProcessed, DateTime.Now - transferStarted, sourceFtpFileItem.FullName, destinationFilePath, metaProgress);
							}
						}
					}

					await Task.Delay(1000);
				}

				FtpTrace.WriteLine(FtpTraceLevel.Info, $"FXP transfer of file {sourceFtpFileItem.FullName} has completed");

				await NoopAsync(token);
				await fxpDestinationClient.NoopAsync(token);

				return true;
			}
			else
			{
				FtpTrace.WriteLine(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}

		}

		public async Task<FtpStatus> FXPFileCopyAsync(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath,
			bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken))
		{

			LogFunc("FXPFileCopyAsync", new object[] { sourceFtpFileItem, fxpDestinationClient, destinationFilePath, FXPDataType });

			#region "Verify and Check vars and prequisites"

			if (fxpDestinationClient is null)
			{
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "fxpDestinationClient");
			}

			if (sourceFtpFileItem is null)
			{
				throw new ArgumentNullException("FtpListItem must be specified!", "sourceFtpFileItem");
			}

			if (destinationFilePath.IsBlank())
			{
				throw new ArgumentException("Required parameter is null or blank.", "destinationFilePath");
			}

			if (!fxpDestinationClient.IsConnected)
			{
				throw new FluentFTP.FtpException("The connection must be open before a transfer between servers can be intitiated");
			}

			if (!this.IsConnected)
			{
				throw new FluentFTP.FtpException("The source FXP FtpClient must be open and connected before a transfer between servers can be intitiated");
			}

			if (!await FileExistsAsync(sourceFtpFileItem.FullName,token)){
				throw new FluentFTP.FtpException(string.Format("Source File {0} cannot be found or does not exists!", sourceFtpFileItem.FullName));
			}

			#endregion

			bool fxpSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do
			{

				fxpSuccess = await FXPFileCopyInternalAsync(sourceFtpFileItem, fxpDestinationClient, destinationFilePath, createRemoteDir, existsMode, progress, token, new FtpProgress(1, 0));
				attemptsLeft--;

				// if verification is needed
				if (fxpSuccess && verifyOptions != FtpVerify.None)
				{
					verified = await VerifyFXPTransferAsync(sourceFtpFileItem.FullName, fxpDestinationClient, destinationFilePath, token);
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
				await fxpDestinationClient.DeleteFileAsync(destinationFilePath,token);
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
