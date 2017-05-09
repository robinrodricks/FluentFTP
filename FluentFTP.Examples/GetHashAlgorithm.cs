using System;
using System.Net;
using FluentFTP;

namespace Examples {
    public class GetHashAlgorithmExample {
        public static void GetHashAlgorithm() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("user", "pass");
                cl.Host = "some.ftpserver.on.the.internet.com";

                Console.WriteLine("The server is using the following algorithm for computing hashes: "+ 
                    cl.GetHashAlgorithm());   
            }
        }
    }
}
