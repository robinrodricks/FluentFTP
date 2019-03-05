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
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX)
using System.Threading;
#endif
#if (CORE || NET45)
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	
	public partial class FtpClient : IDisposable {

		#region Properties

		private int m_transferChunkSize = 65536;
		/// <summary>
		/// Gets or sets the number of bytes transferred in a single chunk (a single FTP command).
		/// Used by <see cref="o:UploadFile"/>/<see cref="o:UploadFileAsync"/> and <see cref="o:DownloadFile"/>/<see cref="o:DownloadFileAsync"/>
		/// to transfer large files in multiple chunks.
		/// </summary>
		public int TransferChunkSize {
			get {
				return m_transferChunkSize;
			}
			set {
				m_transferChunkSize = value;
			}
		}

		private FtpDataType CurrentDataType;

		private int m_retryAttempts = 3;
		/// <summary>
		/// Gets or sets the retry attempts allowed when a verification failure occurs during download or upload.
		/// This value must be set to 1 or more.
		/// </summary>
		public int RetryAttempts {
			get { return m_retryAttempts; }
			set { m_retryAttempts = value > 0 ? value : 1; }
		}

		uint m_uploadRateLimit = 0;

		/// <summary>
		/// Rate limit for uploads in kbyte/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint UploadRateLimit {
			get { return m_uploadRateLimit; }
			set { m_uploadRateLimit = value; }
		}

		uint m_downloadRateLimit = 0;

		/// <summary>
		/// Rate limit for downloads in kbytes/s. Set this to 0 for unlimited speed.
		/// Honored by high-level API such as Upload(), Download(), UploadFile(), DownloadFile()..
		/// </summary>
		public uint DownloadRateLimit {
			get { return m_downloadRateLimit; }
			set { m_downloadRateLimit = value; }
		}

		public FtpDataType m_UploadDataType = FtpDataType.Binary;
		/// <summary>
		/// Controls if the high-level API uploads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType UploadDataType {
			get { return m_UploadDataType; }
			set { m_UploadDataType = value; }
		}

		public FtpDataType m_DownloadDataType = FtpDataType.Binary;
		/// <summary>
		/// Controls if the high-level API downloads files in Binary or ASCII mode.
		/// </summary>
		public FtpDataType DownloadDataType {
			get { return m_DownloadDataType; }
			set { m_DownloadDataType = value; }
		}


		// ADD PROPERTIES THAT NEED TO BE CLONED INTO
		// FtpClient.CloneConnection()

		#endregion

		#region Upload Multiple Files

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int UploadFiles(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (remoteDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remoteDir");

			this.LogFunc("UploadFiles", new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

			//int count = 0;
			bool errorEncountered = false;
			List<string> successfulUploads = new List<string>();

			// ensure ends with slash
			remoteDir = !remoteDir.EndsWith("/") ? remoteDir + "/" : remoteDir;

			//flag to determine if existence checks are required
			bool checkFileExistence = true;

			// create remote dir if wanted
			if (createRemoteDir) {
				if (!DirectoryExists(remoteDir)) {
					CreateDirectory(remoteDir);
					checkFileExistence = false;
				}
			}

			// get all the already existing files
			string[] existingFiles = checkFileExistence ? GetNameListing(remoteDir) : new string[0];

			// per local file
			foreach (string localPath in localPaths) {

				// calc remote path
				string fileName = Path.GetFileName(localPath);
				string remotePath = remoteDir + fileName;

				// try to upload it
				try {
					bool ok = UploadFileFromFile(localPath, remotePath, false, existsMode, FtpExtensions.FileExistsInNameListing(existingFiles, remotePath), true, verifyOptions, null);
					if (ok) {
						successfulUploads.Add(remotePath);
						//count++;
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					this.LogStatus(FtpTraceLevel.Error, "Upload Failure for " + localPath + ": " + ex);
					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulUploads(successfulUploads);
						}

						throw new FtpException("An error occurred uploading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					PurgeSuccessfulUploads(successfulUploads);
					successfulUploads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred uploading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulUploads.Count;
		}

		private void PurgeSuccessfulUploads(IEnumerable<string> remotePaths) {
			foreach (string remotePath in remotePaths) {
				this.DeleteFile(remotePath);
			}
		}

		/// <summary>
		/// Uploads the given file paths to a single folder on the server.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localFiles">Files to be uploaded</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int UploadFiles(IEnumerable<FileInfo> localFiles, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None) {
			return UploadFiles(localFiles.Select(f => f.FullName), remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling);
		}

#if ASYNC
		/// <summary>
		/// Uploads the given file paths to a single folder on the server asynchronously.
		/// All files are placed directly into the given folder regardless of their path on the local filesystem.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// Faster than uploading single files with <see cref="o:UploadFile"/> since it performs a single "file exists" check rather than one check per file.
		/// </summary>
		/// <param name="localPaths">The full or relative paths to the files on the local file system. Files can be from multiple folders.</param>
		/// <param name="remoteDir">The full or relative path to the directory that files will be uploaded on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to FtpExists.None for fastest performance but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="token">The token to monitor for cancellation requests</param>
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (remoteDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remoteDir");
			
			this.LogFunc("UploadFilesAsync", new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

			//check if cancellation was requested and throw to set TaskStatus state to Canceled
			token.ThrowIfCancellationRequested();

			//int count = 0;
			bool errorEncountered = false;
			List<string> successfulUploads = new List<string>();
			// ensure ends with slash
			remoteDir = !remoteDir.EndsWith("/") ? remoteDir + "/" : remoteDir;

			//flag to determine if existence checks are required
			bool checkFileExistence = true;

			// create remote dir if wanted
			if (createRemoteDir) {
				if (!await DirectoryExistsAsync(remoteDir, token)) {
					await CreateDirectoryAsync(remoteDir, token);
					checkFileExistence = false;
				}
			}

			// get all the already existing files (if directory was created just create an empty array)
			string[] existingFiles = checkFileExistence ? await GetNameListingAsync(remoteDir, token) : new string[0];

			// per local file
			foreach (string localPath in localPaths) {

				// check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();

				// calc remote path
				string fileName = Path.GetFileName(localPath);
				string remotePath = remoteDir + fileName;

				// try to upload it
				try {
					bool ok = await UploadFileFromFileAsync(localPath, remotePath, false, existsMode, FtpExtensions.FileExistsInNameListing(existingFiles, remotePath), true, verifyOptions, token, null);
					if (ok) {
						successfulUploads.Add(remotePath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					if (ex is OperationCanceledException) {
						//DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
						this.LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
						throw;
					}
					//suppress all other upload exceptions (errors are still written to FtpTrace)
					this.LogStatus(FtpTraceLevel.Error, "Upload Failure for " + localPath + ": " + ex);
					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulUploads(successfulUploads);
						}

						throw new FtpException("An error occurred uploading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					await PurgeSuccessfulUploadsAsync(successfulUploads);
					successfulUploads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred uploading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulUploads.Count;
		}

		private async Task PurgeSuccessfulUploadsAsync(IEnumerable<string> remotePaths) {
			foreach (string remotePath in remotePaths) {
				await this.DeleteFileAsync(remotePath);
			}
		}
#endif

		#endregion

		#region Download Multiple Files

		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically switch to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public int DownloadFiles(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None,
			FtpError errorHandling = FtpError.None) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (localDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localDir");

			this.LogFunc("DownloadFiles", new object[] { localDir, remotePaths, existsMode, verifyOptions });

			bool errorEncountered = false;
			List<string> successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (string remotePath in remotePaths) {

				// calc local path
				string localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
					bool ok = DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, null);
					if (ok) {
						successfulDownloads.Add(localPath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					this.LogStatus(FtpTraceLevel.Error, "Failed to download " + remotePath + ". Error: " + ex);
					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulDownloads(successfulDownloads);
						}

						throw new FtpException("An error occurred downloading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					PurgeSuccessfulDownloads(successfulDownloads);
					successfulDownloads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred downloading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulDownloads.Count;
		}

		/*
		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		public int DownloadFiles(string localDir, List<string> remotePaths, bool overwrite = true, FtpError errorHandling = FtpError.None) {
			return DownloadFiles(localDir, remotePaths.ToArray(), overwrite);
		}*/

		private void PurgeSuccessfulDownloads(IEnumerable<string> localFiles) {
			foreach (string localFile in localFiles) {
				// absorb any errors because we don't want this to throw more errors!
				try {
					File.Delete(localFile);
				} catch (Exception ex) {
					this.LogStatus(FtpTraceLevel.Warn, "FtpClient : Exception caught and discarded while attempting to delete file '" + localFile + "' : " + ex.ToString());
				}
			}
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
        /// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it dosen't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <param name="token">The token to monitor for cancellation requests</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
        public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, FtpLocalExists existsMode = FtpLocalExists.Overwrite,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (localDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			
            this.LogFunc("DownloadFilesAsync", new object[] { localDir, remotePaths, existsMode, verifyOptions });

			//check if cancellation was requested and throw to set TaskStatus state to Canceled
			token.ThrowIfCancellationRequested();
			bool errorEncountered = false;
			List<string> successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (string remotePath in remotePaths) {
				//check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();
				// calc local path
				string localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
                    bool ok = await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, token: token);
					if (ok) {
						successfulDownloads.Add(localPath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					if (ex is OperationCanceledException) {
						this.LogStatus(FtpTraceLevel.Info, "Download cancellation requested");
						//DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
						throw;
					}
					if (errorHandling.HasFlag(FtpError.Stop)) {
						errorEncountered = true;
						break;
					}

					if (errorHandling.HasFlag(FtpError.Throw)) {
						if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
							PurgeSuccessfulDownloads(successfulDownloads);
						}

						throw new FtpException("An error occurred downloading file(s).  See inner exception for more info.", ex);
					}
				}
			}

			if (errorEncountered) {
				//Delete any successful uploads if needed
				if (errorHandling.HasFlag(FtpError.DeleteProcessed)) {
					PurgeSuccessfulDownloads(successfulDownloads);
					successfulDownloads.Clear(); //forces return of 0
				}

				//Throw generic error because requested
				if (errorHandling.HasFlag(FtpError.Throw)) {
					throw new FtpException("An error occurred downloading one or more files.  Refer to trace output if available.");
				}
			}

			return successfulDownloads.Count;
		}
#endif

		#endregion

		#region Upload File

		/// <summary>
		/// Uploads the specified file directly onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to  <see cref="FtpExists.NoCheck"/> for fastest performance 
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// </remarks>
		public bool UploadFile(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false,
			FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("UploadFile", new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				this.LogStatus(FtpTraceLevel.Error, "File does not exist.");
				return false;
			}

			return UploadFileFromFile(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, progress);
		}

#if ASYNC

		/// <summary>
		/// Uploads the specified file directly onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to  <see cref="FtpExists.NoCheck"/> for fastest performance
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <param name="token">The token to monitor for cancellation requests.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// </remarks>
		public async Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			// skip uploading if the local file does not exist
#if CORE
			if (!await Task.Run(()=>File.Exists(localPath), token)) {
#else
			if (!File.Exists(localPath)) {
#endif
				this.LogStatus(FtpTraceLevel.Error, "File does not exist.");
				return false;
			}

			this.LogFunc("UploadFileAsync", new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			return await UploadFileFromFileAsync(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, token, progress);
		}
#endif

		private bool UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, IProgress<double> progress) {

			// If retries are allowed set the retry counter to the allowed count
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;

			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			bool verified = true;
			bool uploadSuccess;
			do {

				// write the file onto the server
				using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {

					// Upload file
					uploadSuccess = UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress);
					attemptsLeft--;

					// If verification is needed, update the validated flag
					if (uploadSuccess && verifyOptions != FtpVerify.None) {
						verified = VerifyTransfer(localPath, remotePath);
						this.LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {

							// Force overwrite if a retry is required
							this.LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							existsMode = FtpExists.Overwrite;
						}
					}
				}
			} while (!verified && attemptsLeft > 0);//Loop if attempts are available and validation failed


			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				this.DeleteFile(remotePath);
			}

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Uploaded file checksum value does not match local file");
			}

			return uploadSuccess && verified;
		}

#if ASYNC
		private async Task<bool> UploadFileFromFileAsync(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, CancellationToken token, IProgress<double> progress) {

			// If retries are allowed set the retry counter to the allowed count
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			
			// Default validation to true (if verification isn't needed it'll allow a pass-through)
			bool verified = true;
			bool uploadSuccess;
			do {

				// write the file onto the server
				using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) {
					uploadSuccess = await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, progress, token);
					attemptsLeft--;

					// If verification is needed, update the validated flag
					if (verifyOptions != FtpVerify.None) {
						verified = await VerifyTransferAsync(localPath, remotePath, token);
						this.LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							
							// Force overwrite if a retry is required
							this.LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							existsMode = FtpExists.Overwrite;
						}
					}
				}
			} while (!verified && attemptsLeft > 0);

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				await this.DeleteFileAsync(remotePath, token);
			}

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Uploaded file checksum value does not match local file");
			}

			return uploadSuccess && verified;
		}
#endif
		#endregion

		#region	Upload Bytes/Stream

		/// <summary>
		/// Uploads the specified stream as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileStream">The full data of the file, as a stream</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		public bool Upload(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null) {

			// verify args
			if (fileStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("Upload", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, false, false, progress);
		}
		/// <summary>
		/// Uploads the specified byte array as a file onto the server.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance 
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		public bool Upload(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null) {

			// verify args
			if (fileData == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("Upload", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (MemoryStream ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return UploadFileInternal(ms, remotePath, createRemoteDir, existsMode, false, false, progress);
			}
		}


#if ASYNC
		/// <summary>
		/// Uploads the specified stream as a file onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileStream">The full data of the file, as a stream</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="token">The token to monitor for cancellation requests.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (fileStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			this.LogFunc("UploadAsync", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, false, false, progress, token);
		}

		/// <summary>
		/// Uploads the specified byte array as a file onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="token">The token to monitor for cancellation requests.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (fileData == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("UploadAsync", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (MemoryStream ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return await UploadFileInternalAsync(ms, remotePath, createRemoteDir, existsMode, false, false, progress, token);
			}
		}
#endif

		#endregion

		#region Upload File Internal

		/// <summary>
		/// Upload the given stream to the server as a new file. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool UploadFileInternal(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, IProgress<double> progress) {
			Stream upStream = null;

			try {

				long offset = 0;
				bool checkFileExistsAgain = false;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpExists.NoCheck) {
					checkFileExistsAgain = true;
				} else if (existsMode == FtpExists.AppendNoCheck) {
					checkFileExistsAgain = true;

					offset = GetFileSize(remotePath);
					if (offset == -1) {
						offset = 0; // start from the beginning
					}
				} else {
					if (!fileExistsKnown) {
						fileExists = FileExists(remotePath);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								this.LogStatus(FtpTraceLevel.Warn, "File " + remotePath + " exists on server & existsMode is set to FileExists.Skip");
								return false;
							}
							break;
						case FtpExists.Overwrite:
							if (fileExists) {
								DeleteFile(remotePath);
							}
							break;
						case FtpExists.Append:
							if (fileExists) {
								offset = GetFileSize(remotePath);
								if (offset == -1) {
									offset = 0; // start from the beginning
								}
							}
							break;
					}
				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					string dirname = remotePath.GetFtpDirectoryName();
					if (!DirectoryExists(dirname)) {
						CreateDirectory(dirname);
					}
				}

				// FIX #213 : Do not change Stream.Position if not supported
				if (fileData.CanSeek) {
					try {

						// seek to required offset
						fileData.Position = offset;

					} catch (Exception ex2) {
					}
				}

				// open a file connection
				if (offset == 0) {
					upStream = OpenWrite(remotePath, UploadDataType, checkFileExistsAgain);
				} else {
					upStream = OpenAppend(remotePath, UploadDataType, checkFileExistsAgain);
				}

				// loop till entire file uploaded
				long len = fileData.Length;
				byte[] buffer = new byte[TransferChunkSize];

				if (UploadRateLimit == 0) {
					while (offset < len) {
						try {

							// read a chunk of bytes from the file
							int readBytes;
							while ((readBytes = fileData.Read(buffer, 0, buffer.Length)) > 0) {

								// write chunk to the FTP stream
								upStream.Write(buffer, 0, readBytes);
								upStream.Flush();
								offset += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, len, offset);
								}
							}

							// zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
							break;
						} catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							if (!ResumeUpload(remotePath, ref upStream, offset, ex)) {
								throw;
							}
						}
					}
				} else {
					Stopwatch sw = new Stopwatch();
					double rateLimitBytes = UploadRateLimit * 1024;
					while (offset < len) {
						try {

							// read a chunk of bytes from the file
							int readBytes;
							double limitCheckBytes = 0;
							sw.Start();
							while ((readBytes = fileData.Read(buffer, 0, buffer.Length)) > 0) {

								// write chunk to the FTP stream
								upStream.Write(buffer, 0, readBytes);
								upStream.Flush();
								offset += readBytes;
								limitCheckBytes += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, len, offset);
								}

								// honor the speed limit
								int swTime = (int)sw.ElapsedMilliseconds;
								if (swTime >= 1000) {
									double timeShouldTake = limitCheckBytes / rateLimitBytes * 1000;
									if (timeShouldTake > swTime) {
#if CORE14
                                        Task.Delay((int)(timeShouldTake - swTime)).Wait();
#else
										Thread.Sleep((int)(timeShouldTake - swTime));
#endif
									}
									limitCheckBytes = 0;
									sw.Restart();
								}
							}

							// zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
							break;
						} catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							if (!ResumeUpload(remotePath, ref upStream, offset, ex)) {
								sw.Stop();
								throw;
							}

						}
					}

					sw.Stop();
				}

				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// send progress reports
				if (progress != null) {
					progress.Report(100.0);
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!EnableThreadSafeDataConnections) {
					FtpReply status = GetReply();

					// Fix #353: if server sends 550 the transfer was received but could not be confirmed by the server
					if (status.Code != null && status.Code != "" && status.Code.StartsWith("5")) {
						return false;
					}
				}

				return true;

			} catch (Exception ex1) {

				// close stream before throwing error
				try {
					if (upStream != null)
						upStream.Dispose();
				} catch (Exception) { }

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}

