using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	/// <summary>
	/// Determines how we handle downloading and uploading folders
	/// </summary>
	public enum FtpFolderSyncMode {

		/// <summary>
		/// Dangerous but useful method!
		/// Uploads/downloads all the missing files to update the server/local filesystem.
		/// Deletes the extra files to ensure that the target is an exact mirror of the source.
		/// </summary>
		Mirror,

		/// <summary>
		/// Safe method!
		/// Uploads/downloads all the missing files to update the server/local filesystem.
		/// </summary>
		Update,

	}
}
