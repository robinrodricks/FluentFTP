using System;
using System.Net;
using System.Net.FtpClient;
using System.IO;
using System.Threading;

namespace Examples {
    public static class BeginOpenReadExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginOpenRead() {
            // The using statement here is OK _only_ because m_reset.WaitOne()
            // causes the code to block until the async process finishes, otherwise
            // the connection object would be disposed early. In practice, you
            // typically would not wrap the following code with a using statement.
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.BeginOpenRead("/path/to/file",
                    new AsyncCallback(BeginOpenReadCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void BeginOpenReadCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                using (Stream istream = conn.EndOpenRead(ar)) {
                    byte[] buf = new byte[8192];

                    try {
                        DateTime start = DateTime.Now;

                        while (istream.Read(buf, 0, buf.Length) > 0) {
                            double perc = 0;

                            if (istream.Length > 0)
                                perc = (double)istream.Position / (double)istream.Length;

                            Console.Write("\rTransferring: {0}/{1} {2}/s {3:p}         ",
                                          istream.Position.FormatBytes(),
                                          istream.Length.FormatBytes(),
                                          (istream.Position / DateTime.Now.Subtract(start).TotalSeconds).FormatBytes(),
                                          perc);
                        }
                    }
                    finally {
                        Console.WriteLine();
                        istream.Close();
                    }
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
