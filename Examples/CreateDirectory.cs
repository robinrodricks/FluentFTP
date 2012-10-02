using System;
using System.Net;
using System.Net.FtpClient;
using System.IO;

namespace Examples {
    public static class CreateDirectoryExample {
        public static void CreateDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.CreateDirectory("/test/path/that/should/be/created", true);
            }
        }
    }
}
