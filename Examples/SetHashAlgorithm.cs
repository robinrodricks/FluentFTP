using System;
using System.Net;
using System.Net.FtpClient;

namespace Examples {
    public class SetHashAlgorithmExample {
        public static void SetHashAlgorithm() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("user", "pass");
                cl.Host = "some.ftpserver.on.the.internet.com";

                if (cl.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5))
                    cl.SetHashAlgorithm(FtpHashAlgorithm.MD5);
            }
        }
    }
}
