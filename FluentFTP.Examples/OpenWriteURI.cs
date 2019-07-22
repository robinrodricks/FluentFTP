using System;
using System.IO;
using FluentFTP;

namespace Examples {
	internal static class OpenWriteURI {
		public static Stream OpenNewWrite(Uri uri) {
			FtpClient cl = null;

			uri.ValidateFtpServer();

			cl = FtpClient.Connect(uri, true);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenWrite(uri.PathAndQuery, FtpDataType.Binary);
		}

		public static void OpenURI() {
			using (var s = OpenNewWrite(new Uri("ftp://server/path/file"))) {
				try {
					// write data to the file on the server
				}
				finally {
					s.Close();
				}
			}
		}
	}
}