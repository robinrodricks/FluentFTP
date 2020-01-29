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

		/// <summary>
		/// Downloads the specified directory onto the local file system.
		/// In Mirror mode, we will delete extra files from the local filesystem that are not present on the server.
		/// In Update mode, we will only download files onto the local filesystem and preserve any extra files.
		/// Only downloads the files and folders matching all the rules provided, if any.
		/// </summary>
		/// <returns>
		/// Returns a listing of all the remote files, indicating if they were downloaded, skipped or overwritten.
		/// Returns a blank list if nothing was transfered. Never returns null.
		/// </returns>
		public List<FtpResult> DownloadDirectory(string localFolder, string remoteFolder, FtpFolderSyncMode folderSyncMode = FtpFolderSyncMode.Update, FtpLocalExists existsMode = FtpLocalExists.Skip, FtpVerify verifyOptions = FtpVerify.None, List<FtpRule> rules = null, Action<FtpProgress> progress = null) {

			if (localFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localFolder");
			}

			if (remoteFolder.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remoteFolder");
			}

			LogFunc("DownloadDirectory", new object[] { localFolder, remoteFolder, folderSyncMode, existsMode, verifyOptions });

			var results = new List<FtpResult>();

			// ensure ends with slash
			localFolder = !localFolder.EndsWith(Path.DirectorySeparatorChar.ToString()) ? localFolder + Path.DirectorySeparatorChar.ToString() : localFolder;

			// cleanup the remote path
			remoteFolder = remoteFolder.GetFtpPath();

			// if the dir does not exist or some error, fail fast
			if (!DirectoryExists(remoteFolder)) {
				return results;
			}

			// ensure the local dir exists
			localFolder.EnsureDirectory();

			// get all the files in the remote directory
			var listing = GetListing(remoteFolder, FtpListOption.Recursive | FtpListOption.Size);

			// loop thru each file and transfer it
			foreach (var remoteFile in listing) {

				// calculate the local path
				var relativeFilePath = remoteFile.FullName.Replace(remoteFolder, "");
				var localFile = Path.Combine(localFolder, relativeFilePath);

				// create the result object
				var result = new FtpResult() {
					Type = remoteFile.Type,
					Size = remoteFile.Size,
					Name = remoteFile.Name,
					RemotePath = remoteFile.FullName,
					LocalPath = localFile
				};

				// only files are processed
				if (remoteFile.Type == FtpFileSystemObjectType.File) {

					// absorb errors
					try {

						// download the file
						var transferred = this.DownloadFile(result.LocalPath, result.RemotePath, existsMode, verifyOptions, progress);
						result.IsSuccess = true;
						result.IsSkipped = !transferred;
					}
					catch (Exception ex) {

						// absorb and record errors
						result.IsFailed = true;
						result.Exception = ex;
					}

					// record it
					results.Add(result);


				}
				else if (remoteFile.Type == FtpFileSystemObjectType.Directory) {

					// absorb errors
					try {

						// create directory on local filesystem
						// to ensure we download the blank remote dirs as well
						var created = result.LocalPath.EnsureDirectory();
						result.IsSuccess = true;
						result.IsSkipped = !created;

					}
					catch (Exception ex) {

						// absorb and record errors
						result.IsFailed = true;
						result.Exception = ex;
					}

					// record it
					results.Add(result);

				}

			}

			// delete the extra local files if in mirror mode
			if (folderSyncMode == FtpFolderSyncMode.Mirror) {

				// get all the local files
				//var localListing = ?;

			}

			return results;
		}

	}
}