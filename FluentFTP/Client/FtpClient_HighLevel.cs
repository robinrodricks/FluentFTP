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
#if (CORE || NETFX45)
using System.Threading.Tasks;
#endif

namespace FluentFTP {

	/// <summary>
	/// FTP Control Connection. Speaks the FTP protocol with the server and
	/// provides facilities for performing transactions.
	/// 
	/// Debugging problems with FTP transactions is much easier to do when
	/// you can see exactly what is sent to the server and the reply 
	/// FluentFTP gets in return. Please review the Debug example
	/// below for information on how to add <see cref="System.Diagnostics.TraceListener"/>s for capturing
	/// the conversation between FluentFTP and the server.
	/// </summary>
	/// <example>The following example illustrates how to assist in debugging
	/// FluentFTP by getting a transaction log from the server.
	/// <code source="..\Examples\Debug.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates adding a custom file
	/// listing parser in the event that you encounter a list format
	/// not already supported.
	/// <code source="..\Examples\CustomParser.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to validate
	/// a SSL certificate when using SSL/TLS.
	/// <code source="..\Examples\ValidateCertificate.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file.
	/// <code source="..\Examples\OpenRead.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file
	/// using a URI object.
	/// <code source="..\Examples\OpenReadURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file.
	/// <code source="..\Examples\OpenWrite.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file
	/// using a URI object.
	/// <code source="..\Examples\OpenWriteURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file.
	/// <code source="..\Examples\OpenAppend.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file
	/// using a URI object.
	/// <code source="..\Examples\OpenAppendURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to get a file
	/// listing from the server.
	/// <code source="..\Examples\GetListing.cs" lang="cs" />
	/// </example>
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
			
