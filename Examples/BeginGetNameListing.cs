using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public static class BeginGetNameListingExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginGetNameListing() {
            // The using statement here is OK _only_ because m_reset.WaitOne()
            // causes the code to block until the async process finishes, otherwise
            // the connection object would be disposed early. In practice, you
            // typically would not wrap the following code with a using statement.
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginGetNameListing(new AsyncCallback(EndGetNameListing), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void EndGetNameListing(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                foreach (string s in conn.EndGetNameListing(ar)) {
                    // load some information about the object
                    // returned from the listing...
                    bool isDirectory = conn.DirectoryExists(s);
                    DateTime modify = conn.GetModifiedTime(s);
                    long size = isDirectory ? 0 : conn.GetFileSize(s);
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
