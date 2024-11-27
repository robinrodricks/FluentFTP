using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {

	/// <summary>
	/// An async FTP folder monitor that monitors specific remote folders on the FTP server.
	/// It triggers the `ChangeDetected` event when files are added, changed or removed.
	/// Internally it polls the remote folder(s) every `PollInterval` and checks for changed files.
	/// If `WaitForUpload` is true, then the file is only detected as an added file if the file size is stable.
	/// </summary>
	public class AsyncFtpMonitor : BaseFtpMonitor {
		private AsyncFtpClient _ftpClient;

		/// <summary>
		/// Event triggered when any change is detected
		/// </summary>
		public event EventHandler<FtpMonitorEventArgs> ChangeDetected;

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
		/// This FTP client would then be owned and controlled by this class.
		/// </summary>
		public AsyncFtpMonitor(AsyncFtpClient ftpClient, List<string> folderPaths) {
			_ftpClient = ftpClient;
			FolderPaths = folderPaths;
		}

		/// <summary>
		/// Starts monitoring the FTP folder
		/// </summary>
		public async Task Start() {
			if (!Active) {
				if (!_ftpClient.IsConnected) {
					await _ftpClient.Connect();
				}
				StartTimer(PollFolder);
				Active = true;
			}
		}

		/// <summary>
		/// Stops monitoring the FTP folder
		/// </summary>
		public async Task Stop() {
			if (Active) {
				StopTimer();
				await _ftpClient.Disconnect();
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

				// Step 2: Handle unstable files if WaitForUpload is true
				if (WaitForUpload) {
					currentListing = HandleUnstableFiles(currentListing);
				}

				// Step 3: Compare current listing to last listing
				var added = new List<string>();
				var changed = new List<string>();
				foreach (var file in currentListing) {
					if (!_lastListing.TryGetValue(file.Key, out long lastSize)) {
						added.Add(file.Key);
					}
					else if (lastSize != file.Value) {
						changed.Add(file.Key);
					}
				}
				var deleted = _lastListing.Keys.Except(currentListing.Keys).ToList();

				// Trigger event
				if (added.Count > 0 || changed.Count > 0 || deleted.Count > 0) {
					var args = new FtpMonitorEventArgs(added, changed, deleted, _ftpClient);
					ChangeDetected?.Invoke(this, args);
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
		private async Task<Dictionary<string, long>> GetCurrentListing() {
			FtpListOption options = GetListingOptions(_ftpClient.Capabilities);

			// per folder to check
			var allItems = new Dictionary<string, long>();
			foreach (var folderPath in FolderPaths) {

				// get listing
				var items = await _ftpClient.GetListing(folderPath, options);
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
