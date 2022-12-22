using Microsoft.Extensions.Logging;

namespace FluentFTP.Logging {
	public sealed class MicrosoftLoggingAdapter : IFluentLogger {
		private readonly ILogger adaptee;

		public MicrosoftLoggingAdapter(ILogger adaptee) =>
			this.adaptee = adaptee;

		public void Log(LogEntry entry) =>
			adaptee.Log(ToLevel(entry.Severity), 0, entry.Message, entry.Exception, (s, _) => s);

		private static LogLevel ToLevel(FtpTraceLevel s) => s switch {
			FtpTraceLevel.Verbose => LogLevel.Debug,
			FtpTraceLevel.Info => LogLevel.Information,
			FtpTraceLevel.Warn => LogLevel.Warning,
			FtpTraceLevel.Error => LogLevel.Error,
			_ => LogLevel.Information
		};
	}
}