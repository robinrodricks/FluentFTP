using System;
using System.Net;
using System.Threading;
using FluentFTP;

namespace Examples {
	internal static class BeginDisconnectExample {
		private static ManualResetEvent m_reset = new ManualResetEvent(false);

		public static void BeginDisconnect() {
			// The using statement here is OK _only_ because m_reset.WaitOne()
			// causes the code to block until the async process finishes, otherwise
			// the connection object would be disposed early. In practice, you
			// typically would not wrap the following code with a using statement.
			using (var conn = new FtpClient()) {
				m_reset.Reset();

				conn.Host = "localhost";
				conn.Credentials = new NetworkCredential("ftptest", "ftptest");
				conn.Connect();
				conn.BeginDisconnect(BeginDisconnectCallback, conn);

				m_reset.WaitOne();
			}
		}

		private static void BeginDisconnectCallback(IAsyncResult ar) {
			var conn = ar.AsyncState as FtpClient;

			try {
				if (conn == null) {
					throw new InvalidOperationException("The FtpControlConnection object is null!");
				}

				conn.EndDisconnect(ar);
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