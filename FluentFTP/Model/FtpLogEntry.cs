using System;

namespace FluentFTP {
	/// <summary>
	/// Metadata of a single log message.
	/// </summary>
	public readonly struct FtpLogEntry {
		public FtpTraceLevel Severity { get; }
		public string Message { get; }
		public Exception Exception { get; }

		public FtpLogEntry(FtpTraceLevel severity, string msg, Exception ex = null) {
			Severity = severity;
			Message = msg;
			Exception = ex;
		}
	}
}
