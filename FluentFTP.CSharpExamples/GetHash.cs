using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class GetHashExample {

		//-----------------------------------------------------------------------------------------
		// NOTE! GetChecksum automatically uses the first available hash algorithm on the server,
		//		 and it should be used as far as possible instead of GetHash, GetMD5, GetSHA256...
		//-----------------------------------------------------------------------------------------

		public static void GetHash() {
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				conn.Connect();

				// If server supports the HASH command then the
				// FtpClient.HashAlgorithms flags will NOT be equal
				// to FtpHashAlgorithm.NONE. 
				if (conn.HashAlgorithms != FtpHashAlgorithm.NONE) {
					FtpHash hash;

					// Ask the server to compute the hash using whatever 
					// the default hash algorithm (probably SHA-1) on the 
					// server is.
					hash = conn.GetHash("/path/to/remote/somefile.ext");

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
					if (conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
						conn.SetHashAlgorithm(FtpHashAlgorithm.MD5);
						hash = conn.GetHash("/path/to/remote/somefile.ext");
						if (hash.Verify("/path/to/local/somefile.ext")) {
							Console.WriteLine("The computed hashes match!");
						}
					}
				}
			}
		}

		public static async Task GetHashAsync() {
			var token = new CancellationToken();
			using (var conn = new FtpClient("127.0.0.1", "ftptest", "ftptest")) {
				await conn.ConnectAsync(token);

				// If server supports the HASH command then the
				// FtpClient.HashAlgorithms flags will NOT be equal
				// to FtpHashAlgorithm.NONE. 
				if (conn.HashAlgorithms != FtpHashAlgorithm.NONE) {
					FtpHash hash;

					// Ask the server to compute the hash using whatever 
					// the default hash algorithm (probably SHA-1) on the 
					// server is.
					hash = await conn.GetHashAsync("/path/to/remote/somefile.ext", token);

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
					if (conn.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
						conn.SetHashAlgorithm(FtpHashAlgorithm.MD5);
						hash = await conn.GetHashAsync("/path/to/remote/somefile.ext", token);
						if (hash.Verify("/path/to/local/somefile.ext")) {
							Console.WriteLine("The computed hashes match!");
						}
					}
				}
			}
		}


	}
}