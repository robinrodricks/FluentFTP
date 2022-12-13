using System;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Verify an FXP file transfer
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="fxpDestinationClient"></param>
		/// <param name="remotePath"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		protected bool VerifyFXPTransfer(string sourcePath, FtpClient fxpDestinationClient, string remotePath) {

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
				Log(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
			}

			// since not supported return true to ignore validation
			return true;
		}

	}
}
