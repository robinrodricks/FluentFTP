using System;

namespace FluentFTP {
	/// <summary>
	/// Different types of hashing algorithms for computing checksums.
	/// </summary>
	[Flags]
	public enum FtpHashAlgorithm : int {
		/// <summary>
		/// HASH command is not supported
		/// </summary>
		NONE = 0,

		/// <summary>
		/// SHA-1
		/// </summary>
		SHA1 = 1,

		/// <summary>
		/// SHA-256
		/// </summary>
		SHA256 = 2,

		/// <summary>
		/// SHA-512
		/// </summary>
		SHA512 = 4,

		/// <summary>
		/// MD5
		/// </summary>
		MD5 = 8,

		/// <summary>
		/// CRC
		/// </summary>
		CRC = 16
	}
}