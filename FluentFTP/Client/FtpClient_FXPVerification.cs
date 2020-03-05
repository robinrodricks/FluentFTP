using System;
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

		/// <summary>
		/// Get the first checksum algorithm mutually supported by both servers.
		/// </summary>
		private FtpHashAlgorithm GetFirstMutualChecksum(FtpClient destination) {

			// special handling for HASH command which is a meta-command supporting all hash types
			if (HasFeature(FtpCapability.HASH) && destination.HasFeature(FtpCapability.HASH)) {
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.MD5)) {
					return FtpHashAlgorithm.MD5;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA1)) {
					return FtpHashAlgorithm.SHA1;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA256)) {
					return FtpHashAlgorithm.SHA256;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.SHA512)) {
					return FtpHashAlgorithm.SHA512;
				}
				if (HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC) && destination.HashAlgorithms.HasFlag(FtpHashAlgorithm.CRC)) {
					return FtpHashAlgorithm.CRC;
				}
			}

			// handling for non-standard specific hashing commands
			if (HasFeature(FtpCapability.MD5) && destination.HasFeature(FtpCapability.MD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XMD5) && destination.HasFeature(FtpCapability.XMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.MMD5) && destination.HasFeature(FtpCapability.MMD5)) {
				return FtpHashAlgorithm.MD5;
			}
			if (HasFeature(FtpCapability.XSHA1) && destination.HasFeature(FtpCapability.XSHA1)) {
				return FtpHashAlgorithm.SHA1;
			}
			if (HasFeature(FtpCapability.XSHA256) && destination.HasFeature(FtpCapability.XSHA256)) {
				return FtpHashAlgorithm.SHA256;
			}
			if (HasFeature(FtpCapability.XSHA512) && destination.HasFeature(FtpCapability.XSHA512)) {
				return FtpHashAlgorithm.SHA512;
			}
			if (HasFeature(FtpCapability.XCRC) && destination.HasFeature(FtpCapability.XCRC)) {
				return FtpHashAlgorithm.CRC;
			}
			return FtpHashAlgorithm.NONE;
		}

		private bool VerifyFXPTransfer(string sourcePath, FtpClient fxpDestinationClient, string remotePath) {
			
			// verify args
			if (sourcePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			if (fxpDestinationClient is null) {
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "fxpDestinationClient");
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
				LogLine(FtpTraceLevel.Info, "Source and Destination does not support the same hash algorythm");
			}

			// since not supported return true to ignore validation
			return true;
		}

#if ASYNC
		private async Task<bool> VerifyFXPTransferAsync(string sourcePath, FtpClient fxpDestinationClient, string remotePath, CancellationToken token = default(CancellationToken)) {
			
			// verify args
			if (sourcePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			if (fxpDestinationClient is null) {
				throw new ArgumentNullException("Destination FXP FtpClient cannot be null!", "fxpDestinationClient");
			}

			// check if any algorithm is supported by both servers
			var algorithm = GetFirstMutualChecksum(fxpDestinationClient);
			if (algorithm != FtpHashAlgorithm.NONE) {

				// get the hashes of both files using the same mutual algorithm

				FtpHash sourceHash = await GetChecksumAsync(sourcePath, token, algorithm);
				if (!sourceHash.IsValid) {
					return false;
				}

				FtpHash destinationHash = await fxpDestinationClient.GetChecksumAsync(remotePath, token, algorithm);
				if (!destinationHash.IsValid) {
					return false;
				}

				return sourceHash.Value == destinationHash.Value;
			}
			else {
				LogLine(FtpTraceLevel.Info, "Source and Destination does not support the same hash algorythm");
			}

			// since not supported return true to ignore validation
			return true;
		}

#endif


	}
}