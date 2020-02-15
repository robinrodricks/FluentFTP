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

#if ASYNC

		private async Task<FtpFxpSession> OpenPassiveFXPConnection(FtpClient fxpDestinationClient, FtpDataType ftpDataType, CancellationToken token)
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
				await destinationClient.SetWorkingDirectoryAsync(await GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				destinationClient = fxpDestinationClient;
			}

			await sourceClient.SetDataTypeAsync(ftpDataType,token);
			await destinationClient.SetDataTypeAsync(ftpDataType,token);

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

		private async Task<bool> FXPFileCopyInternalAsync(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath, bool createRemoteDir, FtpRemoteExists existsMode, FtpDataType ftpDataType, CancellationToken token)
		{
			FtpReply reply;
			long offset = 0;
			bool fileExists = false;

			var ftpFxpSession = await OpenPassiveFXPConnection(fxpDestinationClient, ftpDataType, token);

			if (ftpFxpSession != null)
			{

				ftpFxpSession.sourceFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;
				ftpFxpSession.destinationFtpClient.ReadTimeout = (int)TimeSpan.FromMinutes((double)30).TotalMilliseconds;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpRemoteExists.AppendNoCheck)
				{
					offset = await fxpDestinationClient.GetFileSizeAsync(destinationFilePath);
					if (offset == -1)
					{
						offset = 0; // start from the beginning
					}
				}
				else
				{
					fileExists = await fxpDestinationClient.FileExistsAsync(destinationFilePath);

					switch (existsMode)
					{
						case FtpRemoteExists.Skip:

							if (fileExists)
							{
								LogStatus(FtpTraceLevel.Warn, "File " + destinationFilePath + " exists on server & existsMode is set to FileExists.Skip");

								// Fix #413 - progress callback isn't called if the file has already been uploaded to the server
								// send progress reports
								//if (progress != null)
								//{
								//	progress(new FtpProgress(100.0, 0, TimeSpan.FromSeconds(0), localPath, remotePath, metaProgress));
								//}

								return false;
							}

							break;

						case FtpRemoteExists.Overwrite:
							if (fileExists)
							{
								await fxpDestinationClient.DeleteFileAsync(destinationFilePath);
							}

							break;

						case FtpRemoteExists.Append:
							if (fileExists)
							{
								offset = await fxpDestinationClient.GetFileSizeAsync(destinationFilePath);
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
					if (!await fxpDestinationClient.DirectoryExistsAsync(dirname))
					{
						await CreateDirectoryAsync(dirname);
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
					
				var sourceFXPTransferReply = ftpFxpSession.sourceFtpClient.GetReplyAsync(token);
				var destinationFXPTransferReply = ftpFxpSession.destinationFtpClient.GetReplyAsync(token);

				while (sourceFXPTransferReply.IsCompleted == false | destinationFXPTransferReply.IsCompleted == false)
				{
					await Task.Delay(1000);
				}

				FtpTrace.Write(FtpTraceLevel.Info, $"FXP transfer of file {sourceFtpFileItem.FullName} has completed");

				await GetWorkingDirectoryAsync(token);
				await fxpDestinationClient.GetWorkingDirectoryAsync(token);

				return true;
			}
			else
			{
				FtpTrace.Write(FtpTraceLevel.Error, "Failed to open FXP passive Connection");
				return false;
			}

		}

		public async Task<bool> FXPFileCopyAsync(FtpListItem sourceFtpFileItem, FtpClient fxpDestinationClient, string destinationFilePath, bool createRemoteDir = false, FtpRemoteExists existsMode = FtpRemoteExists.Append, FtpVerify verifyOptions = FtpVerify.None,
			FtpDataType ftpDataType = FtpDataType.Binary, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken))
		{

			LogFunc("FXPFileCopyAsync", new object[] { sourceFtpFileItem, fxpDestinationClient, destinationFilePath, ftpDataType });

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

			bool downloadSuccess;
			var verified = true;
			var attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do
			{

				downloadSuccess = await FXPFileCopyInternalAsync(sourceFtpFileItem, fxpDestinationClient, destinationFilePath, createRemoteDir, existsMode, ftpDataType, token);
				attemptsLeft--;

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None)
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

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete))
			{
				await fxpDestinationClient.DeleteFileAsync(destinationFilePath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw))
			{
				throw new FtpException("Destination file checksum value does not match source file");
			}

			return downloadSuccess && verified;

		}
#endif
	}
}
