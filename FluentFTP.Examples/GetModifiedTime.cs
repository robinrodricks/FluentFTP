using System;
using System.Net;
using FluentFTP;

namespace Examples {
    static class GetModifiedTimeExample {
        public static void GetModifiedTime() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                Console.WriteLine("The modified type is: "+
                    conn.GetModifiedTime("/full/or/relative/path/to/file"));
            }
        }
    }
}
