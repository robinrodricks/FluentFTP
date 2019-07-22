using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class FileExistsExample {
		public static void FileExists() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				// The last parameter forces FluentFTP to use LIST -a 
				// for getting a list of objects in the parent directory.
				if (conn.FileExists("/full/or/relative/path")) {
					// dome something
				}
			}
		}
	}
}