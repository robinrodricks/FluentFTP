using System;
using System.IO;
using System.Collections.Generic;
using FluentFTP.Rules;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Linq;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Client.Modules {
	internal static class FileUploadModule {

		/// <summary>
		/// Check if the file is cleared to be uploaded, taking its existence/filesize and existsMode options into account.
		/// </summary>
		public static bool CanUploadFile(BaseFtpClient client, FtpResult result, FtpListItem[] remoteListing, FtpRemoteExists existsMode, out FtpRemoteExists existsModeToUse) {

			// check if the file already exists on the server
			existsModeToUse = existsMode;
			var fileExists = FileListings.FileExistsInListing(remoteListing, result.RemotePath);

			// if we want to skip uploaded files and the file already exists, mark its skipped
			if (existsMode == FtpRemoteExists.Skip && fileExists) {

				client.LogWithPrefix(FtpTraceLevel.Info, "Skipped file that already exists: " + result.LocalPath);

				result.IsSuccess = true;
				result.IsSkipped = true;
				return false;
			}

			// in any mode if the file does not exist, mark that exists check is not required
			if (!fileExists) {
				existsModeToUse = existsMode == FtpRemoteExists.Resume ? FtpRemoteExists.ResumeNoCheck : FtpRemoteExists.NoCheck;
			}
			return true;
		}

		/// <summary>
		/// Get a list of all the files that need to be uploaded within the main directory
		/// </summary>
		public static List<FtpResult> GetFilesToUpload(BaseFtpClient client, string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing) {

			var filesToUpload = new List<FtpResult>();

			foreach (var localFile in fileListing) {

				// calculate the local path
				var relativePath = localFile.Replace(localFolder, "").Replace(Path.DirectorySeparatorChar, '/');
				var remoteFile = remoteFolder + relativePath.Replace('\\', '/');

				// record that this file should be uploaded
				RecordFileToUpload(client, rules, results, shouldExist, filesToUpload, localFile, remoteFile);
			}

			return filesToUpload;
		}

		/// <summary>
		/// Create an FtpResult object for the given file to be uploaded, and check if the file passes the rules.
		/// </summary>
		public static void RecordFileToUpload(BaseFtpClient client, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, List<FtpResult> filesToUpload, string localFile, string remoteFile) {

			// create the result object
			var result = new FtpResult() {
				Type = FtpObjectType.File,
				Size = new FileInfo(localFile).Length,
				Name = Path.GetFileName(localFile),
				RemotePath = remoteFile,
				LocalPath = localFile
			};

			// record the file
			results.Add(result);

			// only upload the file if it passes all the rules
			if (FileRuleModule.FilePassesRules(client, result, rules, true)) {

				// record that this file should exist
				shouldExist.Add(remoteFile.ToLower(), true);

				// absorb errors
				filesToUpload.Add(result);

			}
		}


	}
}
