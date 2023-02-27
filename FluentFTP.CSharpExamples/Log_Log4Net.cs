using System;
using System.IO;
using System.Net;
using System.Reflection;
using FluentFTP;
using FluentFTP.Logging;
using log4net.Repository;
using log4net;
using Microsoft.Extensions.Logging;

namespace Examples {
	internal static class Log4NetExample {
		public static void Configure() {

			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");


				// create Log4Net logger
				var microsoftLogger = new Log4NetLogger("FTP", "config.xml", true);

				// wrap with FtpLogAdapter
				conn.Logger = new FtpLogAdapter(microsoftLogger);


				conn.Connect();
			}
		}
	}
	public class Log4NetLogger : ILogger {
		private readonly string _name;

		private readonly ILog _log;

		private readonly bool _skipDiagnosticLogs;

		private ILoggerRepository _loggerRepository;

		public Log4NetLogger(string name, string filePath, bool skipDiagnosticLogs) {
			_name = name;
			_loggerRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			_log = LogManager.GetLogger(_loggerRepository.Name, name);
			_skipDiagnosticLogs = skipDiagnosticLogs;

			log4net.Config.XmlConfigurator.Configure(_loggerRepository, new FileInfo(filePath));
		}

		public IDisposable BeginScope<TState>(TState state) {
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) {
			switch (logLevel) {
				case LogLevel.Critical:
					return _log.IsFatalEnabled;
				case LogLevel.Debug:
				case LogLevel.Trace:
					return _log.IsDebugEnabled && AllowDiagnostics();
				case LogLevel.Error:
					return _log.IsErrorEnabled;
				case LogLevel.Information:
					return _log.IsInfoEnabled && AllowDiagnostics();
				case LogLevel.Warning:
					return _log.IsWarnEnabled;
				default:
					throw new ArgumentOutOfRangeException(nameof(logLevel));
			}
		}

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception exception,
			Func<TState, Exception, string> formatter) {
			if (!IsEnabled(logLevel)) {
				return;
			}

			if (formatter == null) {
				throw new ArgumentNullException(nameof(formatter));
			}

			string message = $"{formatter(state, exception)} {exception}";

			if (!string.IsNullOrEmpty(message) || exception != null) {
				switch (logLevel) {
					case LogLevel.Critical:
						_log.Fatal(message);
						break;
					case LogLevel.Debug:
					case LogLevel.Trace:
						_log.Debug(message);
						break;
					case LogLevel.Error:
						_log.Error(message);
						break;
					case LogLevel.Information:
						_log.Info(message);
						break;
					case LogLevel.Warning:
						_log.Warn(message);
						break;
					default:
						_log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
						_log.Info(message, exception);
						break;
				}
			}
		}

		private bool AllowDiagnostics() {
			if (!_skipDiagnosticLogs) {
				return true;
			}

			return !(_name.ToLower().StartsWith("microsoft")
				|| _name == "IdentityServer4.AccessTokenValidation.Infrastructure.NopAuthenticationMiddleware");
		}
	}
}
