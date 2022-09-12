using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept files whose names match the given regular expression(s), or exclude files that match.
	/// </summary>
	public class FtpFileNameRegexRule : FtpRule {

		/// <summary>
		/// If true, only items where one of the supplied regex pattern matches are uploaded or downloaded.
		/// If false, items where one of the supplied regex pattern matches are excluded.
		/// </summary>
		public bool Whitelist { get; set; }

		/// <summary>
		/// The files names to match
		/// </summary>
		public List<string> RegexPatterns { get; set; }

		/// <summary>
		/// Only accept items that match one of the supplied regex patterns.
		/// </summary>
		/// <param name="whitelist">If true, only items where one of the supplied regex pattern matches are uploaded or downloaded. If false, items where one of the supplied regex pattern matches are excluded.</param>
		/// <param name="regexPatterns">The list of regex patterns to match. Only valid patterns are accepted and stored. If none of the patterns are valid, this rule is disabled and passes all objects.</param>
		public FtpFileNameRegexRule(bool whitelist, IList<string> regexPatterns) {
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

			// only check files
			if (item.Type == FtpObjectType.File) {
				var fileName = item.Name;

				if (Whitelist) {
					return RegexPatterns.Any(x => Regex.IsMatch(fileName, x));
				}
				else {
					return !RegexPatterns.Any(x => Regex.IsMatch(fileName, x));
				}
			}
			else {
				return true;
			}
		}

	}
}