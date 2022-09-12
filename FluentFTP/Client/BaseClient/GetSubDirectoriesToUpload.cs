using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Get a list of all the sub directories that need to be created within the main directory
		/// </summary>
		protected List<FtpResult> GetSubDirectoriesToUpload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, string[] dirListing) {

			var dirsToUpload = new List<FtpResult>();

			foreach (var localFile in dirListing) {

				// calculate the local path
				var relativePath = localFile.RemovePrefix(localFolder).RemovePrefix("\\").RemovePrefix("/").EnsurePostfix(Path.DirectorySeparatorChar.ToString());
				var remoteFile = remoteFolder.EnsurePostfix("/") + relativePath.Replace('\\', '/');

				// create the result object
				var result = new FtpResult() {
					Type = FtpObjectType.Directory,
					Size = 0,
					Name = Path.GetDirectoryName(localFile),
					RemotePath = remoteFile,
					LocalPath = localFile,
					IsDownload = false,
				};

				// record the folder
				results.Add(result);

				// skip uploading the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				dirsToUpload.Add(result);
			}

			return dirsToUpload;
		}

		/// <summary>
		/// Check if the remote file can be deleted, based on the UploadDirectoryDeleteExcluded property
		/// </summary>
		protected bool CanDeleteRemoteFile(List<FtpRule> rules, FtpListItem existingServerFile) {

			// if we should not delete excluded files
			if (!Config.UploadDirectoryDeleteExcluded && !rules.IsBlank()) {

				// create the result object to validate rules to ensure that file from excluded
				// directories are not deleted on the FTP remote server
				var result = new FtpResult() {
					Type = existingServerFile.Type,
					Size = existingServerFile.Size,
					Name = Path.GetFileName(existingServerFile.FullName),
					RemotePath = existingServerFile.FullName,
					IsDownload = false,
				};

				// check if the file passes the rules
				if (FilePassesRules(result, rules, false)) {
					// delete the file because it is included
					return true;
				}
				else {
					// do not delete the file because it is excluded
					return false;
				}
			}

			// always delete the file whether its included or excluded by the rules
			return true;
		}

	}
}