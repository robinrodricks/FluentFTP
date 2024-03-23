using System;
using System.IO;
using FluentFTP.Helpers;
using FluentFTP.Streams;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Verify a file transfer
		/// </summary>
		/// <param name="localPath"></param>
		/// <param name="remotePath"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		protected bool VerifyTransfer(string localPath, string remotePath) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			FtpVerifyMethod verifyMethod = Config.VerifyMethod;

			try {

				//fallback to size if only checksum is set and the server does not support hashing.
				if (verifyMethod == FtpVerifyMethod.Checksum && !SupportsChecksum()) {
					Log(FtpTraceLevel.Info, "Source server does not support any common hashing algorithm");
					Log(FtpTraceLevel.Info, "Falling back to file size comparison");
					verifyMethod = FtpVerifyMethod.Size;
				}

				//compare size
				if (verifyMethod.HasFlag(FtpVerifyMethod.Size)) {
					var localSize = FtpFileStream.GetFileSize(localPath, false);
					var remoteSize = GetFileSize(remotePath, -1);
					if (localSize != remoteSize) {
						return false;
					}
				}

				//compare date modified
				if (verifyMethod.HasFlag(FtpVerifyMethod.Date)) {
					var localDate = FtpFileStream.GetFileDateModifiedUtc(localPath);
					var remoteDate = GetModifiedTime(remotePath);
					if (!localDate.Equals(remoteDate)) {
						return false;
					}
				}

				//compare hash
				if (verifyMethod.HasFlag(FtpVerifyMethod.Checksum) && SupportsChecksum()) {
					FtpHash hash = GetChecksum(remotePath, FtpHashAlgorithm.NONE);
					if (!hash.IsValid) {
						return false;
					}

					return hash.Verify(localPath);
				}

				// check was successful
				return true;
			}
			catch (IOException ex) {
				LogWithPrefix(FtpTraceLevel.Warn, "Failed to verify file " + localPath, ex);
				return false;
			}
		}

	}
}
