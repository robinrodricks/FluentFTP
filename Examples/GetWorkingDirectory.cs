using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class GetWorkingDirectoryExample {
        public static void GetWorkingDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                Console.WriteLine("The working directory is: {0}",
                    conn.GetWorkingDirectory());
            }
        }
    }
}
