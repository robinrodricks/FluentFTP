using System;
using System.Collections.Generic;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept folders that have the given name, or exclude folders of a given name.
	/// </summary>
	public class FtpFolderNameRule : FtpRule {

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
		public bool Whitelist;

		/// <summary>
		/// The folder names to match
		/// </summary>
		public IList<string> Names;

		/// <summary>
		/// Only accept folders that have the given name, or exclude folders of a given name.
		/// </summary>
		/// <param name="whitelist">If true, only folders of the given name are downloaded. If false, folders of the given name are excluded.</param>
		/// <param name="names">The folder names to match</param>
		public FtpFolderNameRule(bool whitelist, IList<string> names) {
			this.Whitelist = whitelist;
			this.Names = names;
		}

		/// <summary>
		/// Checks if the folders has the given name, or exclude folders of the given name.
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {

			// get the folder name of this item
			string[] dirNameParts = null;
			if (item.Type == FtpFileSystemObjectType.File) {
				dirNameParts = item.FullName.GetFtpDirectoryName().GetPathSegments();
			}
			else if (item.Type == FtpFileSystemObjectType.Directory) {
				dirNameParts = item.FullName.GetPathSegments();
			}
			else {
				return true;
			}

			// check against whitelist or blacklist
			if (Whitelist) {

				// whitelist
				foreach (var dirName in dirNameParts) {
					if (Names.Contains(dirName.Trim())) {
						return true;
					}
				}
				return false;
			}
			else {

				// blacklist
				foreach (var dirName in dirNameParts) {
					if (Names.Contains(dirName.Trim())) {
						return false;
					}
				}
				return true;
			}
		}

	}
}