#if ASYNC
		/// <summary>
		/// Upload the given stream to the server as a new file asynchronously. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> UploadFileInternalAsync(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, IProgress<double> progress, CancellationToken token = default(CancellationToken)) {
			Stream upStream = null;
			try {
				long offset = 0;
				bool checkFileExistsAgain = false;

				// check if the file exists, and skip, overwrite or append
				if (existsMode == FtpExists.NoCheck) {
					checkFileExistsAgain = true;
				} else if (existsMode == FtpExists.AppendNoCheck) {
					checkFileExistsAgain = true;
					offset = await GetFileSizeAsync(remotePath, token);
					if (offset == -1) {
						offset = 0; // start from the beginning
					}
				} else {
					if (!fileExistsKnown) {
						fileExists = await FileExistsAsync(remotePath, token);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								this.LogStatus(FtpTraceLevel.Warn, "File " + remotePath + " exists on server & existsMode is set to FileExists.Skip");
								return false;
							}
							break;
						case FtpExists.Overwrite:
							if (fileExists) {
								await DeleteFileAsync(remotePath, token);
							}
							break;
						case FtpExists.Append:
							if (fileExists) {
								offset = await GetFileSizeAsync(remotePath, token);
								if (offset == -1) {
									offset = 0; // start from the beginning
								}
							}
							break;
					}
				}

				// ensure the remote dir exists .. only if the file does not already exist!
				if (createRemoteDir && !fileExists) {
					string dirname = remotePath.GetFtpDirectoryName();
					if (!await DirectoryExistsAsync(dirname, token)) {
						await CreateDirectoryAsync(dirname, token);
					}
				}
				
				// FIX #213 : Do not change Stream.Position if not supported
				if (fileData.CanSeek) {
					try {

						// seek to required offset
						fileData.Position = offset;

					} catch (Exception ex2) {
					}
				}

				// open a file connection
				if (offset == 0) {
					upStream = await OpenWriteAsync(remotePath, UploadDataType, checkFileExistsAgain, token);
				} else {
					upStream = await OpenAppendAsync(remotePath, UploadDataType, checkFileExistsAgain, token);
				}

				// loop till entire file uploaded
				long len = fileData.Length;
				byte[] buffer = new byte[TransferChunkSize];
				if (UploadRateLimit == 0) {
					while (offset < len) {
						try {
							// read a chunk of bytes from the file
							int readBytes;
							while ((readBytes = await fileData.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {

								// write chunk to the FTP stream
								await upStream.WriteAsync(buffer, 0, readBytes, token);
								await upStream.FlushAsync(token);
								offset += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, len, offset);
								}
							}

							// zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
							break;
                        } catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							var resumeResult = await ResumeUploadAsync(remotePath, upStream, offset, ex);
							if (resumeResult.Item1) {
								upStream = resumeResult.Item2;
							}
							else { 
								throw;
							}
						}
					}
				} else {

					Stopwatch sw = new Stopwatch();
					double rateLimitBytes = UploadRateLimit * 1024;
					while (offset < len) {
						try {

							// read a chunk of bytes from the file
							int readBytes;
							double limitCheckBytes = 0;
							sw.Start();
							while ((readBytes = await fileData.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {

								// write chunk to the FTP stream
								await upStream.WriteAsync(buffer, 0, readBytes, token);
								await upStream.FlushAsync(token);
								offset += readBytes;
								limitCheckBytes += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, len, offset);
								}

								// honor the rate limit
								int swTime = (int)sw.ElapsedMilliseconds;
								if (swTime >= 1000) {
									double timeShouldTake = limitCheckBytes / rateLimitBytes * 1000;
									if (timeShouldTake > swTime) {
                                        await Task.Delay((int)(timeShouldTake - swTime), token);
										token.ThrowIfCancellationRequested();
									}
									limitCheckBytes = 0;
									sw.Restart();
								}
                            }

                            // zero return value (with no Exception) indicates EOS; so we should terminate the outer loop here
                            break;
                        } catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							var resumeResult = await ResumeUploadAsync(remotePath, upStream, offset, ex);
							if (resumeResult.Item1) {
								upStream = resumeResult.Item2;
							}
							else { 
								sw.Stop();
								throw;
							}
						}
					}
					sw.Stop();
				}

				// wait for transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// send progress reports
				if (progress != null) {
					progress.Report(100.0);
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = await GetReplyAsync(token);

					// Fix #353: if server sends 550 the transfer was received but could not be confirmed by the server
					if (status.Code != null && status.Code != "" && status.Code.StartsWith("5")) {
						return false;
					}
				}

				return true;
			} catch (Exception ex1) {
				// close stream before throwing error
				try {
					if (upStream != null)
						upStream.Dispose();
				} catch (Exception) { }

				if(ex1 is OperationCanceledException)
				{
					this.LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
					throw;
				}

				// catch errors during upload
				throw new FtpException("Error while uploading the file to the server. See InnerException for more info.", ex1);
			}
		}
