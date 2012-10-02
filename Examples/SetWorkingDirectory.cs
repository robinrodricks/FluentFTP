using System;
using System.Net;
using System.Net.FtpClient;
using System.IO;

namespace Examples {
    public static class SetWorkingDirectoryExample {
        public static void SetWorkingDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.SetWorkingDirectory("/full/or/relative/path");
            }
        }
    }
}
