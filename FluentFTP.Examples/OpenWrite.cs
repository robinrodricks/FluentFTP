using System;
using System.IO;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class OpenWriteExample {
		public static void OpenWrite() {
			using (var conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				using (var ostream = conn.OpenWrite("/full/or/relative/path/to/file")) {
					try {
						// istream.Position is incremented accordingly to the writes you perform
					}
					finally {
						ostream.Close();
					}
				}
			}
		}
	}
}