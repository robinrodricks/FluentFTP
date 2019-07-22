using System;

namespace FluentFTP {
	/// <summary>
	/// Defines if additional verification and actions upon failure that 
	/// should be performed when uploading/downloading files using the high-level APIs.  Ignored if the 
	/// FTP server does not support any hashing algorithms.
	/// </summary>
	public enum FtpDate {
		/// <summary>
		/// The date is whatever the server returns, with no conversion performed.
		/// </summary>
		Original = 0,

#if !CORE
		/// <summary>
		/// The date is converted to the local timezone, based on the TimeOffset property in FtpClient.
		/// </summary>
		Local = 1,

#endif
		/// <summary>
		/// The date is converted to UTC, based on the TimeOffset property in FtpClient.
		/// </summary>
		UTC = 2,
	}
}