using System;
using System.IO;
using FluentFTP;

namespace Examples {
	internal class OpenAppendURI {
		public static Stream OpenNewAppend(Uri uri) {
			FtpClient cl = null;

			uri.ValidateFtpServer();

			cl = FtpClient.Connect(uri, true);
			cl.EnableThreadSafeDataConnections = false;

			return cl.OpenAppend(uri.PathAndQuery, FtpDataType.Binary);
		}

		public static void OpenURI() {
			using (var s = OpenNewAppend(new Uri("ftp://server/path/file"))) {
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