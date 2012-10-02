using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    static class GetListingExample {
        public static void GetListing() {
            using (FtpClient conn = new FtpClient()) {
                conn.Host = "localhost";
                conn.Credentials = new NetworkCredential("ftptest", "ftptest");
                
                foreach (FtpListItem item in conn.GetListing(conn.GetWorkingDirectory(),
                    FtpListOption.Modify | FtpListOption.Size)) {

                    switch (item.Type) {
                        case FtpFileSystemObjectType.Directory:
                            break;
                        case FtpFileSystemObjectType.File:
                            break;
                    }
                }
            }
        }
    }
}
