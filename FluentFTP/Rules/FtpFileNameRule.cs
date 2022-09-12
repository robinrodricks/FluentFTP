using System;
using System.Collections.Generic;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept files that have the given name, or exclude files of a given name.
	/// </summary>
	public class FtpFileNameRule : FtpRule {

		/// <summary>
		/// If true, only files of the given name are uploaded or downloaded. If false, files of the given name are excluded.
		/// </summary>
		public bool Whitelist { get; set; }

		/// <summary>
		/// The files names to match
		/// </summary>
		public IList<string> Names { get; set; }

		/// <summary>
		/// Only accept files that have the given name, or exclude files of a given name.
		/// </summary>
		/// <param name="whitelist">If true, only files of the given name are downloaded. If false, files of the given name are excluded.</param>
		/// <param name="names">The files names to match</param>
		public FtpFileNameRule(bool whitelist, IList<string> names) {
			this.Whitelist = whitelist;
			this.Names = names;
		}

		/// <summary>
		/// Checks if the files has the given name, or exclude files of the given name.
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {
			if (item.Type == FtpObjectType.File) {
				var fileName = item.Name;
				if (Whitelist) {
					return Names.Contains(fileName);
				}
				else {
					return !Names.Contains(fileName);
				}
			}
			else {
				return true;
			}
		}

	}
}