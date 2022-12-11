using System;
using System.IO;
using FluentFTP.Helpers;
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
		protected bool VerifyTransfer(string localPath, string remotePath) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(localPath));
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			try {
				if (SupportsChecksum()) {
					var hash = GetChecksum(remotePath);
					if (!hash.IsValid) {
						return false;
					}

					return hash.Verify(localPath);
				}

				// not supported, so return true to ignore validation
				return true;
			}
			catch (IOException ex) {
				LogWithPrefix(FtpTraceLevel.Warn, "Failed to verify file " + localPath + " : " + ex.Message);
				return false;
			}
		}

	}
}
