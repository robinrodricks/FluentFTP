using System;

namespace FluentFTP {
	/// <summary>
	/// Defines which verification types should be performed when 
	/// uploading/downloading files using the high-level APIs.
	/// Multiple verification types can be combined.
	/// </summary>
	[Flags]
	public enum FtpVerifyMethod {

		// Human Mnemonics

		/// <summary>
		/// For servers that support checksum, only checksum check, else only size check
		/// </summary>
		SizeOnly = Size,

		/// <summary>
		/// For servers that support checksum, only checksum check, else only size check
		/// </summary>
		SizeOrChecksum = Checksum,

		/// <summary>
		/// Checks size first, then checks checksum if the server supports it
		/// </summary>
		SizeThenChecksum = Size | Checksum,

		// Actual Functional Flags

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