using System;
using System.IO;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public class OpenWriteExample {
        public static void OpenWrite() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");

                using (Stream ostream = conn.OpenWrite("/full/or/relative/path/to/file")) {
                    try {
                        // istream.Position is incremented accordingly to the writes you perform
                    }
                    finally {
                        ostream.Close();
                    }
                }
            }
        }
    }
}

