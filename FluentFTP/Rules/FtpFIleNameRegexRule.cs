using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept files that have the given name, or exclude files of a given name.
	/// </summary>
	public class FtpFIleNameRegexRule : FtpRule {

		/// <summary>
		/// If true, only items where one of the supplied regex pattern matches are download. If false, items where one of the supplied regex pattern matches are excluded.
		/// </summary>
		public bool Whitelist;

		/// <summary>
		/// The files names to match
		/// </summary>
		public IList<string> RegexPatterns;

		/// <summary>
		/// Only accept items that one of the supplied regex pattern.
		/// </summary>
		/// <param name="whitelist">If true, only items where one of the supplied regex pattern matches are download. If false, items where one of the supplied regex pattern matches are excluded.</param>
		/// <param name="regexPatterns">The list of regex pattern to match</param>
		public FtpFIleNameRegexRule(bool whitelist, IList<string> regexPatterns) {
			this.Whitelist = whitelist;
			this.RegexPatterns = regexPatterns.Where(x => x.IsValidRegEx()).ToList();
		}

		/// <summary>
		/// Checks if the FtpListItem Name does match any RegexPattern
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {

			var fileName = item.Name;

			if (Whitelist)
			{
				return RegexPatterns.Any(x => Regex.IsMatch(fileName, x));
			}
			else
			{
				return !RegexPatterns.Any(x => Regex.IsMatch(fileName, x));
			}
		}

	}
}
