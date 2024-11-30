using System;

namespace FluentFTP {
	/// <summary>
	/// Controls how timestamps returned by the server are converted.
	/// </summary>
	public enum FtpDate {

		/// <summary>
		/// Returns the server timestamps in Server Time. No timezone conversion is performed.
		/// </summary>
		ServerTime = 0,

		/// <summary>
		/// Returns the server timestamps in Local Time.
		/// Ensure that the `ServerTimeZone` property is set to the server's timezone.
		/// Ensure that the `ClientTimeZone` property is set to the client's timezone.
		/// </summary>
		LocalTime = 1,

		/// <summary>
		/// Returns the server timestamps in UTC (Coordinated Universal Time).
		/// Ensure that the `ServerTimeZone` property is correctly set to the server's timezone.
		/// </summary>
		UTC = 2,

	}
}