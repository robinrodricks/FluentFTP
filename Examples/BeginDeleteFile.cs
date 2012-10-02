using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginDeleteFileExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginDeleteFile() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginDeleteFile("/path/to/file", new AsyncCallback(DeleteFileCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void DeleteFileCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndDeleteFile(ar);
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
