using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP time span values
	/// </summary>
	public static class TimeSpans {

		public static string ToShortString(this TimeSpan span, string format = "0.###", string zeroString = "0ms") {
			if (span.TotalDays > 0) {
				return span.TotalDays.ToString(format) + "h";
			}
			if (span.TotalHours > 0) {
				return span.TotalHours.ToString(format) + "h";
			}
			if (span.TotalMinutes > 0) {
				return span.TotalMinutes.ToString(format) + "m";
			}
			if (span.TotalSeconds > 0) {
				return span.TotalSeconds.ToString(format) + "s";
			}
			if (span.TotalMilliseconds > 0) {
				return span.TotalMilliseconds.ToString(format) + "ms";
			}
			return zeroString;
		}

	}
}
