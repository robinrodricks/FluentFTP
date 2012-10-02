using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class FileExistsExample {
        public static void FileExists() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");

                // The last parameter forces System.Net.FtpClient to use LIST -a 
                // for getting a list of objects in the parent directory.
                if (conn.FileExists("/full/or/relative/path", 
                    FtpListOption.ForceList | FtpListOption.AllFiles)) {
                    // dome something
                }
            }
        }
    }
}
