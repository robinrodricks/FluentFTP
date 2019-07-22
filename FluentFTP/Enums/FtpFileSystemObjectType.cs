using System;

namespace FluentFTP {
	/// <summary>
	/// Type of file system of object
	/// </summary>
	public enum FtpFileSystemObjectType {
		/// <summary>
		/// A file
		/// </summary>
		File,

		/// <summary>
		/// A directory
		/// </summary>
		Directory,

		/// <summary>
		/// A symbolic link
		/// </summary>
		Link
	}
}