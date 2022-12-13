using System;
using System.Collections.Generic;
using System.IO;
using FluentFTP.Helpers;
using FluentFTP.Rules;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {


		/// <summary>
		/// Delete the extra local files if in mirror mode
		/// </summary>
		protected void DeleteExtraLocalFiles(string localFolder, FtpFolderSyncMode mode, Dictionary<string, bool> shouldExist, List<FtpRule> rules) {
			if (mode == FtpFolderSyncMode.Mirror) {

				LogFunction(nameof(DeleteExtraLocalFiles));

				// get all the local files
				var localListing = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

				// delete files that are not in listed in shouldExist
				foreach (var existingLocalFile in localListing) {

					if (!shouldExist.ContainsKey(existingLocalFile.ToLower())) {

						// only delete the local file if its permitted by the configuration
						if (CanDeleteLocalFile(rules, existingLocalFile)) {
							LogWithPrefix(FtpTraceLevel.Info, "Delete extra file from disk: " + existingLocalFile);

							// delete the file from disk
							try {
								File.Delete(existingLocalFile);
							}
							catch {
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Check if the local file can be deleted, based on the DownloadDirectoryDeleteExcluded property
		/// </summary>
		protected bool CanDeleteLocalFile(List<FtpRule> rules, string existingLocalFile) {

			// if we should not delete excluded files
			if (!Config.DownloadDirectoryDeleteExcluded && !rules.IsBlank()) {

				// create the result object to validate rules to ensure that file from excluded
				// directories are not deleted on the local filesystem
				var result = new FtpResult() {
					Type = FtpObjectType.File,
					Size = 0,
					Name = Path.GetFileName(existingLocalFile),
					LocalPath = existingLocalFile,
					IsDownload = false,
				};

				// check if the file passes the rules
				if (FilePassesRules(result, rules, true)) {
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
