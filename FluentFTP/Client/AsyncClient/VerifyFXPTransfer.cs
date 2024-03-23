using System;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Streams;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Verify an FXP transfer
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="fxpDestinationClient"></param>
		/// <param name="remotePath"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		protected async Task<bool> VerifyFXPTransferAsync(string sourcePath, AsyncFtpClient fxpDestinationClient, string remotePath, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (sourcePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(sourcePath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			if (fxpDestinationClient is null) {
				throw new ArgumentNullException(nameof(fxpDestinationClient), "Destination FXP AsyncFtpClient cannot be null!");
			}

			FtpVerifyMethod verifyMethod = Config.VerifyMethod;

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);

			//fallback to size if only checksum is set and the server does not support hashing.
			if (verifyMethod == FtpVerifyMethod.Checksum && algorithm == FtpHashAlgorithm.NONE) {
				Log(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
				Log(FtpTraceLevel.Info, "Falling back to file size comparison");
				verifyMethod = FtpVerifyMethod.Size;
			}

			//compare size
			if (verifyMethod.HasFlag(FtpVerifyMethod.Size)) {
				var sourceSize = await GetFileSize(sourcePath, -1, token);
				var remoteSize = await GetFileSize(remotePath, -1, token);
				if (sourceSize != remoteSize) {
					return false;
				}
			}

			//compare date modified
			if (verifyMethod.HasFlag(FtpVerifyMethod.Date)) {
				var sourceDate = await FtpFileStream.GetFileDateModifiedUtcAsync(sourcePath, token);
				var remoteDate = await GetModifiedTime(remotePath, token);
				if (!sourceDate.Equals(remoteDate)) {
					return false;
				}
			}

			//compare hash
			if (verifyMethod.HasFlag(FtpVerifyMethod.Checksum) && algorithm != FtpHashAlgorithm.NONE) {
				// get the hashes of both files using the same mutual algorithm
				FtpHash sourceHash = await GetChecksum(sourcePath, algorithm, token);
				if (!sourceHash.IsValid) {
					return false;
				}

				FtpHash destinationHash = await fxpDestinationClient.GetChecksum(remotePath, algorithm, token);
				if (!destinationHash.IsValid) {
					return false;
				}

				return sourceHash.Value == destinationHash.Value;
			}

			// check was successful
			return true;
		}

	}
}