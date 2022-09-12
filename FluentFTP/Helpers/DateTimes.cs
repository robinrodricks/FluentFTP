using System;
using System.Globalization;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class DateTimes {

		/// <summary>
		/// Converts the FTP date string into a DateTime object, without performing any timezone conversion.
		/// </summary>
		/// <param name="dateString">The date string</param>
		/// <param name="formats">Date formats to try parsing the value from (eg "yyyyMMddHHmmss")</param>
		/// <returns>A <see cref="DateTime"/> object representing the date, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public static DateTime ParseFtpDate(this string dateString, FtpClient client, string[] formats = null) {
			if (formats == null) {
				formats = FtpDateFormats;
			}

			// parse the raw timestamp without performing any timezone conversions
			try {
				DateTime date = DateTime.ParseExact(dateString, FtpDateFormats, client.ListingCulture.DateTimeFormat, DateTimeStyles.None); // or client.ListingCulture.DateTimeFormat

				return date;
			}
			catch (FormatException) {
				client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + dateString + "'");
			}

			return DateTime.MinValue;
		}

		/// <summary>
		/// Generates an FTP date-string from the DateTime object, without performing any timezone conversion.
		/// </summary>
		/// <param name="date">The date value</param>
		/// <returns>A string representing the date</returns>
		public static string GenerateFtpDate(this DateTime date) {

			// generate final pretty printed date
			var timeStr = date.ToString("yyyyMMddHHmmss");
			return timeStr;
		}

		private static string[] FtpDateFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmss'.'f", "yyyyMMddHHmmss'.'ff", "yyyyMMddHHmmss'.'fff", "MMM dd  yyyy", "MMM  d  yyyy", "MMM dd HH:mm", "MMM  d HH:mm" };

	}
}