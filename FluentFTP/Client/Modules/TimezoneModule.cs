using System;

namespace FluentFTP.Client.Modules {
	internal static class TimezoneModule {

		/// <summary>
		/// If `reverse` is false, converts the date provided by the FTP server into the timezone required locally.
		/// If `reverse` is true, converts the local timezone date into the date required by the FTP server.
		///
		/// Affected by properties: TimeConversion, ServerTimeZone, ClientTimeZone.
		/// </summary>
		public static DateTime ConvertDate(DateTime date, FtpConfig config, bool reverse) {

			// if server time is wanted, don't perform any conversion
			if (config.TimeConversion == FtpDate.ServerTime) {
				return date;
			}

			if (!reverse) {

				// Server to client conversion
				if (config.TimeConversion == FtpDate.LocalTime) {
					date = TimeZoneInfo.ConvertTime(date, config.ServerTimeZone, config.ClientTimeZone);
				}
				else {

					// Server to UTC conversion
					date = TimeZoneInfo.ConvertTime(date, config.ServerTimeZone, TimeZoneInfo.Utc);
				}
			}
			else {

				// Client to server conversion
				if (config.TimeConversion == FtpDate.LocalTime) {
					date = TimeZoneInfo.ConvertTime(date, config.ClientTimeZone, config.ServerTimeZone);
				}
				else {

					// UTC to server conversion
					date = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Utc, config.ServerTimeZone);
				}
			}

			return date;
		}

	}
}
