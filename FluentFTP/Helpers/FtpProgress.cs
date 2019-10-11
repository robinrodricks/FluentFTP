using System;

namespace FluentFTP {
	/// <summary>
	/// Class to report FTP file transfer progress during upload or download of files
	/// </summary>
	public class FtpProgress {
		/// <summary>
		/// A value between 0-100 indicating percentage complete, or -1 for indeterminate.
		/// </summary>
		public double Progress { get; set; }

		/// <summary>
		/// A value representing the current Transfer Speed in Bytes per seconds
		/// </summary>
		public double TransferSpeed { get; set; }

		/// <summary>
		/// A value representing the calculated 'Estimated time of arrival'
		/// </summary>
		public TimeSpan ETA { get; set; }

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