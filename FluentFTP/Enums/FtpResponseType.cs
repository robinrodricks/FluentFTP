using System;

namespace FluentFTP {
	/// <summary>
	/// The type of response the server responded with
	/// </summary>
	public enum FtpResponseType : int {
		/// <summary>
		/// No response
		/// </summary>
		None = 0,

		/// <summary>
		/// Success
		/// </summary>
		PositivePreliminary = 1,

		/// <summary>
		/// Success
		/// </summary>
		PositiveCompletion = 2,

		/// <summary>
		/// Success
		/// </summary>
		PositiveIntermediate = 3,

		/// <summary>
		/// Temporary failure
		/// </summary>
		TransientNegativeCompletion = 4,

		/// <summary>
		/// Permanent failure
		/// </summary>
		PermanentNegativeCompletion = 5
	}
}