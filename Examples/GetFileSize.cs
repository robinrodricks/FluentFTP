using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class GetFileSizeExample {
        public static void GetFileSize() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                Console.WriteLine("The file size is: {0}",
                    conn.GetFileSize("/full/or/relative/path/to/file"));
            }
        }
    }
}
