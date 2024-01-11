using System;

namespace FluentFTP {
	/// <summary>
	/// Class to report FTP file transfer progress during upload or download of files
	/// </summary>
	public class FtpProgress {
		/// <summary>
		/// A value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public double Progress { get; set; }

		/// <summary>
		/// A value indicating how many bytes have been transferred.
		/// When unable to calculate percentage, having the partial byte count may help in providing some feedback.
		/// </summary>
		public long TransferredBytes { get; set; }

		/// <summary>
		/// A value representing the current Transfer Speed in Bytes per seconds.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public double TransferSpeed { get; set; }

		/// <summary>
		/// A value representing the calculated 'Estimated time of arrival'.
		/// Used to track the progress of an individual file transfer.
		/// </summary>
		public TimeSpan ETA { get; set; }

		/// <summary>
		/// Stores the absolute remote path of the current file being transferred.
		/// </summary>
		public string RemotePath { get; set; }

		/// <summary>
		/// Stores the absolute local path of the current file being transferred.
		/// </summary>
		public string LocalPath { get; set; }

		/// <summary>
		/// Stores the index of the file in the listing.
		/// Only used when transferring multiple files or an entire directory.
		/// </summary>
		public int FileIndex { get; set; }

		/// <summary>
		/// Stores the total count of the files to be transferred.
		/// Only used when transferring multiple files or an entire directory.
		/// </summary>
		public int FileCount { get; set; }

		/// <summary>
		/// Create a new FtpProgress object for meta progress info.
		/// </summary>
		public FtpProgress(int fileCount, int fileIndex) {
			FileCount = fileCount;
			FileIndex = fileIndex;
		}

		/// <summary>
		/// Create a new FtpProgress object for individual file transfer progress.
		/// </summary>
		public FtpProgress(double progress, long bytesTransferred, double transferspeed, TimeSpan remainingtime, string localPath, string remotePath, FtpProgress metaProgress) {

			// progress of individual file transfer
			Progress = progress;
			TransferSpeed = transferspeed;
			ETA = remainingtime;
			LocalPath = localPath;
			RemotePath = remotePath;
			TransferredBytes = bytesTransferred;

			// progress of the entire task
			if (metaProgress != null) {
				FileCount = metaProgress.FileCount;
				FileIndex = metaProgress.FileIndex;
			}
		}

		/// <summary>
		/// Convert Transfer Speed (bytes per second) in human readable format
		/// </summary>
		public string TransferSpeedToString() {
			var value = TransferSpeed > 0 ? TransferSpeed / 1024 : 0; //get KB/s

			if (value < 1024) {
				return Math.Round(value, 2).ToString() + " KB/s";
			}
			else {
				value = value / 1024;
				return Math.Round(value, 2).ToString() + " MB/s";
			}
		}


		/// <summary>
		/// Create a new FtpProgress object for a file transfer and calculate the ETA, Percentage and Transfer Speed.
		/// </summary>
		public static FtpProgress Generate(long fileSize, long position, long bytesProcessed, TimeSpan elapsedtime, string localPath, string remotePath, FtpProgress metaProgress) {

			// default values to send
			double progressValue = -1;
			double transferSpeed = 0;
			var estimatedRemaingTime = TimeSpan.Zero;

			// catch any divide-by-zero errors
			try {

				// calculate raw transferSpeed (bytes per second)
				transferSpeed = bytesProcessed / elapsedtime.TotalSeconds;

				// If fileSize < 0 the below computations make no sense 
				if (fileSize > 0) {

					// calculate % based on file length vs file offset
					// send a value between 0-100 indicating percentage complete
					progressValue = (double)bytesProcessed / (double)fileSize * 100;

					//calculate remaining time			
					estimatedRemaingTime = TimeSpan.FromSeconds((fileSize - bytesProcessed) / transferSpeed);
				}
			}
			catch (Exception) {
			}

			// suppress invalid values and send -1 instead
			if (double.IsNaN(progressValue) || double.IsInfinity(progressValue)) {
				progressValue = -1;
			}
			if (double.IsNaN(transferSpeed) || double.IsInfinity(transferSpeed)) {
				transferSpeed = 0;
			}

			var p = new FtpProgress(progressValue, bytesProcessed, transferSpeed, estimatedRemaingTime, localPath, remotePath, metaProgress);
			return p;
		}

	}
}