using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public static class ConnectExample {
        public static void Connect() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                conn.Connect();
            }
        }
    }
}
