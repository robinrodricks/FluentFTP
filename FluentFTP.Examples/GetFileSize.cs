using System;
using System.Net;
using FluentFTP;

namespace Examples {
    static class GetFileSizeExample {
        public static void GetFileSize() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                Console.WriteLine("The file size is: "+
                    conn.GetFileSize("/full/or/relative/path/to/file"));
            }
        }
    }
}
