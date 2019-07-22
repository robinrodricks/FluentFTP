using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class DeleteFileExample {
		public static void DeleteFile() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.DeleteFile("/full/or/relative/path/to/file");
			}
		}
	}
}