using System;

namespace FluentFTP {
	/// <summary>
	/// Different types of hashing algorithms for computing checksums.
	/// </summary>
	[Flags]
	public enum FtpHashAlgorithm : int {

		/// <summary>
		/// Automatic algorithm, or hashing not supported.
		/// </summary>
		NONE = 0,

		/// <summary>
		/// SHA-1 algorithm
		/// </summary>
		SHA1 = 1,

		/// <summary>
		/// SHA-256 algorithm
		/// </summary>
		SHA256 = 2,

		/// <summary>
		/// SHA-512 algorithm
		/// </summary>
		SHA512 = 4,

		/// <summary>
		/// MD5 algorithm
		/// </summary>
		MD5 = 8,

		/// <summary>
		/// CRC algorithm
		/// </summary>
		CRC = 16
	}
}