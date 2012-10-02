using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginGetListing {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginGetListingExample() {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.Connect();
                conn.BeginGetListing(new AsyncCallback(GetListingCallback), conn);
                
                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void GetListingCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                foreach (FtpListItem item in conn.EndGetListing(ar))
                    Console.WriteLine(item);
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
