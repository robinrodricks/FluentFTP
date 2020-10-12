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

			// parse the raw timestamp without performing any timezone conversions
			if (DateTime.TryParseExact(date, FtpDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed)) {

				// if server time is wanted, don't perform any conversion
				if (m_timeConversion != FtpDate.ServerTime) {

					// convert server timezone to UTC based on the TimeZone property
					if (m_serverTimeZone != 0) {
						parsed = parsed - m_serverTimeOffset;
					}

					// convert UTC to local time if wanted (on .NET Core this is based on the LocalTimeZone property)
					if (m_timeConversion == FtpDate.LocalTime) {
#if CORE
						parsed = parsed + m_localTimeOffset;
#else
						parsed = System.TimeZone.CurrentTimeZone.ToLocalTime(parsed);
#endif
					}
				}
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

			// if server time is wanted, don't perform any conversion
			if (m_timeConversion != FtpDate.ServerTime) {

				// convert local to UTC if wanted (on .NET Core this is based on the LocalTimeZone property)
				if (m_timeConversion == FtpDate.LocalTime) {
#if CORE
					date = date - m_localTimeOffset;
#else
					date = System.TimeZone.CurrentTimeZone.ToUniversalTime(date);
#endif
				}

				// convert UTC to server timezone, based on the TimeZone property
				if (m_serverTimeZone != 0) {
					date = date + m_serverTimeOffset;
				}
			}

			// generate final pretty printed date
			var timeStr = date.ToString("yyyyMMddHHmmss");
			return timeStr;
		}

	}
}