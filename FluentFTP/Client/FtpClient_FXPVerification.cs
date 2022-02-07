using System;
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;

#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		private bool VerifyFXPTransfer(string sourcePath, FtpClient fxpDestinationClient, string remotePath) {
			
			// verify args
			if (sourcePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(sourcePath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			if (fxpDestinationClient is null) {
				throw new ArgumentNullException(nameof(fxpDestinationClient), "Destination FXP FtpClient cannot be null!");
			}

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);
			if (algorithm != FtpHashAlgorithm.NONE) {

				// get the hashes of both files using the same mutual algorithm

				FtpHash sourceHash = GetChecksum(sourcePath, algorithm);
				if (!sourceHash.IsValid) {
					return false;
				}

				FtpHash destinationHash = fxpDestinationClient.GetChecksum(remotePath, algorithm);
				if (!destinationHash.IsValid) {
					return false;
				}

				return sourceHash.Value == destinationHash.Value;
			}
			else {
				LogLine(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
			}

			// since not supported return true to ignore validation
			return true;
		}

#if ASYNC
		private async Task<bool> VerifyFXPTransferAsync(string sourcePath, FtpClient fxpDestinationClient, string remotePath, CancellationToken token = default(CancellationToken)) {
			
			// verify args
			if (sourcePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(sourcePath));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			if (fxpDestinationClient is null) {
				throw new ArgumentNullException(nameof(fxpDestinationClient), "Destination FXP FtpClient cannot be null!");
			}

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);
			if (algorithm != FtpHashAlgorithm.NONE) {

				// get the hashes of both files using the same mutual algorithm

				FtpHash sourceHash = await GetChecksumAsync(sourcePath, algorithm, token);
				if (!sourceHash.IsValid) {
					return false;
				}

				FtpHash destinationHash = await fxpDestinationClient.GetChecksumAsync(remotePath, algorithm, token);
				if (!destinationHash.IsValid) {
					return false;
				}

				return sourceHash.Value == destinationHash.Value;
			}
			else {
				LogLine(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
			}

			// since not supported return true to ignore validation
			return true;
		}

#endif


	}
}