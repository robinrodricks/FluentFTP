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
		/// Get a list of all the files that need to be uploaded within the main directory
		/// </summary>
		protected List<FtpResult> GetFilesToUpload(string localFolder, string remoteFolder, List<FtpRule> rules, List<FtpResult> results, Dictionary<string, bool> shouldExist, string[] fileListing) {

			var filesToUpload = new List<FtpResult>();

			foreach (var localFile in fileListing) {

				// calculate the local path
				var relativePath = localFile.Replace(localFolder, "").Replace(Path.DirectorySeparatorChar, '/');
				var remoteFile = remoteFolder + relativePath.Replace('\\', '/');

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

				// skip uploading the file if it does not pass all the rules
				if (!FilePassesRules(result, rules, true)) {
					continue;
				}

				// record that this file should exist
				shouldExist.Add(remoteFile.ToLower(), true);

				// absorb errors
				filesToUpload.Add(result);
			}

			return filesToUpload;
		}

	}
}
