using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using Timer = System.Threading.Timer;

namespace FluentFTP.Monitors {
	public abstract class BaseFtpMonitor {

		internal Dictionary<string, long> _lastListing = new Dictionary<string, long>();
		internal Dictionary<string, long> _unstableFiles = new Dictionary<string, long>();

		internal Timer _timer;

		/// <summary>
		/// Is the monitoring started?
		/// </summary>
		public bool Active { get; internal set; }

		/// <summary>
		/// Gets the monitored FTP folder path
		/// </summary>
		public string FolderPath { get; internal set; }

		/// <summary>
		/// Gets or sets the polling interval in seconds
		/// </summary>
		public int PollIntervalSeconds { get; set; } = 60;

		/// <summary>
		/// Gets or sets whether to wait for files to be fully uploaded before reporting
		/// </summary>
		public bool WaitTillFileFullyUploaded { get; set; } = false;

		/// <summary>
		/// Gets or sets whether to recursively monitor subfolders
		/// </summary>
		public bool Recursive { get; set; } = false;


		internal void StartTimer(TimerCallback callback) {
			_timer = new Timer(callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(PollIntervalSeconds));
		}

		internal void StopTimer() {
			_timer?.Dispose();
			_timer = null;
		}
		internal FtpListOption GetListingOptions(List<FtpCapability> caps) {
			FtpListOption options = FtpListOption.Modify | FtpListOption.Size;

			if (Recursive) {
				options |= FtpListOption.Recursive;
			}

			if (caps.Contains(FtpCapability.MLST)) {
				//options |= FtpListOption.;
			}
			else if (caps.Contains(FtpCapability.STAT)) {
				options |= FtpListOption.UseStat;
			}

			return options;
		}

		/// <summary>
		/// Handles unstable files when WaitTillFileFullyUploaded is true
		/// </summary>
		internal Dictionary<string, long> HandleUnstableFiles(Dictionary<string, long> currentListing) {
			var stableFiles = new Dictionary<string, long>();

			foreach (var file in currentListing) {
				if (_unstableFiles.TryGetValue(file.Key, out long previousSize)) {
					if (previousSize == file.Value) {
						// File size is stable, move to stable files
						stableFiles[file.Key] = file.Value;
						_unstableFiles.Remove(file.Key);
					}
					else {
						// File size is still changing, update unstable files
						_unstableFiles[file.Key] = file.Value;
					}
				}
				else if (!_lastListing.ContainsKey(file.Key)) {
					// New file, add to unstable files
					_unstableFiles[file.Key] = file.Value;
				}
				else {
					// Existing file, add to stable files
					stableFiles[file.Key] = file.Value;
				}
			}

			// Remove any unstable files that are no longer present
			var missingFiles = _unstableFiles.Keys.Except(currentListing.Keys).ToList();
			foreach (var file in missingFiles) {
				_unstableFiles.Remove(file);
			}

			return stableFiles;
		}


	}
}
