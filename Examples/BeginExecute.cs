using System;
using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace Examples {
    public class BeginExecuteExample {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginExecute() {
            // The using statement here is OK _only_ because m_reset.WaitOne()
            // causes the code to block until the async process finishes, otherwise
            // the connection object would be disposed early. In practice, you
            // typically would not wrap the following code with a using statement.
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();
                
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.Connect();
                conn.BeginExecute("SYST", new AsyncCallback(BeginExecuteCallback), conn);

                m_reset.WaitOne();
                conn.Disconnect();
            }
        }

        static void BeginExecuteCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;
            FtpReply reply;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                reply = conn.EndExecute(ar);
                if (!reply.Success)
                    throw new FtpCommandException(reply);

                Console.WriteLine(reply.Message);
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
