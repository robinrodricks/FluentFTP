using System;

namespace FluentFTP {
	/// <summary>
	/// Defines if additional verification and actions upon failure that 
	/// should be performed when uploading/downloading files using the high-level APIs.  Ignored if the 
	/// FTP server does not support any hashing algorithms.
	/// </summary>
	[Flags]
	public enum FtpVerify :ushort {
		/// <summary>
		/// No verification of the file is performed
		/// </summary>
		None = 0,

		// Section Options
		// 0x0001 -> 0x0080

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

		// Section Methods
		// 0x0100 -> 0x8000

		/// <summary>
		/// Compares the file size.
		/// Both file sizes should exactly match for the file to be considered equal.
		/// </summary>
		Size = 256,

		/// <summary>
		/// Compares the date modified of the file.
		/// Both dates should exactly match for the file to be considered equal.
		/// </summary>
		Date = 1024,

		/// <summary>
		/// Compares the checksum or hash of the file using the first supported hash algorithm.
		/// Both checksums should exactly match for the file to be considered equal.
		/// </summary>
		Checksum = 2048,

		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then the method returns false and no other action is taken.
		/// </summary>
		OnlyChecksum = 4096,
	}
}