using System;
using System.Collections.Generic;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept folders that have the given name, or exclude folders of a given name.
	/// </summary>
	public class FtpFolderNameRule : FtpRule {

		/// <summary>
		/// Common folders to blacklist
		/// </summary>
		public static List<string> CommonBlacklistedFolders = new List<string> {
			".git",
			".svn",
			".DS_Store",
			"node_modules",
		};

		/// <summary>
		/// If true, only folders of the given name are uploaded or downloaded.
		/// If false, folders of the given name are excluded.
		/// </summary>
		public bool Whitelist { get; set; }

		/// <summary>
		/// The folder names to match
		/// </summary>
		public IList<string> Names { get; set; }

		/// <summary>
		/// Which path segment to start checking from
		/// </summary>
		public int StartSegment { get; set; }

		/// <summary>
		/// Only accept folders that have the given name, or exclude folders of a given name.
		/// </summary>
		/// <param name="whitelist">If true, only folders of the given name are downloaded. If false, folders of the given name are excluded.</param>
		/// <param name="names">The folder names to match</param>
		/// <param name="startSegment">Which path segment to start checking from. 0 checks root folder onwards. 1 skips root folder.</param>
		public FtpFolderNameRule(bool whitelist, IList<string> names, int startSegment = 0) {
			this.Whitelist = whitelist;
			this.Names = names;
			this.StartSegment = startSegment;
		}

		/// <summary>
		/// Checks if the folders has the given name, or exclude folders of the given name.
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {

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
					if (Names.Contains(dirName.Trim())) {
						return true;
					}
				}
				return false;
			}
			else {

				// loop through path segments starting at given index
				for (int d = StartSegment; d < dirNameParts.Length; d++) {
					var dirName = dirNameParts[d];

					// blacklist
					if (Names.Contains(dirName.Trim())) {
						return false;
					}
				}
				return true;
			}
		}

	}
}