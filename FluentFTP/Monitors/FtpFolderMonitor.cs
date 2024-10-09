using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace FluentFTP.Monitors {
	public class FtpFolderMonitor : IDisposable {
		private FtpClient _ftpClient;
		private Timer _timer;

		private Dictionary<string, long> _lastListing = new Dictionary<string, long>();
		private Dictionary<string, long> _unstableFiles = new Dictionary<string, long>();

		/// <summary>
		/// Is the monitoring started?
		/// </summary>
		public bool Active { get; private set; }

		/// <summary>
		/// Gets the monitored FTP folder path
		/// </summary>
		public string FolderPath { get; }

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

		/// <summary>
		/// Event triggered when files are changed
		/// </summary>
		public event EventHandler<List<string>> FilesChanged;

		/// <summary>
		/// Event triggered when files are added
		/// </summary>
		public event EventHandler<List<string>> FilesAdded;

		/// <summary>
		/// Event triggered when files are deleted
		/// </summary>
		public event EventHandler<List<string>> FilesDeleted;

		/// <summary>
		/// Event triggered when any change is detected
		/// </summary>
		public event EventHandler<EventArgs> ChangeDetected;

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
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
				_timer = new Timer(PollFolder, null, TimeSpan.Zero, TimeSpan.FromSeconds(PollIntervalSeconds));
				Active = true;
			}
		}

		/// <summary>
		/// Stops monitoring the FTP folder
		/// </summary>
		public void Stop() {
			if (Active) {
				_timer?.Dispose();
				_timer = null;
				_ftpClient.Disconnect();
				Active = false;
			}
		}

		/// <summary>
		/// Polls the FTP folder for changes
		/// </summary>
		private async void PollFolder(object state) {
			try {
				// exit if not connected
				if (!_ftpClient.IsConnected) {
					return;
				}

				// Step 1: Get the current listing
				var currentListing = await GetCurrentListing();

				// Step 2: Handle unstable files if WaitTillFileFullyUploaded is true
				if (WaitTillFileFullyUploaded) {
					currentListing = HandleUnstableFiles(currentListing);
				}

				// Step 3: Compare current listing to last listing
				var filesAdded = new List<string>();
				var filesChanged = new List<string>();
				var filesDeleted = new List<string>();

				foreach (var file in currentListing) {
					if (!_lastListing.TryGetValue(file.Key, out long lastSize)) {
						filesAdded.Add(file.Key);
					}
					else if (lastSize != file.Value) {
						filesChanged.Add(file.Key);
					}
				}

				filesDeleted = _lastListing.Keys.Except(currentListing.Keys).ToList();

				// Trigger events
				if (filesAdded.Count > 0) FilesAdded?.Invoke(this, filesAdded);
				if (filesChanged.Count > 0) FilesChanged?.Invoke(this, filesChanged);
				if (filesDeleted.Count > 0) FilesDeleted?.Invoke(this, filesDeleted);

				if (filesAdded.Count > 0 || filesChanged.Count > 0 || filesDeleted.Count > 0) {
					ChangeDetected?.Invoke(this, EventArgs.Empty);
				}

				// Step 4: Update last listing
				_lastListing = currentListing;
			}
			catch (Exception ex) {
				// Log the exception or handle it as needed
				Console.WriteLine($"Error polling FTP folder: {ex.Message}");
			}
		}

		/// <summary>
		/// Gets the current listing of files from the FTP server
		/// </summary>
		private async Task<Dictionary<string, long>> GetCurrentListing() {
			FtpListOption options = FtpListOption.Modify | FtpListOption.Size;

			if (Recursive) {
				options |= FtpListOption.Recursive;
			}

			if (_ftpClient.Capabilities.Contains(FtpCapability.MLST)) {
				//options |= FtpListOption.;
			}
			else if (_ftpClient.Capabilities.Contains(FtpCapability.STAT)) {
				options |= FtpListOption.UseStat;
			}

			var files = await _ftpClient.GetListingAsync(FolderPath, options);
			return files.Where(f => f.Type == FtpObjectType.File)
						.ToDictionary(f => f.FullName, f => f.Size);
		}

		/// <summary>
		/// Handles unstable files when WaitTillFileFullyUploaded is true
		/// </summary>
		private Dictionary<string, long> HandleUnstableFiles(Dictionary<string, long> currentListing) {
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

		/// <summary>
		/// Releases the resources used by the FtpFolderMonitor
		/// </summary>
		public void Dispose() {
			_timer?.Dispose();
			_ftpClient?.Dispose();
		}
	}
}
