using System;
using System.Net;
using System.Net.FtpClient;
using System.IO;
using System.Threading;

namespace Examples {
    public static class BeginOpenAppendExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginOpenAppend() {
            // The using statement here is OK _only_ because m_reset.WaitOne()
            // causes the code to block until the async process finishes, otherwise
            // the connection object would be disposed early. In practice, you
            // typically would not wrap the following code with a using statement.
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginOpenAppend("/path/to/file",
                    new AsyncCallback(BeginOpenAppendCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void BeginOpenAppendCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;
            Stream istream = null, ostream = null;
            byte[] buf = new byte[8192];
            int read = 0;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                ostream = conn.EndOpenAppend(ar);
                istream = new FileStream("input_file", FileMode.Open, FileAccess.Read);

                while ((read = istream.Read(buf, 0, buf.Length)) > 0) {
                    ostream.Write(buf, 0, read);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            finally {
                if (istream != null)
                    istream.Close();

                if (ostream != null)
                    ostream.Close();

                m_reset.Set();
            }
        }
    }
}
