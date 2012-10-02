using System;
using System.IO;
using System.Net.FtpClient;

namespace Examples {
    static class OpenWriteURI {
        public static void OpenURI() {
            using (Stream s = FtpClient.OpenWrite(new Uri("ftp://server/path/file"))) {
                try {
                    // write data to the file on the server
                }
                finally {
                    s.Close();
                }
            }
        }
    }
}
