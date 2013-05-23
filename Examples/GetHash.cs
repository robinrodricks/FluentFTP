using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public class GetHashExample {
        public static void GetHash() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("user", "pass");
                cl.Host = "some.ftpserver.on.the.internet.com";

                // If server supports the HASH command then the
                // FtpClient.HashAlgorithms flags will NOT be equal
                // to FtpHashAlgorithm.NONE. 
                if (cl.HashAlgorithms != FtpHashAlgorithm.NONE) {
                    FtpHash hash;

                    // Ask the server to compute the hash using whatever 
                    // the default hash algorithm (probably SHA-1) on the 
                    // server is.
                    hash = cl.GetHash("/path/to/remote/somefile.ext");

                    // The FtpHash.Verify method computes the hash of the
                    // specified file or stream based on the hash algorithm
                    // the server computed its hash with. The classes used
                    // for computing the local hash are  part of the .net
                    // framework, located in the System.Security.Cryptography
                    // namespace and are derived from 
                    // System.Security.Cryptography.HashAlgorithm.
                    if (hash.Verify("/path/to/local/somefile.ext")) {
                        Console.WriteLine("The computed hashes match!");
                    }

                    // Manually specify the hash algorithm to use.
                    if (cl.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
                        cl.SetHashAlgorithm(FtpHashAlgorithm.MD5);
                        hash = cl.GetHash("/path/to/remote/somefile.ext");
                        if (hash.Verify("/path/to/local/somefile.ext")) {
                            Console.WriteLine("The computed hashes match!");
                        }
                    }
                }
            }
        }
    }
}
