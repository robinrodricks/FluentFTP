using System;

namespace FluentFTP {
	/// <summary>
	/// Controls how timestamps returned by the server are converted.
	/// </summary>
	public enum FtpDate {
		/// <summary>
		/// The date is whatever the server returns, with no conversion performed.
		/// </summary>
		Original = 0,

#if !CORE
		/// <summary>
		/// Assumes that the server timestamps are in UTC, and converts the timestamps to the local time.
		/// When you modify the date of files, your local time is converted back to UTC and sent to the server.
		/// </summary>
		UTCToLocal = 1,

#endif
		/// <summary>
		/// Attempts to convert the server timestamps into UTC timestamps (Coordinated Universal Time).
		/// </summary>
		UTC = 2,

		/// <summary>
		/// Uses the time offset value specified in the FtpClient to shift dates from one timezone to another (TimeOffset property).
		/// </summary>
		TimeOffset = 3,

	}
}