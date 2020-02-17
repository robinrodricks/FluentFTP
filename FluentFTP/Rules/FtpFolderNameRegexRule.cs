using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept folders whose names match the given regular expression, or exclude files that match.
	/// This rule does not check the folder paths of file objects. Only folders are checked.
	/// </summary>
	public class FtpFolderRegexRule : FtpRule {

		/// <summary>
		/// If true, only folders where one of the supplied regex pattern matches are uploaded or downloaded.
		/// If false, folders where one of the supplied regex pattern matches are excluded.
		/// </summary>
		public bool Whitelist;

		/// <summary>
		/// The files names to match
		/// </summary>
		public IList<string> RegexPatterns;

		/// <summary>
		/// Only accept items that one of the supplied regex pattern.
		/// </summary>
		/// <param name="whitelist">If true, only folders where one of the supplied regex pattern matches are uploaded or downloaded. If false, folders where one of the supplied regex pattern matches are excluded.</param>
		/// <param name="regexPatterns">The list of regex patterns to match. Only valid patterns are accepted and stored. If none of the patterns are valid, this rule is disabled and passes all objects.</param>
		public FtpFolderRegexRule(bool whitelist, IList<string> regexPatterns) {
			this.Whitelist = whitelist;
			this.RegexPatterns = regexPatterns.Where(x => x.IsValidRegEx()).ToList();
		}

		/// <summary>
		/// Checks if the FtpListItem Name does match any RegexPattern
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {

			// if no valid regex patterns, accept all objects
			if (RegexPatterns.Count == 0) {
				return true;
			}

			// only check folders
			if (item.Type == FtpFileSystemObjectType.Directory) {
				var folderName = item.Name;

				if (Whitelist) {
					return RegexPatterns.Any(x => Regex.IsMatch(folderName, x));
				}
				else {
					return !RegexPatterns.Any(x => Regex.IsMatch(folderName, x));
				}
			}
			else {
				return true;
			}

		}

	}
}
