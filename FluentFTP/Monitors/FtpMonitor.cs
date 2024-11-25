using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {

	/// <summary>
	/// A synchronous FTP folder monitor that monitors specific remote folders on the FTP server.
	/// It triggers events when files are added or removed.
	/// Internally it polls the remote folder(s) every `PollInterval` and checks for changed files.
	/// If `WaitTillFileFullyUploaded` is true, then the file is only detected as an added file if the file size is stable.
	/// </summary>
	public class FtpMonitor : BaseFtpMonitor, IDisposable {
		private FtpClient _ftpClient;

		/// <summary>
		/// Event triggered when any change is detected
		/// </summary>
		public event EventHandler<FtpMonitorEventArgs> ChangeDetected;

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
		/// This FTP client would then be owned and controlled by this class.
		/// </summary>

		public FtpMonitor(FtpClient ftpClient, List<string> folderPaths) {
			_ftpClient = ftpClient;
			FolderPaths = folderPaths;
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
		private async void PollFolder(object state) {
			try {

				// exit if not connected
				if (!_ftpClient.IsConnected) {
					return;
				}

				// stop the timer
				StopTimer();

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
			finally {
				// restart the timer
				StartTimer(PollFolder);
			}
		}

		/// <summary>
		/// Gets the current listing of files from the FTP server
		/// </summary>
		private async Task<Dictionary<string, long>> GetCurrentListing() {
			FtpListOption options = GetListingOptions(_ftpClient.Capabilities);

			// per folder to check
			var allItems = new Dictionary<string, long>();
			foreach (var folderPath in FolderPaths) {

				// get listing
				var items = _ftpClient.GetListing(folderPath, options);
				foreach (var f in items) {
					if (f.Type == FtpObjectType.File) {

						// combine it into the results
						allItems[f.FullName] = f.Size;
					}
				}
			}

			return allItems;
		}

		/// <summary>
		/// Releases the resources used by the FtpFolderMonitor
		/// </summary>
		public void Dispose() {
			StopTimer();
			_ftpClient?.Dispose();
			_ftpClient = null;
		}
	}
}
