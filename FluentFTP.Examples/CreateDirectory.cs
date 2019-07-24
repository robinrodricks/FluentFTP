using System;
using System.Net;
using FluentFTP;
using System.IO;

namespace Examples {
	internal static class CreateDirectoryExample {
		public static void CreateDirectory() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.CreateDirectory("/test/path/that/should/be/created", true);
			}
		}
	}
}