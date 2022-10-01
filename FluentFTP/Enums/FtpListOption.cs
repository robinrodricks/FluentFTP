using System;

namespace FluentFTP {
	/// <summary>
	/// Flags that can control how a file listing is performed. If you are unsure what to use, set it to Auto.
	/// </summary>
	[Flags]
	public enum FtpListOption {
		/// <summary>
		/// Tries machine listings (MDTM command) if supported,
		/// and if not then falls back to OS-specific listings (LIST command)
		/// </summary>
		Auto = 0,

		/// <summary>
		/// Load the modify date using MDTM when it could not
		/// be parsed from the server listing. This only pertains
		/// to servers that do not implement the MLSD command.
		/// </summary>
		Modify = 1,

		/// <summary>
		/// Load the file size using the SIZE command when it
		/// could not be parsed from the server listing. This
		/// only pertains to servers that do not support the
		/// MLSD command.
		/// </summary>
		Size = 2,

		/// <summary>
		/// Combines the Modify and Size flags
		/// </summary>
		SizeModify = Modify | Size,

		/// <summary>
		/// Show hidden/dot files. This only pertains to servers
		/// that do not support the MLSD command. This option
		/// makes use the non standard -a parameter to LIST to
		/// tell the server to show hidden files. Since it's a
		/// non-standard option it may not always work. MLSD listings
		/// have no such option and whether or not a hidden file is
		/// shown is at the discretion of the server.
		/// </summary>
		AllFiles = 4,

		/// <summary>
		/// Force the use of OS-specific listings (LIST command) even if
		/// machine listings (MLSD command) are supported by the server
		/// </summary>
		ForceList = 8,

		/// <summary>
		/// Use the NLST command instead of LIST for a reliable file listing
		/// </summary>
		NameList = 16,

		/// <summary>
		/// Force the use of the NLST command (the slowest mode) even if machine listings
		/// and OS-specific listings are supported by the server
		/// </summary>
		ForceNameList = ForceList | NameList,

		/// <summary>
		/// Sets the ForceList flag and uses `LS' instead of `LIST' as the
		/// command for getting a directory listing. This option overrides
		/// ForceNameList and ignores the AllFiles flag.
		/// </summary>
		UseLS = 64 | ForceList,

		/// <summary>
		/// Gets files within subdirectories as well. Adds the -r option to the LIST command.
		/// Some servers may not support this feature.
		/// </summary>
		Recursive = 128,

		/// <summary>
		/// Do not retrieve path when no path is supplied to GetListing(),
		/// instead just execute LIST with no path argument.
		/// </summary>
		NoPath = 256,

		/// <summary>
		/// Include two extra items into the listing, for the current directory (".")
		/// and the parent directory (".."). Meaningless unless you want these two
		/// items for some reason.
		/// </summary>
		IncludeSelfAndParent = 512,

		/// <summary>
		/// Force the use of STAT command for getting file listings
		/// </summary>
		UseStat = 1024

	}
}