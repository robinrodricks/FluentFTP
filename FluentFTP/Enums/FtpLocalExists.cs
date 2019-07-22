using System;

namespace FluentFTP {
	/// <summary>
	/// Determines how we handle partially downloaded files
	/// </summary>
	public enum FtpLocalExists {
		/// <summary>
		/// Restart the download of a file if it is partially downloaded.
		/// Overwrites the file if it exists on disk.
		/// </summary>
		Overwrite,

		/// <summary>
		/// Resume the download of a file if it is partially downloaded.
		/// Appends to the file if it exists, by checking the length and adding the missing data.
		/// If the file doesn't exist on disk, a new file is created.
		/// </summary>
		Append,

		/// <summary>
		/// Blindly skip downloading the file if it exists on disk, without any more checks.
		/// This is only included to be compatible with legacy behaviour.
		/// </summary>
		Skip
	}
}