using System;
using System.IO;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		protected bool VerifyTransfer(string localPath, string remotePath) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
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
