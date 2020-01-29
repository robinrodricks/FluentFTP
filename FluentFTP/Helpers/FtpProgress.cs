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
		/// Stores the absolute remote path of the the current file being transfered.
		/// </summary>
		public string RemotePath { get; set; }

		/// <summary>
		/// Stores the absolute local path of the the current file being transfered.
		/// </summary>
		public string LocalPath { get; set; }

		/// <summary>
		/// Stores the index of the the file in the listing.
		/// Only used when transfering multiple files or an entire directory.
		/// </summary>
		public int FileIndex { get; set; }

		/// <summary>
		/// Stores the total count of the files to be transfered.
		/// Only used when transfering multiple files or an entire directory.
		/// </summary>
		public int FileCount { get; set; }

		/// <summary>
		/// Contructor for the class
		/// </summary>
		public FtpProgress(double progress, double transferspeed, TimeSpan remainingtime) {
			Progress = progress;
			TransferSpeed = transferspeed;
			ETA = remainingtime;
		}

		/// <summary>
		/// Convert Transfer Speed (bytes per second) in human readable format
		/// </summary>
		public string TransferSpeedToString() {
			var value = TransferSpeed > 0 ? TransferSpeed / 1024 : 0; //get KB/s

			if (value < 1024) {
				return string.Format("{0} KB/s", Math.Round(value, 2));
			}
			else {
				value = value / 1024;
				return string.Format("{0} MB/s", Math.Round(value, 2));
			}
		}
	}
}