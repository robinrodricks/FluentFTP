using System;
using System.Net;
using FluentFTP;

namespace Examples {
    static class DeleteFileExample {
        public static void DeleteFile() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.DeleteFile("/full/or/relative/path/to/file");
            }
        }
    }
}
