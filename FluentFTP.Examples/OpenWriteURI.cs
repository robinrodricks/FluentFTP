using System;
using System.IO;
using FluentFTP;

namespace Examples {
	static class OpenWriteURI {
		public static Stream OpenNewWrite(Uri uri) {
			FtpClient cl = null;

			uri.ValidateFtpServer();

			cl = FtpClient.Connect(uri, true);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenWrite(uri.PathAndQuery, FtpDataType.Binary);
		}
		public static void OpenURI() {
			using (Stream s = OpenNewWrite(new Uri("ftp://server/path/file"))) {
				try {
					// write data to the file on the server
				} finally {
					s.Close();
				}
			}
		}
	}
}
