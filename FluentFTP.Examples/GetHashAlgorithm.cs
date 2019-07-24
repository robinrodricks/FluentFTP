using System;
using System.Net;
using FluentFTP;

namespace Examples {
	internal static class GetHashAlgorithmExample {
		public static void GetHashAlgorithm() {
			using (var cl = new FtpClient()) {
				cl.Credentials = new NetworkCredential("user", "pass");
				cl.Host = "some.ftpserver.on.the.internet.com";

				Console.WriteLine("The server is using the following algorithm for computing hashes: " +
				                  cl.GetHashAlgorithm());
			}
		}
	}
}