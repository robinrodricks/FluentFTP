using System;
using System.IO;
using FluentFTP;

namespace Examples {
	public static class OpenReadURI {
		public static Stream OpenNewRead(Uri uri) {
			FtpClient cl = null;

			uri.ValidateFtpServer();

			cl = FtpClient.Connect(uri, true);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenRead(uri.PathAndQuery, FtpDataType.Binary);
		}

		public static void OpenURI() {
			using (var s = OpenNewRead(new Uri("ftp://server/path/file"))) {
				var buf = new byte[8192];
				var read = 0;

				try {
					while ((read = s.Read(buf, 0, buf.Length)) > 0) {
						Console.Write("\r{0}/{1} {2:p}     ",
							s.Position, s.Length,
							(double) s.Position / (double) s.Length);
					}
				}
				finally {
					Console.WriteLine();
					s.Close();
				}
			}
		}
	}
}