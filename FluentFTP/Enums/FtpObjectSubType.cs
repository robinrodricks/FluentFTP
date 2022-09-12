using System;

namespace FluentFTP {
	/// <summary>
	/// Type of file system of object
	/// </summary>
	public enum FtpObjectSubType {

		/// <summary>
		/// The default subtype.
		/// </summary>
		Unknown,

		/// <summary>
		/// A sub directory within the listed directory.
		/// (Only set when machine listing is available and type is 'dir')
		/// </summary>
		SubDirectory,

		/// <summary>
		/// The self directory.
		/// (Only set when machine listing is available and type is 'cdir')
		/// </summary>
		SelfDirectory,

		/// <summary>
		/// The parent directory.
		/// (Only set when machine listing is available and type is 'pdir')
		/// </summary>
		ParentDirectory,

	}
}