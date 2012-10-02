using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginRenameExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginRename() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginRename("/source/object", "/new/path/and/name",
                    new AsyncCallback(BeginRenameCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void BeginRenameCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndRename(ar);
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
