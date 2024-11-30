using System;
using System.Collections.Generic;
using System.IO;
using FluentFTP.Client.Modules;
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
						if (FileRuleModule.CanDeleteLocalFile(this, rules, existingLocalFile)) {
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

	}
}
