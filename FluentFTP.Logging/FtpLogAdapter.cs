using FluentFTP;
using Microsoft.Extensions.Logging;

namespace FluentFTP.Logging {
	/// <summary>
	/// Logging adapter to help FluentFTP integrate with MELA-compatible Loggers (NLog, Serilog, Log4Net, PLogger, etc).
	/// Read the Logging page: https://github.com/robinrodricks/FluentFTP/wiki/Logging
	/// </summary>
	public sealed class FtpLogAdapter : IFtpLogger {
		private readonly ILogger adaptee;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public FtpLogAdapter(ILogger adaptee) =>
			this.adaptee = adaptee;

		public void Log(FtpLogEntry entry) =>
			adaptee.Log(ToLevel(entry.Severity), 0, entry.Message, entry.Exception, (s, _) => s);

		private static LogLevel ToLevel(FtpTraceLevel s) => s switch {
			FtpTraceLevel.Verbose => LogLevel.Debug,
			FtpTraceLevel.Info => LogLevel.Information,
			FtpTraceLevel.Warn => LogLevel.Warning,
			FtpTraceLevel.Error => LogLevel.Error,
			_ => LogLevel.Information
		};
	}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

}