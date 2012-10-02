using System;
using System.IO;
using System.Net.FtpClient;

namespace Examples {
    public static class OpenReadURI {
        public static void OpenURI() {
            using (Stream s = FtpClient.OpenRead(new Uri("ftp://server/path/file"))) {
                byte[] buf = new byte[8192];
                int read = 0;

                try {
                    while ((read = s.Read(buf, 0, buf.Length)) > 0) {
                        Console.Write("\r{0}/{1} {2:p}     ",
                            s.Position, s.Length,
                            ((double)s.Position / (double)s.Length));
                    }
                }
                finally {
                    Console.WriteLine();
                    s.Close();
                }
            }
        }
    }
}
