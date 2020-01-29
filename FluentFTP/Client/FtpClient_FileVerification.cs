using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
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
		#region Verification

		private bool VerifyTransfer(string localPath, string remotePath) {
			// verify args
			if (localPath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "localPath");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			if (HasFeature(FtpCapability.HASH) || HasFeature(FtpCapability.MD5) ||
				HasFeature(FtpCapability.XMD5) || HasFeature(FtpCapability.XCRC) ||
				HasFeature(FtpCapability.XSHA1) || HasFeature(FtpCapability.XSHA256) ||
				HasFeature(FtpCapability.XSHA512)) {
				var hash = GetChecksum(remotePath);
				if (!hash.IsValid) {
					return false;
				}

				return hash.Verify(localPath);
			}

			//Not supported return true to ignore validation
			return true;
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

			if (HasFeature(FtpCapability.HASH) || HasFeature(FtpCapability.MD5) ||
				HasFeature(FtpCapability.XMD5) || HasFeature(FtpCapability.XCRC) ||
				HasFeature(FtpCapability.XSHA1) || HasFeature(FtpCapability.XSHA256) ||
				HasFeature(FtpCapability.XSHA512)) {
				FtpHash hash = await GetChecksumAsync(remotePath, token);
				if (!hash.IsValid) {
					return false;
				}

				return hash.Verify(localPath);
			}

			//Not supported return true to ignore validation
			return true;
		}
#endif

		#endregion

		#region Utilities

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		private void ReportProgress(IProgress<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = CalculateProgress(fileSize, position, bytesProcessed, elapsedtime);

			// send progress to parent
			progress.Report(status);
		}

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		private void ReportProgress(Action<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = CalculateProgress(fileSize, position, bytesProcessed, elapsedtime);

			// send progress to parent
			progress(status);
		}

		private static FtpProgress CalculateProgress(long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime) {
			// default values to send
			double progressValue = -1;
			double transferSpeed = 0;
			var estimatedRemaingTime = TimeSpan.Zero;

			// catch any divide-by-zero errors
			try {
				// calculate % based on file length vs file offset
				// send a value between 0-100 indicating percentage complete
				progressValue = (double)position / (double)fileSize * 100;

				// calculate raw transferSpeed (bytes per second)
				transferSpeed = bytesProcessed / elapsedtime.TotalSeconds;

				//calculate remaining time			
				estimatedRemaingTime = TimeSpan.FromSeconds((fileSize - position) / transferSpeed);
			}
			catch (Exception) {
			}

			// suppress invalid values and send -1 instead
			if (double.IsNaN(progressValue) && double.IsInfinity(progressValue)) {
				progressValue = -1;
			}

			if (double.IsNaN(transferSpeed) && double.IsInfinity(transferSpeed)) {
				transferSpeed = 0;
			}

			var p = new FtpProgress(progressValue, transferSpeed, estimatedRemaingTime);
			return p;
		}

		#endregion
	}
}