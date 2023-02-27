using System;
using System.Net;
using FluentFTP;
using FluentFTP.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Examples {
	internal static class SerilogExample {

		public static void Configure() {

			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");


				// create Serilog logger
				var serilogLogger = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.WriteTo.File("logs/FluentFTPLogs.txt", rollingInterval: RollingInterval.Day)
					.CreateLogger();

				// wrap with MELA ILogger
				var microsoftLogger = new SerilogLoggerFactory(serilogLogger)
					.CreateLogger("FTP");

				// wrap with FtpLogAdapter
				conn.Logger = new FtpLogAdapter(microsoftLogger);


				conn.Connect();
			}
		}

	}
}