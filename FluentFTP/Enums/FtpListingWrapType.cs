using System;

namespace FluentFTP {
	/// <summary>
	/// How to handle entry of listing commands
	/// </summary>
	public enum FtpListingWrapType {
		/// <summary>
		/// Just perform the listing command directly with the desired pathname
		/// </summary>
		None,

		/// <summary>
		/// Always execute a CWD command to change to the directory of the desired listing
		/// prior to the list command (which is then performed without any path as parameter),
		/// and switch back to the original directory after the list command.
		/// </summary>
		Always,

		/// <summary>
		/// If the desired listing path contains blanks, then execute a CWD command to change to the directory of the desired listing
		/// prior to the list command (which is then performed without any path as parameter),
		/// and switch back to the original directory after the list command, otherwise if there
		/// are no blanks involved then perform the list command in the normal fashion.
		/// </summary>
		IfBlanks

	}
}