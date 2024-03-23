using System;
using System.Globalization;
using System.Text;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP date time values
	/// </summary>
	public static class DateTimes {

		/// <summary>
		/// Converts the FTP date string into a DateTime object, without performing any timezone conversion.
		/// </summary>
		/// <param name="dateString">The date string</param>
		/// <param name="client">The client object this is done for</param>
		/// <param name="formats">Date formats to try parsing the value from (eg "yyyyMMddHHmmss")</param>
		/// <param name="styles">The <see cref="DateTimeStyles"/> used when parsing</param>
		/// <returns>A <see cref="DateTime"/> object representing the date, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public static DateTime ParseFtpDate(this string dateString, BaseFtpClient client, string[] formats = null, DateTimeStyles styles = DateTimeStyles.None) {
			if (formats == null) {
				formats = FtpDateFormats;
			}

			// parse the raw timestamp without performing any timezone conversions
			try {
				DateTime date = DateTime.ParseExact(dateString, FtpDateFormats, client.Config.ListingCulture.DateTimeFormat, styles); // or client.ListingCulture.DateTimeFormat

				return date;
			}
			catch (FormatException) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + dateString + "'");
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


		/// <summary>
		/// Generates C# code to create this date.
		/// </summary>
		public static string ToCode(this DateTime date) {
			var sb = new StringBuilder();
			sb.Append("new DateTime(");
			sb.Append(date.Year);
			sb.Append(',');
			sb.Append(date.Month);
			sb.Append(',');
			sb.Append(date.Day);
			sb.Append(',');
			sb.Append(date.Hour);
			sb.Append(',');
			sb.Append(date.Minute);
			sb.Append(',');
			sb.Append(date.Second);
			sb.Append(',');
			sb.Append(date.Millisecond);
			sb.Append(")");
			return sb.ToString();
		}

		private static string[] FtpDateFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmss'.'f", "yyyyMMddHHmmss'.'ff", "yyyyMMddHHmmss'.'fff", "MMM dd  yyyy", "MMM  d  yyyy", "MMM dd HH:mm", "MMM  d HH:mm" };

	}
}