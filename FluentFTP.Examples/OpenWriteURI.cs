using System;
using System.IO;
using FluentFTP;

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
