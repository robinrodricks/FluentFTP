using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	/// <summary>
	/// Flags that control how file comparison is performed. If you are unsure what to use, set it to Auto.
	/// </summary>
	[Flags]
	public enum FtpCompareOption {

		/// <summary>
		/// Compares the file size and the checksum of the file (using the first supported hash algorithm).
		/// The local and remote file sizes and checksums should exactly match for the file to be considered equal.
		/// </summary>
		Auto = 0,

		/// <summary>
		/// Compares the file size.
		/// Both file sizes should exactly match for the file to be considered equal.
		/// </summary>
		Size = 1,

		/// <summary>
		/// Compares the date modified of the file.
		/// Both dates should exactly match for the file to be considered equal.
		/// </summary>
		DateModified = 2,

		/// <summary>
		/// Compares the checksum or hash of the file using the first supported hash algorithm.
		/// Both checksums should exactly match for the file to be considered equal.
		/// </summary>
		Checksum = 4,

	}
}
