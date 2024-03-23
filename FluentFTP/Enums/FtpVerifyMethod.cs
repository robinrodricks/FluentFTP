using System;

namespace FluentFTP {
	/// <summary>
	/// Defines which verification types should be performed when 
	/// uploading/downloading files using the high-level APIs.
	/// Multiple verification types can be combined.
	/// </summary>
	[Flags]
	public enum FtpVerifyMethod {
		/// <summary>
		/// Compares the file size.
		/// Both file sizes should exactly match for the file to be considered equal.
		/// </summary>
		Size = 1,

		/// <summary>
		/// Compares the date modified of the file.
		/// Both dates should exactly match for the file to be considered equal.
		/// </summary>
		Date = 2,

		/// <summary>
		/// Compares the checksum or hash of the file using the first supported hash algorithm.
		/// Both checksums should exactly match for the file to be considered equal.
		/// </summary>
		Checksum = 4,
	}
}