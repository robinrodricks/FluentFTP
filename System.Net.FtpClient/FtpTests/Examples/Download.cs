using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.FtpClient;

namespace FtpTests.Examples {
	public class Download {
		public static void DownloadFile() {
			using (FtpClient cl = new FtpClient("ftp", "ftp", "ftp.microsoft.com")) {
				string remote_file = "/Softlib/MSLFILES/SS3SP1-A.ZIP";
				long size = cl.GetFileSize(remote_file);

				using (FtpDataChannel chan = cl.OpenRead(remote_file)) {
					byte[] buf = new byte[4096];
					int read = 0;
					long total = 0;

					while ((read = chan.Read(buf, 0, buf.Length)) > 0) {
						total += read;

						// write the bytes to another stream...

						Console.Write("\rDownloading: {0}/{1} {2:p2}",
							total, size, ((double)total / (double)size));
					}

					Console.WriteLine();

					// when Dispose() is called on the chan object, the data channel
					// stream will automatically be closed
				}

				// when Dispose() is called on the cl object, a logout will
				// automatically be performed and the socket will be closed.
			}
		}
	}
}
