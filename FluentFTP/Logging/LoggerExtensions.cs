using System;

namespace FluentFTP.Logging {
	public static class LoggerExtensions {
		public static void Log(this IFluentLogger logger, FtpTraceLevel eventType, string message, Exception ex = null) =>
			logger.Log(new LogEntry(eventType, message, ex));
	}
}
