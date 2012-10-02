using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class GetModifiedTimeExample {
        public static void GetModifiedTime() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                Console.WriteLine("The modified type is: {0}",
                    conn.GetModifiedTime("/full/or/relative/path/to/file"));
            }
        }
    }
}
