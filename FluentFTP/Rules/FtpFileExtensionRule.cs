using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentFTP.Helpers;

namespace FluentFTP.Rules {

	/// <summary>
	/// Only accept files that have the given extension, or exclude files of a given extension.
	/// </summary>
	public class FtpFileExtensionRule : FtpRule {

		/// <summary>
		/// If true, only files of the given extension are uploaded or downloaded. If false, files of the given extension are excluded.
		/// </summary>
		public bool Whitelist;

		/// <summary>
		/// The extensions to match
		/// </summary>
		public IList<string> Exts;

		/// <summary>
		/// Only accept files that have the given extension, or exclude files of a given extension.
		/// </summary>
		/// <param name="whitelist">If true, only files of the given extension are uploaded or downloaded. If false, files of the given extension are excluded.</param>
		/// <param name="exts">The extensions to match</param>
		public FtpFileExtensionRule(bool whitelist, IList<string> exts) {
			this.Whitelist = whitelist;
			this.Exts = exts;
		}

		/// <summary>
		/// Checks if the files has the given extension, or exclude files of the given extension.
		/// </summary>
		public override bool IsAllowed(FtpListItem item) {
			if (item.Type == FtpObjectType.File) {
				var ext = Path.GetExtension(item.Name).Replace(".", "").ToLower();
				if (Whitelist) {

					// whitelist
					if (ext.IsBlank()) {
						return false;
					}
					else {
						return Exts.Contains(ext);
					}
				}
				else {

					// blacklist
					if (ext.IsBlank()) {
						return true;
					}
					else {
						return !Exts.Contains(ext);
					}
				}
			}
			else {
				return true;
			}
		}

	}
}