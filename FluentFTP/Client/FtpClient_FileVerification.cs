using System;
using System.IO;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;

#endif
#if (CORE || NET45)
using System.Threading.Tasks;

#endif
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region Verification

		private bool SupportsChecksum() {
			return HasFeature(FtpCapability.HASH) || HasFeature(FtpCapability.MD5) ||
					HasFeature(FtpCapability.XMD5) || HasFeature(FtpCapability.XCRC) ||
					HasFeature(FtpCapability.XSHA1) || HasFeature(FtpCapability.XSHA256) ||
					HasFeature(FtpCapability.XSHA512);
		}

		private bool VerifyTransfer(string localPath, string remotePath) {

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
				LogStatus(FtpTraceLevel.Warn, "Failed to verify file " + localPath + " : " + ex.Message);
				return false;
			}
		}

#if ASYNC
		private async Task<bool> VerifyTransferAsync(string localPath, string remotePath, CancellationToken token = default(CancellationToken)) {

			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			try {
				if (SupportsChecksum()) {
					FtpHash hash = await GetChecksumAsync(remotePath, FtpHashAlgorithm.NONE, token);
					if (!hash.IsValid) {
						return false;
					}

					return hash.Verify(localPath);
				}

				// not supported, so return true to ignore validation
				return true;
			}
			catch (IOException ex) {
				LogStatus(FtpTraceLevel.Warn, "Failed to verify file " + localPath + " : " + ex.Message);
				return false;
			}
		}
		
#endif

		#endregion

		#region Utilities

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		private void ReportProgress(IProgress<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = FtpProgress.Generate(fileSize, position, bytesProcessed, elapsedtime, localPath, remotePath, metaProgress);

			// send progress to parent
			progress.Report(status);
		}

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		private void ReportProgress(Action<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = FtpProgress.Generate(fileSize, position, bytesProcessed, elapsedtime, localPath, remotePath, metaProgress);

			// send progress to parent
			progress(status);
		}
		#endregion
	}
}