using System;

namespace FluentFTP {
	/// <summary>
	/// Directory navigation mode that control how server-side directory traversal is performed.
	/// Manual mode is the legacy version which allows users full control of the working directory.
	/// All the other modes are smarter automatic versions where FluentFTP will take control of the working directory
	/// when executing FTP subcommands that accept a pathname[+filename] combination.
	/// </summary>
	[Flags]
	public enum FtpNavigate:uint {

		/// <summary>
		/// The legacy navigation mode.
		/// No automatic directory navigation performed by FluentFTP.
		/// Users can `SetWorkingDirectory` (CWD).
		/// Paths provided to FTP API can be absolute or relative to the current working directory.
		/// </summary>
		Manual = 0,

		/// <summary>
		/// Fully automatic directory traversal on the server-side.
		/// Users can `SetWorkingDirectory` (CWD), which will only temporarily override the CWD that will be set internally.
		/// Paths provided to FTP API can be absolute or relative to the current working directory.
		/// FluentFTP will automatically change the working directory based on the file path provided to the API method.
		/// Fast mode.
		/// </summary>
		Auto = 1,

		/// <summary>
		/// Fully automatic directory traversal on the server-side.
		/// FluentFTP will automatically change the working directory based on the file path provided to the API method.
		/// Users can `SetWorkingDirectory` (CWD), which will only temporarily override the CWD that will be set internally.
		/// Paths provided to FTP API can be absolute or relative to the current working directory.
		/// This mode is slower than `Auto` because we will restore the directory (if needed) to the one the one that was active
		/// prior to the API call.
		/// </summary>
		SemiAuto = 2,

		/// <summary>
		/// Adds a flag to the enum that allows automatic directory traversal ONLY if the path contains spaces.
		/// Only works with `Auto` or `SemiAuto` modes.
		/// </summary>
		Conditional = 255,

	}
}