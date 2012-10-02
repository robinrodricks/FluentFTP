using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class DirectoryExistsExample {
        public static void DeleteDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");

                if (conn.DirectoryExists("/full/or/relative/path")) {
                    // do something
                }

                
            }
        }
    }
}
