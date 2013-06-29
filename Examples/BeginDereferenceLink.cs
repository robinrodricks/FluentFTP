using System;
using System.Net;
using System.Threading;
using System.Net.FtpClient;

namespace Examples {
    /// <summary>
    /// This example illustrates how to dereference a symbolic link asyncrhonously. The
    /// code bollow takes a FtpListItem object and checks if it is a symbolic link and
    /// that the LinkTarget property has been initalized before executing the method. Not
    /// doing so can result in a FtpException being thrown.
    /// 
    /// Also see the DerefenceLink() example! There is lots of information
    /// not mentioned here!
    /// </summary>
    static class BeginDereferenceLink {
        static ManualResetEvent m_reset = new ManualResetEvent(false);

        public static void BeginDereferenceLinkExample(FtpListItem item) {
            using (FtpClient conn = new FtpClient()) {
                m_reset.Reset();

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.Connect();

                if (item.Type == FtpFileSystemObjectType.Link && item.LinkTarget != null) {
                    conn.BeginDereferenceLink(item, new AsyncCallback(DereferenceLinkCallback), conn);
                    m_reset.WaitOne();
                }

                conn.Disconnect();
            }
        }

        static void DereferenceLinkCallback(IAsyncResult ar) {
            FtpClient conn = ar.AsyncState as FtpClient;
            FtpListItem target;

            try {
                if (conn == null)
                    throw new InvalidOperationException("The FtpControlConnection object is null!");

                target = conn.EndDereferenceLink(ar);
                if (target != null) {
                    // success...
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
