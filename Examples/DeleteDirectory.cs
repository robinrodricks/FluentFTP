using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class DeleteDirectoryExample {
        public static void DeleteDirectory() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                
                // Remove the directory and all objects beneath it. The last parameter
                // forces System.Net.FtpClient to use LIST -a for getting a list of objects
                // beneath the specified directory.
                conn.DeleteDirectory("/path/to/directory", true, 
                    FtpListOption.AllFiles | FtpListOption.ForceList);
            }
        }
    }
}
