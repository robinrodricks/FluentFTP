using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class GetWorkingDirectoryExample {
		public static void GetWorkingDirectory() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				Console.WriteLine("The working directory is: " +
				                  conn.GetWorkingDirectory());
			}
		}
	}
}