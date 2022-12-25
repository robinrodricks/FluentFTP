using System;
using FluentFTP.Helpers.Logging;

namespace FluentFTP.Helpers.Logging {
	internal static class LoggerExtensions {

		/// <summary>
		/// Log a message to the given IFluentLogger class.
		/// </summary>
		public static void Log(this IFtpLogger logger, FtpTraceLevel eventType, string message, Exception ex = null) {
			logger.Log(new FtpLogEntry(eventType, message, ex));
		}

		/// <summary>
		/// Get the log prefix for the given trace level type.
		/// </summary>
		public static string GetLogPrefix(this FtpTraceLevel eventType) {
			switch (eventType) {
				case FtpTraceLevel.Verbose:
					return "Status:   ";

				case FtpTraceLevel.Info:
					return "Status:   ";

				case FtpTraceLevel.Warn:
					return "Warning:  ";

				case FtpTraceLevel.Error:
					return "Error:    ";
			}

			return "Status:   ";
		}

	}
}
