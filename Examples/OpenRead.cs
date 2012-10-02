using System;
using System.IO;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
	public class OpenReadExample {
		public static void OpenRead() {
			using(FtpClient conn = new FtpClient()) {
				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");

				using(Stream istream = conn.OpenRead("/full/or/relative/path/to/file")) {
					try {
						// istream.Position is incremented accordingly to the reads you perform
                        // istream.Length == file size if the server supports getting the file size
                        // also note that file size for the same file can vary between ASCII and Binary
                        // modes and some servers won't even give a file size for ASCII files! It is
                        // recommended that you stick with Binary and worry about character encodings
                        // on your end of the connection.
					}
					finally {
						Console.WriteLine();
						istream.Close();
					}
				}
			}
		}
	}
}

