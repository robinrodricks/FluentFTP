using System;
using System.Net;
using System.Net.FtpClient;
using System.Net.FtpClient.Extensions;

namespace Examples {
    public static class GetChecksumExample {
        public static void GetChceksumExample() {
            FtpHash hash = null;

            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("user", "pass");
                cl.Host = "some.ftpserver.on.the.internet.com";

                hash = cl.GetChecksum("/path/to/remote/file");
                // Make sure it returned a, to the best of our knowledge, valid
                // hash object. The commands for retrieving checksums are
                // non-standard extensions to the protocol so we have to
                // presume that the response was in a format understood by
                // System.Net.FtpClient and parsed correctly.
                //
                // In addition, there is no built-in support for verifying
                // CRC hashes. You will need to write you own or use a 
                // third-party solution.
                if (hash.IsValid && hash.Algorithm != FtpHashAlgorithm.CRC) {
                    if (hash.Verify("/some/local/file")) {
                        Console.WriteLine("The checksum's match!");
                    }
                }
            }
        }
    }
}
