using System;
using System.Net;
using FluentFTP;
using System.Threading;

namespace Examples {
	public static class BeginGetNameListingExample {
		private static ManualResetEvent m_reset = new ManualResetEvent(false);

		public static void BeginGetNameListing() {
			// The using statement here is OK _only_ because m_reset.WaitOne()
			// causes the code to block until the async process finishes, otherwise
			// the connection object would be disposed early. In practice, you
			// typically would not wrap the following code with a using statement.
			using (var conn = new FtpClient()) {
				m_reset.Reset();

				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.BeginGetNameListing(new AsyncCallback(EndGetNameListing), conn);

				m_reset.WaitOne();
				conn.Disconnect();
			}
		}

		private static void EndGetNameListing(IAsyncResult ar) {
			var conn = ar.AsyncState as FtpClient;

			try {
				if (conn == null) {
					throw new InvalidOperationException("The FtpControlConnection object is null!");
				}

				foreach (var s in conn.EndGetNameListing(ar)) {
					// load some information about the object
					// returned from the listing...
					var isDirectory = conn.DirectoryExists(s);
					var modify = conn.GetModifiedTime(s);
					var size = isDirectory ? 0 : conn.GetFileSize(s);
				}
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
			finally {
				m_reset.Set();
			}
		}
	}
}