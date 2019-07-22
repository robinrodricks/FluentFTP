using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class DirectoryExistsExample {
		public static void DeleteDirectory() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				if (conn.DirectoryExists("/full/or/relative/path")) {
					// do something
				}
			}
		}
	}
}