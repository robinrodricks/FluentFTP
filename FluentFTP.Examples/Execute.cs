using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class ExecuteExample {
		public static void Execute() {
			using (var conn = new FtpClient()) {
				FtpReply reply;

				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				if (!(reply = conn.Execute("SITE CHMOD 640 FOO.TXT")).Success) {
					throw new FtpCommandException(reply);
				}
			}
		}
	}
}