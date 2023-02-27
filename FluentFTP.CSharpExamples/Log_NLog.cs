using System;
using System.Net;
using FluentFTP;
using FluentFTP.Logging;
using NLog.Extensions.Logging;

namespace Examples {
	internal static class NLogExample {

		public static void Configure() {

			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");


				// create NLog logger
				var nlogLogger = new NLogLoggerProvider();

				// wrap with MELA ILogger
				var microsoftLogger = nlogLogger.CreateLogger(typeof(Log4NetExample).FullName);

				// wrap with FtpLogAdapter
				conn.Logger = new FtpLogAdapter(microsoftLogger);


				conn.Connect();
			}
		}

	}
}