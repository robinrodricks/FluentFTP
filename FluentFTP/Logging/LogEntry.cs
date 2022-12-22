using System;

namespace FluentFTP.Logging {
	public readonly struct LogEntry {
		public FtpTraceLevel Severity { get; }
		public string Message { get; }
		public Exception Exception { get; }

		public LogEntry(FtpTraceLevel severity, string msg, Exception ex = null) {
			Severity = severity;
			Message = msg;
			Exception = ex;
		}
	}
}
