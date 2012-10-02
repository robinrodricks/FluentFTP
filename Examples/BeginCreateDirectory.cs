using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public static class BeginCreateDirectoryExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginCreateDirectory() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
        
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.DeleteDirectory("/test", true);
                conn.BeginCreateDirectory("/test/path/that/should/be/created", true,
                    new AsyncCallback(CreateDirectoryCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void CreateDirectoryCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                conn.EndCreateDirectory(ar);
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
