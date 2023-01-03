using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Linq;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Get a list of all the files and folders that need to be downloaded
		/// </summary>
		protected List<FtpResult> GetFilesToDownload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, FtpListItem[] listing, Dictionary<string, bool> shouldExist) {

			var toDownload = new List<FtpResult>();

			foreach (var remoteFile in listing) {

				// only files and folders are processed
				if (remoteFile.Type == FtpObjectType.File ||
					remoteFile.Type == FtpObjectType.Directory) {

					// calculate the local path
					var relativePath = remoteFile.FullName.EnsurePrefix("/").RemovePrefix(remoteFolder).Replace('/', Path.DirectorySeparatorChar);
					var localFile = localFolder.CombineLocalPath(relativePath);
					RecordFileToDownload(rules, results, shouldExist, toDownload, remoteFile, localFile);

				}
			}

			return toDownload;
		}

		/// <summary>
		/// Get a list of all the files and folders that need to be downloaded
		/// </summary>
		protected List<FtpResult> GetFilesToDownload2(string localFolder, IEnumerable<string> remotePaths, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist) {

			var toDownload = new List<FtpResult>();

			foreach (var remotePath in remotePaths) {

				// calc local path
				var localPath = localFolder.CombineLocalPath(remotePath.GetFtpFileName());

				RecordFileToDownload(rules, results, shouldExist, toDownload, null, localPath, remotePath);

			}

			return toDownload;
		}

		/// <summary>
		/// Create an FtpResult object for the given file to be downloaded, and check if the file passes the rules.
		/// </summary>
		protected void RecordFileToDownload(List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, List<FtpResult> toDownload, FtpListItem remoteFile, string localFile, string remoteFilePath = null) {

			// create the result object
			FtpResult result;
			if (remoteFile != null) {
				result = new FtpResult() {
					Type = remoteFile.Type,
					Size = remoteFile.Size,
					Name = remoteFile.Name,
					RemotePath = remoteFile.FullName,
					LocalPath = localFile,
					IsDownload = true,
				};
			}
			else {
				result = new FtpResult() {
					Type = FtpObjectType.File,
					Size = 0,
					Name = remoteFilePath.GetFtpFileName(),
					RemotePath = remoteFilePath,
					LocalPath = localFile,
					IsDownload = true,
				};
			}

			// record the file
			results.Add(result);

			// only download the file if it passes all the rules
			if (FilePassesRules(result, rules, false, remoteFile)) {

				// record that this file/folder should exist
				shouldExist.Add(localFile.ToLower(), true);

				// only files are processed
				toDownload.Add(result);

			}
		}
	}
}