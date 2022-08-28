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

				// calculate the local path
				var relativePath = remoteFile.FullName.EnsurePrefix("/").RemovePrefix(remoteFolder).Replace('/', Path.DirectorySeparatorChar);
				var localFile = localFolder.CombineLocalPath(relativePath);

				// create the result object
				var result = new FtpResult() {
					Type = remoteFile.Type,
					Size = remoteFile.Size,
					Name = remoteFile.Name,
					RemotePath = remoteFile.FullName,
					LocalPath = localFile,
					IsDownload = true,
				};

				// only files and folders are processed
				if (remoteFile.Type == FtpObjectType.File ||
					remoteFile.Type == FtpObjectType.Directory) {


					// record the file
					results.Add(result);

					// skip downloading the file if it does not pass all the rules
					if (!FilePassesRules(result, rules, false, remoteFile)) {
						continue;
					}

					// record that this file/folder should exist
					shouldExist.Add(localFile.ToLower(), true);

					// only files are processed
					toDownload.Add(result);


				}
			}

			return toDownload;
		}

	}
}