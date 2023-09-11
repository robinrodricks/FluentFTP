using System;

namespace FluentFTP {
	/// <summary>
	/// Directory navigation mode that control how server-side directory traversal is performed.
	/// Manual mode is the legacy version which allows users full control of the working directory.
	/// All the other modes are smarter automatic versions where FluentFTP will take control of the working directory.
	/// </summary>
	[Flags]
	public enum FtpNavigate:uint {

		/// <summary>
		/// The legacy navigation mode.
		/// No automatic directory navigation performed by FluentFTP.
		/// Users can `SetWorkingDirectory` (CWD).
		/// Paths provided to FTP API can be absolute or relative to the working directory.
		/// </summary>
		Manual = 0,

		/// <summary>
		/// Fully automatic directory traversal on the server-side.
		/// FluentFTP will automatically change the working directory based on the file path provided to the API method.
		/// We assume that all file paths provided will be absolute paths.
		/// Users CANNOT `SetWorkingDirectory` (CWD).
		/// Fast mode.
		/// </summary>
		Auto = 1,

		/// <summary>
		/// Fully automatic directory traversal on the server-side.
		/// FluentFTP will automatically change the working directory based on the file path provided to the API method.
		/// We assume that all file paths provided will be absolute paths.
		/// Users CAN `SetWorkingDirectory` (CWD), which will override the path set internally.
		/// This mode is slower than `Auto` because we need to restore the directory to the one the user last set after each API call.
		/// </summary>
		SemiAuto = 2,

		/// <summary>
		/// Adds a flag to the enum that allows for automatic directory traversal ONLY if the path contains spaces.
		/// Only works with `Auto` or `SemiAuto` modes.
		/// </summary>
		Conditional = 255,

	}
}