using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginGetWorkingDirectoryExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginGetWorkingDirectory() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.Connect();
                conn.BeginGetWorkingDirectory(new AsyncCallback(BeginGetWorkingDirectoryCallback), conn);
                
                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void BeginGetWorkingDirectoryCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                Console.WriteLine("Working directory: {0}", conn.EndGetWorkingDirectory(ar));
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
