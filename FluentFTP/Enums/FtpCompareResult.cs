using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {

	/// <summary>
	/// The result of a file comparison operation.
	/// </summary>
	public enum FtpCompareResult {

		/// <summary>
		/// Success. Local and remote files are exactly equal.
		/// </summary>
		Equal = 1,

		/// <summary>
		/// Failure. Local and remote files do not match.
		/// </summary>
		NotEqual = 2,

		/// <summary>
		/// Failure. Either the local or remote file does not exist.
		/// </summary>
		FileNotExisting = 3,

		/// <summary>
		/// Failure. Checksum verification is enabled and your server does not support any hash algorithm.
		/// </summary>
		ChecksumNotSupported = 4,

	}
}
