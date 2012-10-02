using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginDirectoryExistsExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginDirectoryExists() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginDirectoryExists("/path/to/directory", new AsyncCallback(DirectoryExistsCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void DirectoryExistsCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                Console.WriteLine("Directory Exiss: {0}", conn.EndDirectoryExists(ar));
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
