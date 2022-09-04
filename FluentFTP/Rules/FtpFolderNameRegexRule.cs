using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept folders whose names match the given regular expression(s), or exclude folders that match.
	/// </summary>
	public class FtpFolderRegexRule : FtpRule {

		/// <summary>
		/// If true, only folders where one of the supplied regex pattern matches are uploaded or downloaded.
		/// If false, folders where one of the supplied regex pattern matches are excluded.
		/// </summary>
		public bool Whitelist { get; set; }

		/// <summary>
		/// The files names to match
		/// </summary>
		public List<string> RegexPatterns { get; set; }

		/// <summary>
		/// Which path segment to start checking from
		/// </summary>
		public int StartSegment { get; set; }

		/// <summary>
		/// Only accept items that one of the supplied regex pattern.
		/// </summary>
		/// <param name="whitelist">If true, only folders where one of the supplied regex pattern matches are uploaded or downloaded. If false, folders where one of the supplied regex pattern matches are excluded.</param>
		/// <param name="regexPatterns">The list of regex patterns to match. Only valid patterns are accepted and stored. If none of the patterns are valid, this rule is disabled and passes all objects.</param>
		/// <param name="startSegment">Which path segment to start checking from. 0 checks root folder onwards. 1 skips root folder.</param>
		public FtpFolderRegexRule(bool whitelist, IList<string> regexPatterns, int startSegment = 0) {
			this.Whitelist = whitelist;
			this.RegexPatterns = regexPatterns.Where(x => x.IsValidRegEx()).ToList();
			this.StartSegment = startSegment;
		}

		/// <summary>
		/// Checks if the FtpListItem Name does match any RegexPattern
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {

			// if no valid regex patterns, accept all objects
			if (RegexPatterns.Count == 0) {
				return true;
			}

			// get the folder name of this item
			string[] dirNameParts = null;
			if (item.Type == FtpObjectType.File) {
				dirNameParts = item.FullName.GetFtpDirectoryName().GetPathSegments();
			}
			else if (item.Type == FtpObjectType.Directory) {
				dirNameParts = item.FullName.GetPathSegments();
			}
			else {
				return true;
			}

			// check against whitelist or blacklist
			if (Whitelist) {

				// loop through path segments starting at given index
				for (int d = StartSegment; d < dirNameParts.Length; d++) {
					var dirName = dirNameParts[d];

					// whitelist
					foreach (var pattern in RegexPatterns) {
						if (Regex.IsMatch(dirName.Trim(), pattern)) {
							return true;
						}
					}
				}
				return false;
			}
			else {

				// loop through path segments starting at given index
				for (int d = StartSegment; d < dirNameParts.Length; d++) {
					var dirName = dirNameParts[d];

					// blacklist
					foreach (var pattern in RegexPatterns) {
						if (Regex.IsMatch(dirName.Trim(), pattern)) {
							return false;
						}
					}
				}
				return true;
			}

		}

	}
}
