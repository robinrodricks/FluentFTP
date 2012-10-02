using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    class BeginDeleteDirectoryExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginDeleteDirectory() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.CreateDirectory("/some/test/directory");
                conn.BeginDeleteDirectory("/some", true, new AsyncCallback(DeleteDirectoryCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void DeleteDirectoryCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndDeleteDirectory(ar);
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
