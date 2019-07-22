using System;

namespace FluentFTP {
	/// <summary>
	/// Defines how multi-file processes should handle a processing error.
	/// </summary>
	/// <remarks><see cref="FtpError.Stop"/> &amp; <see cref="FtpError.Throw"/> Cannot Be Combined</remarks>
	[Flags]
	public enum FtpError {
		/// <summary>
		/// No action is taken upon errors.  The method absorbs the error and continues.
		/// </summary>
		None = 0,

		/// <summary>
		/// If any files have completed successfully (or failed after a partial download/upload) then should be deleted.  
		/// This will simulate an all-or-nothing transaction downloading or uploading multiple files.  If this option is not
		/// combined with <see cref="FtpError.Stop"/> or <see cref="FtpError.Throw"/> then the method will
		/// continue to process all items whether if they are successful or not and then delete everything if a failure was
		/// encountered at any point.
		/// </summary>
		DeleteProcessed = 1,

		/// <summary>
		/// The method should stop processing any additional files and immediately return upon encountering an error.
		/// Cannot be combined with <see cref="FtpError.Throw"/>
		/// </summary>
		Stop = 2,

		/// <summary>
		/// The method should stop processing any additional files and immediately throw the current error.
		/// Cannot be combined with <see cref="FtpError.Stop"/>
		/// </summary>
		Throw = 4,
	}
}