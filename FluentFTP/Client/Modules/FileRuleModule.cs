using System.Collections.Generic;
using System.IO;
using FluentFTP.Client.BaseClient;
using FluentFTP.Helpers;
using FluentFTP.Rules;

namespace FluentFTP.Client.Modules {
	internal static class FileRuleModule {

		/// <summary>
		/// Returns true if the file passes all the rules
		/// </summary>
		public static bool FilePassesRules(BaseFtpClient client, FtpResult result, List<FtpRule> rules, bool useLocalPath, FtpListItem item = null) {
			if (rules != null && rules.Count > 0) {
				var passes = FtpRule.IsAllAllowed(rules, item ?? result.ToListItem(useLocalPath));
				if (!passes) {

					client.LogWithPrefix(FtpTraceLevel.Info, "Skipped file due to rule: " + (useLocalPath ? result.LocalPath : result.RemotePath));

					// mark that the file was skipped due to a rule
					result.IsSkipped = true;
					result.IsSkippedByRule = true;

					// skip uploading the file
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Check if the local file can be deleted, based on the DownloadDirectoryDeleteExcluded property
		/// </summary>
		public static bool CanDeleteLocalFile(BaseFtpClient client, List<FtpRule> rules, string existingLocalFile) {

			// if we should not delete excluded files
			if (!client.Config.DownloadDirectoryDeleteExcluded && !rules.IsBlank()) {

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
				if (FileRuleModule.FilePassesRules(client, result, rules, true)) {
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
