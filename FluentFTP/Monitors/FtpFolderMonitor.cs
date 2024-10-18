using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {

	/// <summary>
	/// A synchronous FTP folder monitor that monitors a specific remote folder on the FTP server.
	/// It triggers events when files are added or removed.
	/// Internally it polls the remote folder every so often and checks for changed files.
	/// If `WaitTillFileFullyUploaded` is true, then the file is only detected as an added file if the file size is stable.
	/// </summary>
	public class FtpFolderMonitor : BaseFtpMonitor {
		private FtpClient _ftpClient;

		internal Timer _timer;

		/// <summary>
		/// Is the monitoring started?
		/// </summary>
		public bool Active { get; internal set; }

		/// <summary>
		/// Event triggered when files are changed (when the file size changes).
		/// </summary>
		public event EventHandler<FtpFolderMonitorEventArgs> FilesChanged;

		/// <summary>
		/// Event triggered when files are added (if a new file exists, that was not on the server before).
		/// </summary>
		public event EventHandler<FtpFolderMonitorEventArgs> FilesAdded;

		/// <summary>
		/// Event triggered when files are deleted (if a file is missing, which existed on the server before)
		/// </summary>
		public event EventHandler<FtpFolderMonitorEventArgs> FilesDeleted;

		/// <summary>
		/// Event triggered when any change is detected
		/// </summary>
		public event EventHandler<FtpFolderMonitorEventArgs> ChangeDetected;

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
		/// This FTP client would then be owned and controlled by this class.
		/// </summary>

		public FtpFolderMonitor(FtpClient ftpClient, string folderPath) {
			_ftpClient = ftpClient;
			FolderPath = folderPath;
		}

		/// <summary>
		/// Starts monitoring the FTP folder
		/// </summary>
		public void Start() {
			if (!Active) {
				if (!_ftpClient.IsConnected) {
					_ftpClient.Connect();
				}
				StartTimer(PollFolder);
				Active = true;
			}
		}

		/// <summary>
		/// Stops monitoring the FTP folder
		/// </summary>
		public void Stop() {
			if (Active) {
				StopTimer();
				_ftpClient.Disconnect();
				Active = false;
			}
		}

		/// <summary>
		/// Polls the FTP folder for changes
		/// </summary>
		private void PollFolder(object state) {
			try {
				// exit if not connected
				if (!_ftpClient.IsConnected) {
					return;
				}

				// stop the timer
				StopTimer();

				// Step 1: Get the current listing
				var currentListing = GetCurrentListing();

				// Step 2: Handle unstable files if WaitTillFileFullyUploaded is true
				if (WaitTillFileFullyUploaded) {
					currentListing = HandleUnstableFiles(currentListing);
				}

				// Step 3: Compare current listing to last listing
				var filesAdded = new List<string>();
				var filesChanged = new List<string>();

				foreach (var file in currentListing) {
					if (!_lastListing.TryGetValue(file.Key, out long lastSize)) {
						filesAdded.Add(file.Key);
					}
					else if (lastSize != file.Value) {
						filesChanged.Add(file.Key);
					}
				}

				var filesDeleted = _lastListing.Keys.Except(currentListing.Keys).ToList();

				// Trigger events
				if (filesAdded.Count > 0) FilesAdded?.Invoke(this, new FtpFolderMonitorEventArgs { Files = filesAdded });
				if (filesChanged.Count > 0) FilesChanged?.Invoke(this, new FtpFolderMonitorEventArgs { Files = filesChanged });
				if (filesDeleted.Count > 0) FilesDeleted?.Invoke(this, new FtpFolderMonitorEventArgs { Files = filesDeleted });

				if (filesAdded.Count > 0 || filesChanged.Count > 0 || filesDeleted.Count > 0) {
					ChangeDetected?.Invoke(this, new FtpFolderMonitorEventArgs { Files = filesAdded.Concat(filesChanged).Concat(filesDeleted).ToList() });
				}

				// Step 4: Update last listing
				_lastListing = currentListing;
			}
			catch (Exception ex) {
				// Log the exception or handle it as needed
				Console.WriteLine($"Error polling FTP folder: {ex.Message}");
			}

			// restart the timer
			StartTimer(PollFolder);
		}

		/// <summary>
		/// Gets the current listing of files from the FTP server
		/// </summary>
		private Dictionary<string, long> GetCurrentListing() {
			FtpListOption options = GetListingOptions(_ftpClient.Capabilities);

			var files = _ftpClient.GetListing(FolderPath, options);
			return files.Where(f => f.Type == FtpObjectType.File)
						.ToDictionary(f => f.FullName, f => f.Size);
		}

		/// <summary>
		/// Releases the resources used by the FtpFolderMonitor
		/// </summary>
		public void Dispose() {
			StopTimer();
			_ftpClient?.Dispose();
			_ftpClient = null;
		}

		private void StartTimer(TimerCallback callback) {
			var pollInterval = (_unstableFiles.Count > 0 ? UnstablePollInterval : PollInterval);

			_timer = new Timer(callback, null, TimeSpan.Zero, pollInterval);
		}

		private void StopTimer() {
			_timer?.Dispose();
			_timer = null;
		}
	}
}

public class FtpFolderMonitorEventArgs : EventArgs {
	public List<string> Files { get; set; }
}