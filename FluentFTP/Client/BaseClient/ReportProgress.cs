using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		protected void ReportProgress(IProgress<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = FtpProgress.Generate(fileSize, position, bytesProcessed, elapsedtime, localPath, remotePath, metaProgress);

			// send progress to parent
			progress.Report(status);
		}

		/// <summary>
		/// Sends progress to the user, either a value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		protected void ReportProgress(Action<FtpProgress> progress, long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			//  calculate % done, transfer speed and time remaining
			FtpProgress status = FtpProgress.Generate(fileSize, position, bytesProcessed, elapsedtime, localPath, remotePath, metaProgress);

			// send progress to parent
			progress(status);
		}

	}
}