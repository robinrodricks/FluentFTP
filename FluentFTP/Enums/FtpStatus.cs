using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	/// <summary>
	/// The result of an upload or download operation
	/// </summary>
	public enum FtpStatus {

		/// <summary>
		/// The upload or download failed with an error transferring, or the source file did not exist
		/// </summary>
		Failed = 0,

		/// <summary>
		/// The upload or download completed successfully
		/// </summary>
		Success = 1,

		/// <summary>
		/// The upload or download was skipped because the file already existed on the target
		/// </summary>
		Skipped = 2

	}
}
