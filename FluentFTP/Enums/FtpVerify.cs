using System;

namespace FluentFTP {
	/// <summary>
	/// Defines if additional verification and actions upon failure that 
	/// should be performed when uploading/downloading files using the high-level APIs.  Ignored if the 
	/// FTP server does not support any hashing algorithms.
	/// </summary>
	[Flags]
	public enum FtpVerify {
		/// <summary>
		/// No verification of the file is performed
		/// </summary>
		None = 0,

		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then we retry the download/upload
		/// a specified amount of times before giving up. (See <see cref="FtpConfig.RetryAttempts"/>)
		/// </summary>
		Retry = 1,

		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then the failed file will be deleted.
		/// If combined with <see cref="FtpVerify.Retry"/>, then
		/// the deletion will occur if it fails upon the final retry.
		/// </summary>
		Delete = 2,

		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then an exception will be thrown.
		/// If combined with <see cref="FtpVerify.Retry"/>, then the throw will
		/// occur upon the failure of the final retry, and/or if combined with <see cref="FtpVerify.Delete"/>
		/// the method will throw after the deletion is processed.
		/// </summary>
		Throw = 4,

		/// <summary>
		/// OnlyChecksum is now renamed to OnlyVerify.
		/// </summary>
		[Obsolete("OnlyChecksum is now renamed to OnlyVerify to better reflect its behaviour.", true)]
		OnlyChecksum = 8,

		/// <summary>
		/// The file is only verified. If no verification type is specified, checksum is used.
		/// If the comparison fails, the method returns false and no further action is taken.
		/// </summary>
		OnlyVerify = 16,
	}
}