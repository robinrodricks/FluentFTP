using System;
using System.Net;
using System.Net.FtpClient;
using System.IO;

namespace Examples {
    public static class RenameExample {
        public static void Rename() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                // renaming a directory is dependant on the server! if you attempt it
                // and it fails it's not because System.Net.FtpClient has a bug!
                conn.Rename("/full/or/relative/path/to/src", "/full/or/relative/path/to/dest");
            }
        }
    }
}
