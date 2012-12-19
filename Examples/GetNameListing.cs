using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    class GetNameListingExample {
        public static void GetNameListing() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftp", "ftp");
                cl.Host = "ftp.example.com";
                cl.Connect();

                foreach (string s in cl.GetNameListing()) {
                    // load some information about the object
                    // returned from the listing...
                    bool isDirectory = cl.DirectoryExists(s);
                    DateTime modify = cl.GetModifiedTime(s);
                    long size = isDirectory ? 0 : cl.GetFileSize(s);

                    
                }
            }
        }
    }
}
