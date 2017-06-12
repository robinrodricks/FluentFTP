using System;
using System.Net;
using FluentFTP;

namespace Examples {
    static class DeleteDirectoryExample {
        public static void DeleteDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                
                // Remove the directory and all objects beneath it.
                conn.DeleteDirectory("/path/to/directory");
            }
        }
    }
}
