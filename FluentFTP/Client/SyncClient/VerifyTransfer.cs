using System;
using System.IO;
using FluentFTP.Helpers;
using FluentFTP.Streams;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Verify a file transfer
		/// </summary>
		/// <param name="localPath"></param>
		/// <param name="remotePath"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		protected bool VerifyTransfer(string localPath, string remotePath, FtpVerify verify) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			// Isolate verify methods, which are the top byte of 16.
			FtpVerify verifyMethod = (FtpVerify)((ushort)verify & 0xFF00);

			if (verifyMethod == FtpVerify.None) {
				verifyMethod = FtpVerify.Checksum;
			}

			try {
				//fallback to size if only checksum is set and the server does not support hashing.
				if (verifyMethod == FtpVerify.Checksum && !SupportsChecksum()) {
					Log(FtpTraceLevel.Verbose, "Source server does not support any common hashing algorithm");
					Log(FtpTraceLevel.Verbose, "Falling back to file size comparison");
					verifyMethod = FtpVerify.Size;
				}

				//compare size
				if (verifyMethod.HasFlag(FtpVerify.Size)) {
					var localSize = FtpFileStream.GetFileSize(localPath, false);
					var remoteSize = GetFileSize(remotePath, -1);
					if (localSize != remoteSize) {
						return false;
					}
				}

				//compare date modified
				if (verifyMethod.HasFlag(FtpVerify.Date)) {
					var localDate = FtpFileStream.GetFileSize(localPath, true);
					var remoteDate = GetModifiedTime(remotePath);
					if (!localDate.Equals(remoteDate)) {
						return false;
					}
				}

				//compare hash
				if (verifyMethod.HasFlag(FtpVerify.Checksum) && SupportsChecksum()) {
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
