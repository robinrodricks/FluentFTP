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

		public static string ToShortString(this TimeSpan span, string format = "0.###", string zeroString = "<1ms") {
			if (span.TotalDays > 1) {
				return span.TotalDays.ToString(format) + "d";
			}
			if (span.TotalHours > 1) {
				return span.TotalHours.ToString(format) + "h";
			}
			if (span.TotalMinutes > 1) {
				return span.TotalMinutes.ToString(format) + "m";
			}
			if (span.TotalSeconds > 1) {
				return span.TotalSeconds.ToString(format) + "s";
			}
			if (span.TotalMilliseconds > 1) {
				return ((int)span.TotalMilliseconds).ToString() + "ms";
			}
			return zeroString;
		}

	}
}
