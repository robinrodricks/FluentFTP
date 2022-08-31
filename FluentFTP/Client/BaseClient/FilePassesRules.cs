using System.Collections.Generic;
using FluentFTP.Rules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Returns true if the file passes all the rules
		/// </summary>
		protected bool FilePassesRules(FtpResult result, List<FtpRule> rules, bool useLocalPath, FtpListItem item = null) {
			if (rules != null && rules.Count > 0) {
				var passes = FtpRule.IsAllAllowed(rules, item ?? result.ToListItem(useLocalPath));
				if (!passes) {

					LogWithPrefix(FtpTraceLevel.Info, "Skipped file due to rule: " + (useLocalPath ? result.LocalPath : result.RemotePath));

					// mark that the file was skipped due to a rule
					result.IsSkipped = true;
					result.IsSkippedByRule = true;

					// skip uploading the file
					return false;
				}
			}
			return true;
		}

	}
}
