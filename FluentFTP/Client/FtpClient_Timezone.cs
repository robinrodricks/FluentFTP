using System;
using System.Globalization;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		
		private static string[] FtpDateFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmss'.'f", "yyyyMMddHHmmss'.'ff", "yyyyMMddHHmmss'.'fff", "MMM dd  yyyy", "MMM  d  yyyy", "MMM dd HH:mm", "MMM  d HH:mm" };

		/// <summary>
		/// Tries to convert the string FTP date representation into a <see cref="DateTime"/> object
		/// </summary>
		/// <param name="date">The date string</param>
		/// <returns>A <see cref="DateTime"/> object representing the date, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public DateTime ParseFtpDate(string date) {
			DateTime parsed;

			// adjust the timestamp into UTC if wanted
			DateTimeStyles options = m_timeConversion == FtpDate.UTC ?
				(DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces) :
				(DateTimeStyles.AllowWhiteSpaces);

			// parse the raw timestamp without performing any timezone conversions
			if (DateTime.TryParseExact(date, FtpDateFormats, CultureInfo.InvariantCulture, options, out parsed)) {

				// convert server timezone to UTC, based on the TimeOffset property
				if (m_timeConversion == FtpDate.TimeOffset) {
					parsed = parsed - m_timeOffset;
				}

				// convert to local time if wanted
#if !CORE
				if (m_timeConversion == FtpDate.UTCToLocal) {
					parsed = TimeZone.CurrentTimeZone.ToLocalTime(parsed);
				}

#endif
				// return the final parsed date value
				return parsed;
			}


			return DateTime.MinValue;
		}

		/// <summary>
		/// Generates an FTP date-string when provided a date value
		/// </summary>
		/// <param name="date">The date value</param>
		/// <returns>A <see cref="DateTime"/> object representing the date, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public string GenerateFtpDate(DateTime date) {

			// convert local to UTC if wanted
#if !CORE
			if (m_timeConversion == FtpDate.UTCToLocal) {
				date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
			}
#endif

			// convert UTC to server timezone, based on the TimeOffset property
			if (m_timeConversion == FtpDate.TimeOffset) {
				date = date + m_timeOffset;
			}

			// convert UTC to server timezone if no offset specified
			if (m_timeConversion == FtpDate.UTC) {
				throw new ArgumentException("Changing the modified date of a file is not supported when TimeConversion is in UTC mode.", "FtpClient.TimeConversion");
			}

			// generate final pretty printed date
			var timeStr = date.ToString("yyyyMMddHHmmss");
			return timeStr;
		}

	}
}