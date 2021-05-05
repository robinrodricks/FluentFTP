using System;
using System.Globalization;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		
		/// <summary>
		/// If reverse is false, converts the date provided by the FTP server into the timezone required locally.
		/// If reverse is true, converts the local timezone date into the date required by the FTP server.
		/// 
		/// Affected by properties: TimeConversion, TimeZone, LocalTimeZone.
		/// </summary>
		public DateTime ConvertDate(DateTime date, bool reverse = false) {

			// if server time is wanted, don't perform any conversion
			if (m_timeConversion != FtpDate.ServerTime) {

				// convert server time to local time
				if (!reverse) {

					// convert server timezone to UTC based on the TimeZone property
					if (m_serverTimeZone != 0) {
						date = date - m_serverTimeOffset;
					}

					// convert UTC to local time if wanted (on .NET Core this is based on the LocalTimeZone property)
					if (m_timeConversion == FtpDate.LocalTime) {
#if CORE
					date = date + m_localTimeOffset;
#else
						date = System.TimeZone.CurrentTimeZone.ToLocalTime(date);
#endif
					}

				}

				// convert local time to server time
				else {

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
			}

			// return the final date value
			return date;
		}


	}
}