using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	public static class GetChecksumExample {

		public static void GetChecksum() {
			
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				FtpHash hash = conn.GetChecksum("/path/to/remote/file");

				// Make sure it returned a, to the best of our knowledge, valid
				// hash object. The commands for retrieving checksums are
				// non-standard extensions to the protocol so we have to
				// presume that the response was in a format understood by
				// FluentFTP and parsed correctly.
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


		public static async Task GetChecksumAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				FtpHash hash = await conn.GetChecksumAsync("/path/to/remote/file", token);

				// Make sure it returned a, to the best of our knowledge, valid
				// hash object. The commands for retrieving checksums are
				// non-standard extensions to the protocol so we have to
				// presume that the response was in a format understood by
				// FluentFTP and parsed correctly.
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