using System;
using System.IO;
using FluentFTP;

namespace Examples {
    class OpenAppendURI {
        public static void OpenURI() {
            using (Stream s = FtpClient.OpenAppend(new Uri("ftp://server/path/file"))) {
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