			FtpTrace.WriteFunc("UploadFiles", new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

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
					bool ok = UploadFileFromFile(localPath, remotePath, false, existsMode, existingFiles.Contains(fileName), true, verifyOptions);
					if (ok) {
						successfulUploads.Add(remotePath);
						//count++;
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					FtpTrace.WriteStatus(FtpTraceLevel.Error, "Upload Failure for " + localPath + ": " + ex);
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

#if NETFX45
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
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode, bool createRemoteDir, FtpVerify verifyOptions, FtpError errorHandling, CancellationToken token) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (remoteDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remoteDir");
			
			FtpTrace.WriteFunc("UploadFilesAsync", new object[] { localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling });

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
				if (!await DirectoryExistsAsync(remoteDir)) {
					await CreateDirectoryAsync(remoteDir);
					checkFileExistence = false;
				}
			}

			// get all the already existing files (if directory was created just create an empty array)
			string[] existingFiles = checkFileExistence ? await GetNameListingAsync(remoteDir) : new string[0];

			// per local file
			foreach (string localPath in localPaths) {

				// check if cancellation was requested and throw to set TaskStatus state to Canceled
				token.ThrowIfCancellationRequested();

				// calc remote path
				string fileName = Path.GetFileName(localPath);
				string remotePath = remoteDir + fileName;

				// try to upload it
				try {
					bool ok = await UploadFileFromFileAsync(localPath, remotePath, false, existsMode, existingFiles.Contains(fileName), true, verifyOptions, token);
					if (ok) {
						successfulUploads.Add(remotePath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					if (ex is OperationCanceledException) {
						//DO NOT SUPPRESS CANCELLATION REQUESTS -- BUBBLE UP!
						FtpTrace.WriteStatus(FtpTraceLevel.Info, "Upload cancellation requested");
						throw;
					}
					//suppress all other upload exceptions (errors are still written to FtpTrace)
					FtpTrace.WriteStatus(FtpTraceLevel.Error, "Upload Failure for " + localPath + ": " + ex);
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
				await this.DeleteDirectoryAsync(remotePath);
			}
		}

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
		/// <returns>The count of how many files were uploaded successfully. Affected when files are skipped when they already exist.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = true, FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None) {
			return await UploadFilesAsync(localPaths, remoteDir, existsMode, createRemoteDir, verifyOptions, errorHandling, CancellationToken.None);
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
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
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
		public int DownloadFiles(string localDir, IEnumerable<string> remotePaths, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None,
			FtpError errorHandling = FtpError.None) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (localDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			
			FtpTrace.WriteFunc("DownloadFiles", new object[] { localDir, remotePaths, overwrite, verifyOptions });

			bool errorEncountered = false;
			List<string> successfulDownloads = new List<string>();

			// ensure ends with slash
			localDir = !localDir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localDir + Path.DirectorySeparatorChar.ToString() : localDir;

			foreach (string remotePath in remotePaths) {

				// calc local path
				string localPath = localDir + remotePath.GetFtpFileName();

				// try to download it
				try {
					bool ok = DownloadFileToFile(localPath, remotePath, overwrite, verifyOptions);
					if (ok) {
						successfulDownloads.Add(localPath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					FtpTrace.WriteStatus(FtpTraceLevel.Error, "Failed to download " + remotePath + ". Error: " + ex);
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
					FtpTrace.WriteStatus(FtpTraceLevel.Warn, "FtpClient : Exception caught and discarded while attempting to delete file '" + localFile + "' : " + ex.ToString());
				}
			}
		}

#if NETFX45
		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
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
		public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite, FtpVerify verifyOptions,
			FtpError errorHandling, CancellationToken token) {

			// verify args
			if (!errorHandling.IsValidCombination())
				throw new ArgumentException("Invalid combination of FtpError flags.  Throw & Stop cannot be combined");
			if (localDir.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localDir");
			
			FtpTrace.WriteFunc("DownloadFilesAsync", new object[] { localDir, remotePaths, overwrite, verifyOptions });

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
					bool ok = await DownloadFileToFileAsync(localPath, remotePath, overwrite, verifyOptions, token);
					if (ok) {
						successfulDownloads.Add(localPath);
					} else if ((int)errorHandling > 1) {
						errorEncountered = true;
						break;
					}
				} catch (Exception ex) {
					if (ex is OperationCanceledException) {
						FtpTrace.WriteStatus(FtpTraceLevel.Info, "Download cancellation requested");
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

		/// <summary>
		/// Downloads the specified files into a local single directory.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// Same speed as <see cref="o:DownloadFile"/>.
		/// </summary>
		/// <param name="localDir">The full or relative path to the directory that files will be downloaded into.</param>
		/// <param name="remotePaths">The full or relative paths to the files on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="errorHandling">Used to determine how errors are handled</param>
		/// <returns>The count of how many files were downloaded successfully. When existing files are skipped, they are not counted.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// If <see cref="FtpVerify.Throw"/> is set and <see cref="FtpError.Throw"/> is <i>not set</i>, then individual verification errors will not cause an exception
		/// to propagate from this method.
		/// </remarks>
		public async Task<int> DownloadFilesAsync(string localDir, IEnumerable<string> remotePaths, bool overwrite = true,
			FtpVerify verifyOptions = FtpVerify.None, FtpError errorHandling = FtpError.None) {
			return await DownloadFilesAsync(localDir, remotePaths, overwrite, verifyOptions, errorHandling, CancellationToken.None);
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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// </remarks>
		public bool UploadFile(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false,
			FtpVerify verifyOptions = FtpVerify.None) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("UploadFile", new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				FtpTrace.WriteStatus(FtpTraceLevel.Error, "File does not exist.");
				return false;
			}

			return UploadFileFromFile(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions);
		}

#if NETFX45

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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// </remarks>
		public async Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode, bool createRemoteDir,
			FtpVerify verifyOptions, CancellationToken token) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			// skip uploading if the local file does not exist
			if (!File.Exists(localPath)) {
				FtpTrace.WriteStatus(FtpTraceLevel.Error, "File does not exist.");
				return false;
			}

			FtpTrace.WriteFunc("UploadFileAsync", new object[] { localPath, remotePath, existsMode, createRemoteDir, verifyOptions });

			return await UploadFileFromFileAsync(localPath, remotePath, createRemoteDir, existsMode, false, false, verifyOptions, token);
		}

		/// <summary>
		/// Uploads the specified file directly onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpExists.NoCheck"/> for fastest performance 
		/// but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful upload and what to do if it fails verification (See Remarks)</param>
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted the existsMode will automatically be set to <see cref="FtpExists.Overwrite"/>.
		/// </remarks>
		public async Task<bool> UploadFileAsync(string localPath, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false, FtpVerify verifyOptions = FtpVerify.None) {
			return await UploadFileAsync(localPath, remotePath, existsMode, createRemoteDir, verifyOptions, CancellationToken.None);
		}
#endif

		private bool UploadFileFromFile(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions) {
			//If retries are allowed set the retry counter to the allowed count
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			//Default validation to true (if verification isn't needed it'll allow a pass-through)
			bool verified = true;
			bool uploadSuccess;
			do {
				// write the file onto the server
				using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					//Upload file
					uploadSuccess = UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown);
					attemptsLeft--;
					//If verification is needed update the validated flag
					if (uploadSuccess && verifyOptions != FtpVerify.None) {
						verified = VerifyTransfer(localPath, remotePath);
						FtpTrace.WriteStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							//Force overwrite if a retry is required
							FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
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

#if NETFX45
		private async Task<bool> UploadFileFromFileAsync(string localPath, string remotePath, bool createRemoteDir, FtpExists existsMode,
			bool fileExists, bool fileExistsKnown, FtpVerify verifyOptions, CancellationToken token) {


			//If retries are allowed set the retry counter to the allowed count
			int attemptsLeft = verifyOptions.HasFlag(FtpVerify.Retry) ? m_retryAttempts : 1;
			//Default validation to true (if verification isn't needed it'll allow a pass-through)
			bool verified = true;
			bool uploadSuccess;
			do {
				using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					uploadSuccess = await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, fileExists, fileExistsKnown, token);
					attemptsLeft--;

					if (verifyOptions != FtpVerify.None) {
						verified = await VerifyTransferAsync(localPath, remotePath);
						FtpTrace.WriteStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
						if (!verified && attemptsLeft > 0) {
							//Force overwrite if a retry is required
							FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (existsMode != FtpExists.Overwrite ? "  Switching to FtpExists.Overwrite mode.  " : "  ") + attemptsLeft + " attempts remaining");
							existsMode = FtpExists.Overwrite;
						}
					}
				}
			} while (!verified && attemptsLeft > 0);

			if (uploadSuccess && !verified && verifyOptions.HasFlag(FtpVerify.Delete)) {
				await this.DeleteFileAsync(remotePath);
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
		public bool Upload(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {

			// verify args
			if (fileStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("Upload", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return UploadFileInternal(fileStream, remotePath, createRemoteDir, existsMode, false, false);
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
		public bool Upload(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {

			// verify args
			if (fileData == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("Upload", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (MemoryStream ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return UploadFileInternal(ms, remotePath, createRemoteDir, existsMode, false, false);
			}
		}


#if NETFX45

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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token) {

			// verify args
			if (fileStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("UploadAsync", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			return await UploadFileInternalAsync(fileStream, remotePath, createRemoteDir, existsMode, false, false, token);
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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode, bool createRemoteDir, CancellationToken token) {

			// verify args
			if (fileData == null)
				throw new ArgumentException("Required parameter is null or blank.", "fileData");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");

			FtpTrace.WriteFunc("UploadAsync", new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (MemoryStream ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return await UploadFileInternalAsync(ms, remotePath, createRemoteDir, existsMode, false, false, token);
			}
		}

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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(Stream fileStream, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {
			return await UploadAsync(fileStream, remotePath, existsMode, createRemoteDir, CancellationToken.None);
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
		/// <returns>If true then the file was uploaded, false otherwise.</returns>
		public async Task<bool> UploadAsync(byte[] fileData, string remotePath, FtpExists existsMode = FtpExists.Overwrite, bool createRemoteDir = false) {
			return await UploadAsync(fileData, remotePath, existsMode, createRemoteDir, CancellationToken.None);
		}
#endif

		#endregion

		#region Upload File Internal

		/// <summary>
		/// Upload the given stream to the server as a new file. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool UploadFileInternal(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown) {
			Stream upStream = null;

			try {

				long offset = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode != FtpExists.NoCheck) {
					if (!fileExistsKnown) {
						fileExists = FileExists(remotePath);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								FtpTrace.WriteStatus(FtpTraceLevel.Warn, "File " + remotePath + " exists on server & existsMode is set to FileExists.Skip");
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

				// seek to offset
				fileData.Position = offset;

				// open a file connection
				if (offset == 0) {
					upStream = OpenWrite(remotePath, UploadDataType);
				} else {
					upStream = OpenAppend(remotePath, UploadDataType);
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
							}

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var iex = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
								int code = (int)iex.SocketErrorCode;
#else
								int code = iex.ErrorCode;
#endif
								if (iex != null && code == 10054) {
									upStream.Dispose();
									upStream = OpenAppend(remotePath);
									upStream.Position = offset;
								} else throw;
							} else throw;

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

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var iex = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
								int code = (int)iex.SocketErrorCode;
#else
								int code = iex.ErrorCode;
#endif
								if (iex != null && code == 10054) {
									upStream.Dispose();
									upStream = OpenAppend(remotePath);
									upStream.Position = offset;
								} else {
									sw.Stop();
									throw;
								}
							} else {
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

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!EnableThreadSafeDataConnections) {
					FtpReply status = GetReply();
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

#if NETFX45
		/// <summary>
		/// Upload the given stream to the server as a new file asynchronously. Overwrites the file if it exists.
		/// Writes data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> UploadFileInternalAsync(Stream fileData, string remotePath, bool createRemoteDir, FtpExists existsMode, bool fileExists, bool fileExistsKnown, CancellationToken token) {
			Stream upStream = null;
			try {
				long offset = 0;

				// check if the file exists, and skip, overwrite or append
				if (existsMode != FtpExists.NoCheck) {
					if (!fileExistsKnown) {
						fileExists = await FileExistsAsync(remotePath);
					}
					switch (existsMode) {
						case FtpExists.Skip:
							if (fileExists) {
								return false;
							}
							break;
						case FtpExists.Overwrite:
							if (fileExists) {
								await DeleteFileAsync(remotePath);
							}
							break;
						case FtpExists.Append:
							if (fileExists) {
								offset = await GetFileSizeAsync(remotePath);
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
					if (!await DirectoryExistsAsync(dirname)) {
						await CreateDirectoryAsync(dirname);
					}
				}

				// seek to offset
				fileData.Position = offset;

				// open a file connection
				if (offset == 0) {
					upStream = await OpenWriteAsync(remotePath, UploadDataType);
				} else {
					upStream = await OpenAppendAsync(remotePath, UploadDataType);
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
							}
						} catch (IOException ex) {
							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var iex = ex.InnerException as System.Net.Sockets.SocketException;

								if (iex != null) {
#if CORE
							    int code = (int)iex.SocketErrorCode;
#else
									int code = iex.ErrorCode;
#endif
									if (code == 10054) {
										upStream.Dispose();
										//Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
										upStream = OpenAppend(remotePath);
										upStream.Position = offset;
									}
								} else throw;
							} else throw;
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
						} catch (IOException ex) {
							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var iex = ex.InnerException as System.Net.Sockets.SocketException;

								if (iex != null) {
#if CORE
							    int code = (int)iex.SocketErrorCode;
#else
									int code = iex.ErrorCode;
#endif
									if (code == 10054) {
										upStream.Dispose();
										//Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
										upStream = OpenAppend(remotePath);
										upStream.Position = offset;
									}
								} else {
									sw.Stop();
									throw;
								}
							} else {
								sw.Stop();
								throw;
							}
						}
					}
					sw.Stop();
				}

				// wait for while transfer to get over
				while (upStream.Position < upStream.Length) {
				}

				// disconnect FTP stream before exiting
				upStream.Dispose();

				// FIX : if this is not added, there appears to be "stale data" on the socket
				// listen for a success/failure reply
				if (!m_threadSafeDataChannels) {
					FtpReply status = GetReply();
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
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public bool DownloadFile(string localPath, string remotePath, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadFile", new object[] { localPath, remotePath, overwrite, verifyOptions });

			return DownloadFileToFile(localPath, remotePath, overwrite, verifyOptions);
		}

		private bool DownloadFileToFile(string localPath, string remotePath, bool overwrite, FtpVerify verifyOptions) {
			// skip downloading if the local file exists
			if (!overwrite && File.Exists(localPath)) {
				FtpTrace.WriteStatus(FtpTraceLevel.Error, "Overwrite is false and local file already exists.");
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
				using (var outStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
					// download the file straight to a file stream
					downloadSuccess = DownloadFileInternal(remotePath, outStream);
					attemptsLeft--;
				}

				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = VerifyTransfer(localPath, remotePath);
					FtpTrace.WriteLine(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
						FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
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

#if NETFX45
		/// <summary>
		/// Downloads the specified file onto the local file system asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <param name="token">The token to monitor for cancellation requests</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public async Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite, FtpVerify verifyOptions, CancellationToken token) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadFileAsync", new object[] { localPath, remotePath, overwrite, verifyOptions });

			return await DownloadFileToFileAsync(localPath, remotePath, overwrite, verifyOptions, token);
		}

		/// <summary>
		/// Downloads the specified file onto the local file system asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="localPath">The full or relative path to the file on the local file system</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="overwrite">True if you want the local file to be overwritten if it already exists. (Default value is true)</param>
		/// <param name="verifyOptions">Sets if checksum verification is required for a successful download and what to do if it fails verification (See Remarks)</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		/// <remarks>
		/// If verification is enabled (All options other than <see cref="FtpVerify.None"/>) the hash will be checked against the server.  If the server does not support
		/// any hash algorithm, then verification is ignored.  If only <see cref="FtpVerify.OnlyChecksum"/> is set then the return of this method depends on both a successful 
		/// upload &amp; verification.  Additionally, if any verify option is set and a retry is attempted then overwrite will automatically be set to true for subsequent attempts.
		/// </remarks>
		public async Task<bool> DownloadFileAsync(string localPath, string remotePath, bool overwrite = true, FtpVerify verifyOptions = FtpVerify.None) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadFileAsync", new object[] { localPath, remotePath, overwrite, verifyOptions });

			return await DownloadFileToFileAsync(localPath, remotePath, overwrite, verifyOptions, CancellationToken.None);
		}

		private async Task<bool> DownloadFileToFileAsync(string localPath, string remotePath, bool overwrite, FtpVerify verifyOptions, CancellationToken token) {
			if (string.IsNullOrWhiteSpace(localPath))
				throw new ArgumentNullException("localPath");

			// skip downloading if the local file exists
			if (!overwrite && File.Exists(localPath)) {
				FtpTrace.WriteStatus(FtpTraceLevel.Error, "Overwrite is false and local file already exists");
				return false;
			}

			try {
				// create the folders
				string dirPath = Path.GetDirectoryName(localPath);
				if (!String.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath)) {
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
				using (var outStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
					// download the file straight to a file stream
					downloadSuccess = await DownloadFileInternalAsync(remotePath, outStream, token);
					attemptsLeft--;
				}

				if (downloadSuccess && verifyOptions != FtpVerify.None) {
					verified = await VerifyTransferAsync(localPath, remotePath);
					FtpTrace.WriteStatus(FtpTraceLevel.Info, "File Verification: " + (verified ? "PASS" : "FAIL"));
#if DEBUG
					if (!verified && attemptsLeft > 0) {
						FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Retrying due to failed verification." + (overwrite ? "  Overwrite will occur." : "") + "  " + attemptsLeft + " attempts remaining");
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
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(Stream outStream, string remotePath) {

			// verify args
			if (outStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("Download", new object[] { remotePath });

			// download the file from the server
			return DownloadFileInternal(remotePath, outStream);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool Download(out byte[] outBytes, string remotePath) {

			// verify args
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("Download", new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (MemoryStream outStream = new MemoryStream()) {
				ok = DownloadFileInternal(remotePath, outStream);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}
			return ok;
		}

#if NETFX45
		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public async Task<bool> DownloadAsync(Stream outStream, string remotePath, CancellationToken token) {

			// verify args
			if (outStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadAsync", new object[] { remotePath });
			
			// download the file from the server
			return await DownloadFileInternalAsync(remotePath, outStream, token);
		}

		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public async Task<bool> DownloadAsync(Stream outStream, string remotePath) {

			// verify args
			if (outStream == null)
				throw new ArgumentException("Required parameter is null or blank.", "outStream");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadAsync", new object[] { remotePath });
			
			// download the file from the server
			return await DownloadFileInternalAsync(remotePath, outStream, CancellationToken.None);
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">The token to monitor cancellation requests</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadAsync(string remotePath, CancellationToken token) {

			// verify args
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			FtpTrace.WriteFunc("DownloadAsync", new object[] { remotePath });
			
			// download the file from the server
			using (MemoryStream outStream = new MemoryStream()) {
				bool ok = await DownloadFileInternalAsync(remotePath, outStream, token);
				return ok ? outStream.ToArray() : null;
			}
		}

		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadAsync(string remotePath) {

			// download the file from the server
			return await DownloadAsync(remotePath, CancellationToken.None);
		}
#endif

		#endregion

		#region Download File Internal

		/// <summary>
		/// Download a file from the server and write the data into the given stream.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private bool DownloadFileInternal(string remotePath, Stream outStream) {

			Stream downStream = null;

			try {

				// exit if file length == 0
				long fileLen = GetFileSize(remotePath);
				downStream = OpenRead(remotePath, DownloadDataType);

				if (fileLen == 0 && CurrentDataType == FtpDataType.ASCII) {

					// close stream before throwing error
					try {
						downStream.Dispose();
					} catch (Exception) { }

					throw new FtpException("Cannot download file with 0 length in ASCII mode. Use the FtpDataType.Binary data type and try again.");
				}


				// if the server has not reported a length for this file
				// we use an alternate method to download it - read until EOF
				bool readToEnd = (fileLen <= 0);


				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
				long offset = 0;
				if (DownloadRateLimit == 0) {
					while (offset < fileLen || readToEnd) {
						try {

							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							while ((readBytes = downStream.Read(buffer, 0, buffer.Length)) > 0) {

								// write chunk to output stream
								outStream.Write(buffer, 0, readBytes);
								offset += readBytes;
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd) {
								break;
							}

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var ie = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
							int code = (int)ie.SocketErrorCode;
#else
								int code = ie.ErrorCode;
#endif
								if (ie != null && code == 10054) {
									downStream.Dispose();
									downStream = OpenRead(remotePath, DownloadDataType, restart: offset);
								} else throw;
							} else throw;

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
							if (readToEnd) {
								break;
							}

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var ie = ex.InnerException as System.Net.Sockets.SocketException;
#if CORE
							int code = (int)ie.SocketErrorCode;
#else
								int code = ie.ErrorCode;
#endif
								if (ie != null && code == 10054) {
									downStream.Dispose();
									downStream = OpenRead(remotePath, DownloadDataType, restart: offset);
								} else {
									sw.Stop();
									throw;
								}
							} else {
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
					FtpTrace.WriteStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
		}

#if NETFX45
		/// <summary>
		/// Download a file from the server and write the data into the given stream asynchronously.
		/// Reads data in chunks. Retries if server disconnects midway.
		/// </summary>
		private async Task<bool> DownloadFileInternalAsync(string remotePath, Stream outStream, CancellationToken token) {
			Stream downStream = null;
			try {
			    // exit if file length == 0
                long fileLen = GetFileSize(remotePath);
				downStream = await OpenReadAsync(remotePath, DownloadDataType);
				if (fileLen == 0 && CurrentDataType == FtpDataType.ASCII) {
					// close stream before throwing error
					try {
						downStream.Dispose();
					} catch (Exception) { }

				    throw new FtpException("Cannot download file with 0 length in ASCII mode. Use the FtpDataType.Binary data type and try again.");
				}

                // if the server has not reported a length for this file
                // we use an alternate method to download it - read until EOF
                bool readToEnd = (fileLen <= 0);

				// loop till entire file downloaded
				byte[] buffer = new byte[TransferChunkSize];
				long offset = 0;
				if (DownloadRateLimit == 0) {
					while (offset < fileLen || readToEnd) {
						try {
							// read a chunk of bytes from the FTP stream
							int readBytes = 1;
							while ((readBytes = await downStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
								// write chunk to output stream
								await outStream.WriteAsync(buffer, 0, readBytes, token);
								offset += readBytes;
							}

							// if we reach here means EOF encountered
							// stop if we are in "read until EOF" mode
							if (readToEnd) {
								break;
							}

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var ie = ex.InnerException as System.Net.Sockets.SocketException;
								if (ie != null) {
#if CORE
		    					int code = (int)ie.SocketErrorCode;
#else
									int code = ie.ErrorCode;
#endif
									if (code == 10054) {
										downStream.Dispose();
										//Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
										downStream = OpenRead(remotePath, restart: offset);
									}
								} else throw;
							} else throw;

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
							if (readToEnd) {
								break;
							}

						} catch (IOException ex) {

							// resume if server disconnects midway (fixes #39)
							if (ex.InnerException != null) {
								var ie = ex.InnerException as System.Net.Sockets.SocketException;
								if (ie != null) {
#if CORE
		    					int code = (int)ie.SocketErrorCode;
#else
									int code = ie.ErrorCode;
#endif
									if (code == 10054) {
										downStream.Dispose();
										//Async not allowed in catch block until C# version 6.0.  Use Synchronous Method
										downStream = OpenRead(remotePath, restart: offset);
									}
								} else {
									sw.Stop();
									throw;
								}
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
					FtpTrace.WriteStatus(FtpTraceLevel.Error, "File does not exist: " + ex1);
					return false;
				}

				// catch errors during upload
				throw new FtpException("Error while downloading the file from the server. See InnerException for more info.", ex1);
			}
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

#if NETFX45
		private async Task<bool> VerifyTransferAsync(string localPath, string remotePath) {

			// verify args
			if (localPath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			if (remotePath.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			
			if (this.HasFeature(FtpCapability.HASH) || this.HasFeature(FtpCapability.MD5) ||
				this.HasFeature(FtpCapability.XMD5) || this.HasFeature(FtpCapability.XCRC) ||
				this.HasFeature(FtpCapability.XSHA1) || this.HasFeature(FtpCapability.XSHA256) ||
				this.HasFeature(FtpCapability.XSHA512)) {
				FtpHash hash = await this.GetChecksumAsync(remotePath);
				if (!hash.IsValid)
					return false;

				return hash.Verify(localPath);
			}

			//Not supported return true to ignore validation
			return true;
		}
#endif

		#endregion

	}
}