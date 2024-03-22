using System;
using FluentFTP.Helpers;

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
		protected bool VerifyFXPTransfer(string sourcePath, FtpClient fxpDestinationClient, string remotePath, FtpVerify verify) {

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

			// Isolate verify methods, which are the top byte of 16.
			FtpVerify verifyMethod = (FtpVerify)((ushort)verify & 0xFF00);

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);

			//fallback to size if only checksum is set and the server does not support hashing.
			if (verifyMethod == FtpVerify.Checksum && algorithm == FtpHashAlgorithm.NONE) {
				Log(FtpTraceLevel.Info, "Source and Destination servers do not support any common hashing algorithm");
				Log(FtpTraceLevel.Info, "Falling back to file size comparison");
				verifyMethod = FtpVerify.Size;
			}

			//compare size
			if (verifyMethod.HasFlag(FtpVerify.Size)) {
				var sourceSize = GetFileSize(sourcePath, -1);
				var remoteSize = GetFileSize(remotePath, -1);
				if (sourceSize != remoteSize) {
					return false;
				}
			}

			//compare date modified
			if (verifyMethod.HasFlag(FtpVerify.Date)) {
				var sourceDate = GetFileSize(sourcePath);
				var remoteDate = GetModifiedTime(remotePath);
				if (!sourceDate.Equals(remoteDate)) {
					return false;
				}
			}

			//compare hash
			if (verifyMethod.HasFlag(FtpVerify.Checksum) && algorithm != FtpHashAlgorithm.NONE) {
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

			// check was successful
			return true;
		}

	}
}
