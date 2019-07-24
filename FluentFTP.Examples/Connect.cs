using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class ConnectExample {
		public static void Connect() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.Connect();
			}
		}
	}
}