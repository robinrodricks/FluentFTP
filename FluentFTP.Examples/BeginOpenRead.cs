using System;
using System.Net;
using FluentFTP;
using System.IO;
using System.Threading;

namespace Examples {
	public static class BeginOpenReadExample {
		private static ManualResetEvent m_reset = new ManualResetEvent(false);

		public static void BeginOpenRead() {
			// The using statement here is OK _only_ because m_reset.WaitOne()
			// causes the code to block until the async process finishes, otherwise
			// the connection object would be disposed early. In practice, you
			// typically would not wrap the following code with a using statement.
			using (var conn = new FtpClient()) {
				m_reset.Reset();

				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.BeginOpenRead("/path/to/file",
					new AsyncCallback(BeginOpenReadCallback), conn);

				m_reset.WaitOne();
				conn.Disconnect();
			}
		}

		private static void BeginOpenReadCallback(IAsyncResult ar) {
			var conn = ar.AsyncState as FtpClient;

			try {
				if (conn == null) {
					throw new InvalidOperationException("The FtpControlConnection object is null!");
				}

				using (var istream = conn.EndOpenRead(ar)) {
					var buf = new byte[8192];

					try {
						var start = DateTime.Now;

						while (istream.Read(buf, 0, buf.Length) > 0) {
							double perc = 0;

							if (istream.Length > 0) {
								perc = (double) istream.Position / (double) istream.Length;
							}

							Console.Write("\rTransferring: {0}/{1} {2}/s {3:p}         ",
								istream.Position.FormatBytes(),
								istream.Length.FormatBytes(),
								(istream.Position / DateTime.Now.Subtract(start).TotalSeconds).FormatBytes(),
								perc);
						}
					}
					finally {
						Console.WriteLine();
						istream.Close();
					}
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