#endif

		private bool ResumeUpload(string remotePath, ref Stream upStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39)
			if (ex.InnerException != null) {
				var iex = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
				if (iex != null && (int)iex.SocketErrorCode == 10054) {
#else
				if (iex != null && iex.ErrorCode == 10054) {
#endif
					upStream.Dispose();
					upStream = OpenAppend(remotePath, UploadDataType, true);
					upStream.Position = offset;
					return true;
				}
			}
			return false;
		}

#if ASYNC
		private async Task<Tuple<bool, Stream>> ResumeUploadAsync(string remotePath, Stream upStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39)
			if (ex.InnerException != null) {
				var iex = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
				if (iex != null && (int)iex.SocketErrorCode == 10054) {
#else
				if (iex != null && iex.ErrorCode == 10054) {
#endif
					upStream.Dispose();
					var returnStream = await OpenAppendAsync(remotePath, UploadDataType, true);
					returnStream.Position = offset;
					return Tuple.Create(true, returnStream);
				}
			}
			return Tuple.Create(false, (Stream)null);
		}
#endif

		#endregion

		#region Download File

		/// <summary>
		/// Downloads the specified file onto the local file system.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">If the file exists on disk, should we skip it, resume the download or restart the download?</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public bool DownloadFile(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Overwrite, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("DownloadFile", new object[] { localPath, remotePath, existsMode, verifyOptions });

			return DownloadFileToFile(localPath, remotePath, existsMode, verifyOptions, progress);
		}

		private bool DownloadFileToFile(string localPath, string remotePath, FtpLocalExists existsMode, FtpVerify verifyOptions, IProgress<double> progress) {
			FileMode outStreamFileMode = FileMode.Create;
			// skip downloading if local file size matches
			if (existsMode == FtpLocalExists.Append && File.Exists(localPath)) {
				if (GetFileSize(remotePath).Equals(new FileInfo(localPath).Length)) {
					this.LogStatus(FtpTraceLevel.Info, "Append is selected => Local file size matches size on server => skipping");
					return false;
				} else {
					outStreamFileMode = FileMode.Append;
				}
			} else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
				this.LogStatus(FtpTraceLevel.Info, "Skip is selected => Local file exists => skipping");
				return false;
			}

			try {

				// create the folders
				string dirPath = Path.GetDirectoryName(localPath);
				if (!FtpExtensions.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
					Directory.CreateDirectory(dirPath);
				}
			} catch (Exception ex1) {

				// catch errors creating directory
				throw new FtpException("Error while creating directories. See InnerException for more info.", ex1);
			}

			bool downloadSuccess;
			bool verified = true;
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {

				// download the file from server
				using (var outStream = new FileStream(localPath, outStreamFileMode, FileAccess.Write, FileShare.None)) {

					// download the file straight to a file stream
					downloadSuccess = DownloadFileInternal(remotePath, outStream, File.Exists(localPath) ? new FileInfo(localPath).Length : 0, progress);
					attemptsLeft--;
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyTransfer(localPath, remotePath);
					this.LogLine(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
						this.LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}
#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified file onto the local file system asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
        /// <param name="existsMode">Overwrite if you want the local file to be overwritten if it already exists. Append will also create a new file if it dosen't exists</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
        public async Task<bool> DownloadFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
            this.LogFunc("DownloadFileAsync", new object[] { localPath, remotePath, existsMode, verifyOptions });

            return await DownloadFileToFileAsync(localPath, remotePath, existsMode, verifyOptions, progress, token);
		}

		private async Task<bool> DownloadFileToFileAsync(string localPath, string remotePath, FtpLocalExists existsMode = FtpLocalExists.Append, FtpVerify verifyOptions = FtpVerify.None, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {



			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

            this.LogFunc("DownloadFileAsync", new object[] { localPath, remotePath, existsMode, verifyOptions });


            FileMode outStreamFileMode = FileMode.Create;
			// skip downloading if the local file exists
#if CORE
            if (existsMode == FtpLocalExists.Append && await Task.Run(() => File.Exists(localPath), token)) {
                if ((await GetFileSizeAsync(remotePath, token)).Equals((await Task.Run(() => new FileInfo(localPath), token)).Length)) {
#else
			if (existsMode == FtpLocalExists.Append && File.Exists(localPath)) {
				if ((await GetFileSizeAsync(remotePath)).Equals(new FileInfo(localPath).Length)) {
#endif
					this.LogStatus(FtpTraceLevel.Info, "Append is enabled => Local file size matches size on server => skipping");
					return false;
				}
                else {
                    outStreamFileMode = FileMode.Append;
                }
            }
#if CORE
			else if (existsMode == FtpLocalExists.Skip && await Task.Run(() => File.Exists(localPath), token)) {
#else
			else if (existsMode == FtpLocalExists.Skip && File.Exists(localPath)) {
#endif
				this.LogStatus(FtpTraceLevel.Info, "Skip is selected => Local file exists => skipping");
				return false;
			}

			try {
				
				// create the folders
				string dirPath = Path.GetDirectoryName(localPath);
#if CORE
				if (!String.IsNullOrWhiteSpace(dirPath) && !await Task.Run(() => Directory.Exists(dirPath), token)) {
#else
				if (!String.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
#endif
					Directory.CreateDirectory(dirPath);
				}
			} catch (Exception ex1) {
				
				// catch errors creating directory
				throw new FtpException("Error while crated directories. See InnerException for more info.", ex1);
			}

			bool downloadSuccess;
			bool verified = true;
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			do {

				// download the file from server
                using (var outStream = new FileStream(localPath, outStreamFileMode, FileAccess.Write, FileShare.None, 4096, true)) {
					
					// download the file straight to a file stream
                    downloadSuccess = await DownloadFileInternalAsync(remotePath, outStream, await Task.Run(() => File.Exists(localPath), token) ? (await Task.Run(() => new FileInfo(localPath), token)).Length : 0, progress, token);
					attemptsLeft--;
				}

				// if verification is needed
				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = await VerifyTransferAsync(localPath, remotePath, token);
					this.LogStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
                        this.LogStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode == FtpLocalExists.Append ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
					}
#endif
				}
			} while (!verified && attemptsLeft > 0);

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				File.Delete(localPath);
			}

			if (downloadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Throw)) {
				throw new FtpException("Downloaded file checksum value does not match remote file");
			}

			return downloadSuccess && verified;
		}
#endif
		#endregion

		#region	Download Bytes/Stream

		/// <summary>
		/// Downloads the specified file into the specified stream.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(Stream outStream, string remotePath, long restartPosition = 0, IProgress<double> progress = null) {

			// verify args
			if (outStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("Download", new object[] { remotePath });

			// download the file from the server
			return DownloadFileInternal(remotePath, outStream, restartPosition, progress);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(out byte[] outBytes, string remotePath, long restartPosition = 0, IProgress<double> progress = null) {

			// verify args
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			this.LogFunc("Download", new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (MemoryStream outStream = new MemoryStream()) {
				ok = DownloadFileInternal(remotePath, outStream, restartPosition, progress);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}
			return ok;
		}

#if ASYNC
		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
        public async Task<bool> DownloadAsync(Stream outStream, string remotePath, long restartPosition = 0, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (outStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			this.LogFunc("DownloadAsync", new object[] { remotePath });
			
			// download the file from the server
            return await DownloadFileInternalAsync(remotePath, outStream, restartPosition, progress, token);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress. The value provided is in the range 0 to 100, indicating the percentage of the file transferred. If the progress is indeterminate, -1 is sent.</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
        public async Task<byte[]> DownloadAsync(string remotePath, long restartPosition = 0, IProgress<double> progress = null, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			this.LogFunc("DownloadAsync", new object[] { remotePath });
			
			// download the file from the server
			using (MemoryStream outStream = new MemoryStream()) {
                bool ok = await DownloadFileInternalAsync(remotePath, outStream, restartPosition, progress, token);
				return ok ? outStream.ToArray() : null;
			}
		}

		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadAsync(string remotePath, CancellationToken token = default(CancellationToken)) {

			// download the file from the server
            return await DownloadAsync(remotePath, 0, null, token);
		}
#endif

		#endregion

		#region Download File Internal

		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool DownloadFileInternal(string remotePath, Stream outStream, long restartPosition, IProgress<double> progress) {

			Stream downStream = null;

			try {

				// get file size if downloading in binary mode (in ASCII mode we read until EOF)
				long fileLen = 0;
				if (DownloadDataType == FtpDataType.Binary && progress != null) {
					fileLen = GetFileSize(remotePath);
				}

				// open the file for reading
				downStream = OpenRead(remotePath, DownloadDataType, restartPosition, fileLen > 0);

				// if the server has not provided a length for this file
				// we read until EOF instead of reading a specific number of bytes
				bool readToEnd = (fileLen <= 0);

				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
				long offset = restartPosition;
				if (DownloadRateLimit == 0) {
					while (offset < fileLen || readToEnd) {
						try {

							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {

								// write chunk to output stream
								outStream.Write(buffer, 0, readBytes);
								offset += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, fileLen, offset);
								}
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd || offset == fileLen) {
								break;
							}

							// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
							throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
						} catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							if (!ResumeDownload(remotePath, ref downStream, offset, ex)) {
								throw;
							}
						}

					}
				} else {
					Stopwatch sw = new Stopwatch();
					double rateLimitBytes = DownloadRateLimit * 1024;
					while (offset < fileLen || readToEnd) {
						try {

							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							double limitCheckBytes = 0;
							sw.Start();
							while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {

								// write chunk to output stream
								outStream.Write(buffer, 0, readBytes);
								offset += readBytes;
								limitCheckBytes += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, fileLen, offset);
								}

								// honor the rate limit
								int swTime = (int)sw.ElapsedMilliseconds;
								if (swTime >= 1000) {
									double timeShouldTake = limitCheckBytes / rateLimitBytes * 1000;
									if (timeShouldTake > swTime) {
#if CORE14
                                        Task.Delay((int)(timeShouldTake - swTime)).Wait();
#else
										Thread.Sleep((int)(timeShouldTake - swTime));
#endif
									}
									limitCheckBytes = 0;
									sw.Restart();
								}
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd || offset == fileLen) {
								break;
							}

							// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
							throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
						} catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							if (!ResumeDownload(remotePath, ref downStream, offset, ex)) {
								sw.Stop();
								throw;
							}

						}

					}

					sw.Stop();
				}

				// disconnect FTP stream before exiting
				outStream.Flush();
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = GetReply();
				}
				return true;


			} catch (Exception ex1) {

				// close stream before throwing error
				try {
					downStream.Dispose();
				} catch (Exception) { }

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					this.LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

#if ASYNC
		/// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
        private async Task<bool> DownloadFileInternalAsync(string remotePath, Stream outStream, long restartPosition, IProgress<double> progress, CancellationToken token = default(CancellationToken)) {
			Stream downStream = null;
			try {
				
				// get file size if downloading in binary mode (in ASCII mode we read until EOF)
				long fileLen = 0;

				if (DownloadDataType == FtpDataType.Binary && progress != null){
					fileLen = await GetFileSizeAsync(remotePath, token);
				}
				
				// open the file for reading
                downStream = await OpenReadAsync(remotePath, DownloadDataType, restartPosition, fileLen > 0, token);
				
				// if the server has not provided a length for this file
				// we read until EOF instead of reading a specific number of bytes
				bool readToEnd = (fileLen <= 0);
				
				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
                long offset = restartPosition;
				if (DownloadRateLimit == 0) {
					while (offset < fileLen || readToEnd) {
						try {

							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
								
								// write chunk to output stream
								await outStream.WriteAsync(buffer, 0, readBytes, token);
								offset += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, fileLen, offset);
								}
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd || offset == fileLen) {
								break;
							}

							// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
							throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
                        } catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							var resumeResult = await ResumeDownloadAsync(remotePath, downStream, offset, ex);
							if (resumeResult.Item1) {
								downStream = resumeResult.Item2;
							} else { 
								throw;
							}
						}

					}
				} else {
					Stopwatch sw = new Stopwatch();
					double rateLimitBytes = DownloadRateLimit * 1024;
					while (offset < fileLen || readToEnd) {
						try {

							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							double limitCheckBytes = 0;
							sw.Start();
							while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
								
								// write chunk to output stream
								await outStream.WriteAsync(buffer, 0, readBytes, token);
								offset += readBytes;
								limitCheckBytes += readBytes;

								// send progress reports
								if (progress != null) {
									ReportProgress(progress, fileLen, offset);
								}

								// honor the rate limit
								int swTime = (int)sw.ElapsedMilliseconds;
								if (swTime >= 1000) {
									double timeShouldTake = limitCheckBytes / rateLimitBytes * 1000;
									if (timeShouldTake > swTime) {
                                        await Task.Delay((int)(timeShouldTake - swTime), token);
										token.ThrowIfCancellationRequested();
                                    }
                                    limitCheckBytes = 0;
									sw.Restart();
								}
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd || offset == fileLen) {
								break;
							}

							// zero return value (with no Exception) indicates EOS; so we should fail here and attempt to resume
							throw new IOException($"Unexpected EOF for remote file {remotePath} [{offset}/{fileLen} bytes read]");
						} catch (IOException ex) {

							// resume if server disconnected midway, or throw if there is an exception doing that as well
							var resumeResult = await ResumeDownloadAsync(remotePath, downStream, offset, ex);
							if (resumeResult.Item1) {
								downStream = resumeResult.Item2;
							} else { 
								sw.Stop();
								throw;
							}
						}

					}

					sw.Stop();
				}

				// disconnect FTP stream before exiting
				await outStream.FlushAsync(token);
				downStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = await GetReplyAsync(token);
				}
				return true;

			} catch (Exception ex1) {
				// close stream before throwing error
				try {
					downStream.Dispose();
				} catch (Exception) { }

				if (ex1 is OperationCanceledException)
				{
					this.LogStatus(FtpTraceLevel.Info, "Upload cancellation requested");
					throw;
				}

				// absorb "file does not exist" exceptions and simply return false
				if (ex1.Message.Contains("No such file") || ex1.Message.Contains("not exist") || ex1.Message.Contains("missing file") || ex1.Message.Contains("unknown file")) {
					this.LogStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}
#endif

		private bool ResumeDownload(string remotePath, ref Stream downStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39)
			if (ex.InnerException != null || ex.Message.StartsWith("Unexpected EOF for remote file")) {
				var ie = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
				if (ie == null || (ie != null && (int)ie.SocketErrorCode == 10054)) {
#else
				if (ie == null || (ie != null && ie.ErrorCode == 10054)) {
#endif
					downStream.Dispose();
					downStream = OpenRead(remotePath, DownloadDataType, restart: offset);
					return true;
				}
			}
			return false;
		}

#if ASYNC
		private async Task<Tuple<bool, Stream>> ResumeDownloadAsync(string remotePath, Stream downStream, long offset, IOException ex) {
			// resume if server disconnects midway (fixes #39)
			if (ex.InnerException != null || ex.Message.StartsWith("Unexpected EOF for remote file")) {
				var ie = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
				if (ie == null || (ie != null && (int)ie.SocketErrorCode == 10054)) {
#else
				if (ie == null || (ie != null && ie.ErrorCode == 10054)) {
#endif
					downStream.Dispose();
					return Tuple.Create(true, await OpenReadAsync(remotePath, DownloadDataType, restart: offset));
				}
			}
			return Tuple.Create(false, (Stream)null);
		}
#endif

		#endregion

		#region Verification

		private bool VerifyTransfer(string localPath, string remotePath) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			if (this.HasFeature(FtpCapability.HASH) || this.HasFeature(FtpCapability.MD5) ||
				this.HasFeature(FtpCapability.XMD5) || this.HasFeature(FtpCapability.XCRC) ||
				this.HasFeature(FtpCapability.XSHA1) || this.HasFeature(FtpCapability.XSHA256) ||
				this.HasFeature(FtpCapability.XSHA512)) {
				FtpHash hash = this.GetChecksum(remotePath);
				if (!hash.IsValid)
					return false;

				return hash.Verify(localPath);
			}

			//Not supported return true to ignore validation
			return true;
		}

#if ASYNC
		private async Task<bool> VerifyTransferAsync(string localPath, string remotePath, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			if (this.HasFeature(FtpCapability.HASH) || this.HasFeature(FtpCapability.MD5) ||
				this.HasFeature(FtpCapability.XMD5) || this.HasFeature(FtpCapability.XCRC) ||
				this.HasFeature(FtpCapability.XSHA1) || this.HasFeature(FtpCapability.XSHA256) ||
				this.HasFeature(FtpCapability.XSHA512)) {
				FtpHash hash = await this.GetChecksumAsync(remotePath, token);
				if (!hash.IsValid)
					return false;

				return hash.Verify(localPath);
			}

			//Not supported return true to ignore validation
			return true;
		}
#endif

		#endregion

		#region Utilities

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		private void ReportProgress(IProgress<double> progress, long fileSize, long position) {

			// calculate % based on file len vs file offset
			double value = ((double)position / (double)fileSize) * 100;

			// suppress invalid values and send -1 instead
			if (double.IsNaN(value) || double.IsInfinity(value)) {
				progress.Report(-1);
			} else {

				// send a value between 0-100 indicating percentage complete
				progress.Report(value);
			}
		}

		#endregion

	}
}