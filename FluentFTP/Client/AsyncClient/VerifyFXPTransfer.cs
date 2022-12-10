using System;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

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

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);
			if (algorithm != FtpHashAlgorithm.NONE) {

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
			else {
				Log(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
			}

			// since not supported return true to ignore validation
			return true;
		}

	}
}
