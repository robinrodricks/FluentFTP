using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	public static class GetChecksumExample {

		//-----------------------------------------------------------------------------------------
		// NOTE! GetChecksum automatically uses the first available hash algorithm on the server,
		//		 and it should be used as far as possible instead of GetHash, GetMD5, GetSHA256...
		//-----------------------------------------------------------------------------------------

		public static void GetChecksum() {
			
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// Get a hash checksum for the file
				FtpHash hash = conn.GetChecksum("/path/to/remote/file");

				// Make sure it returned a valid hash object
				if (hash.IsValid) {
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

				// Get a hash checksum for the file
				FtpHash hash = await conn.GetChecksumAsync("/path/to/remote/file", FtpHashAlgorithm.NONE, token);

				// Make sure it returned a valid hash object
				if (hash.IsValid) {
					if (hash.Verify("/some/local/file")) {
						Console.WriteLine("The checksum's match!");
					}
				}
			}
		}

	}
}