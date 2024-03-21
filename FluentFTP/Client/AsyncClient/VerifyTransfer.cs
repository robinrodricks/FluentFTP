using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Streams;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Verify a file transfer
		/// </summary>
		/// <param name="localPath"></param>
		/// <param name="remotePath"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		protected async Task<bool> VerifyTransferAsync(string localPath, string remotePath, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			try {

				var localSize = await FtpFileStream.GetFileSizeAsync(localPath, false, token);
				var remoteSize = await GetFileSize(remotePath, -1, token);
				if (localSize != remoteSize) {
					 return false;
				}

				if (SupportsChecksum()) {
					FtpHash hash = await GetChecksum(remotePath, FtpHashAlgorithm.NONE, token);
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
