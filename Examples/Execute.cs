using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class ExecuteExample {
        public static void Execute() {
            using (FtpClient conn = new FtpClient()) {
                FtpReply reply;

                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                
                if (!(reply = conn.Execute("SITE CHMOD 640 FOO.TXT")).Success) {
                    throw new FtpCommandException(reply);
                }
            }
        }
    }
}
