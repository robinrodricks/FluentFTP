using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {
	using System.Diagnostics;

	/// <summary>
	/// An async FTP folder monitor that monitors a specific remote folder on the FTP server.
	/// It triggers events when files are added or removed.
	/// Internally it polls the remote folder every so often and checks for changed files.
	/// If `WaitTillFileFullyUploaded` is true, then the file is only detected as an added file if the file size and modify time is stable.
	/// </summary>
	[DebuggerDisplay("FolderPath = {FolderPath} PollInterval = {PollInterval} WaitTillFileFullyUploaded = {WaitTillFileFullyUploaded}")]
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	// IAsyncDisposable can be used
	public sealed class AsyncFtpMonitor : IDisposable, IAsyncDisposable {
#else
	// IAsyncDisposable is not available
	public sealed class AsyncFtpMonitor : IDisposable {
#endif
		private readonly IAsyncFtpClient _ftpClient;

		internal Dictionary<string, FtpListItem> _lastListing = new Dictionary<string, FtpListItem>();

		internal Dictionary<string, long> _unstableFiles = new Dictionary<string, long>();

		// the handler can not be exposed as a public event because it is async
		// the handler can not be exposed as a public property because it would allow multiple handlers (+=)
		// which does not work well with async handlers
		private Func<AsyncFtpMonitor, AsyncFtpMonitorEventArgs, Task> _handler;

		private FtpListOption _options = FtpListOption.Modify | FtpListOption.Size;

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
		/// This FTP client would then be owned and controlled by this class.
		/// The client can be used in the handler to perform FTP operations.
		/// </summary>
		public AsyncFtpMonitor(IAsyncFtpClient ftpClient, string folderPath) {
			_ftpClient = ftpClient ?? throw new ArgumentNullException(nameof(ftpClient));
			FolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
		}

		/// <summary>
		/// Gets the monitored FTP folder path
		/// </summary>
		public string FolderPath { get; }

		/// <summary>
		/// Gets or sets the polling interval in seconds
		/// </summary>
		public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Gets or sets whether to wait for files to be fully uploaded before reporting
		/// </summary>
		public bool WaitTillFileFullyUploaded { get; set; } = true;

		/// <summary>
		/// Gets or sets the polling interval when new files are detected and <see cref="WaitTillFileFullyUploaded"/> is <see langword="true"/>
		/// </summary>
		public TimeSpan? UnstablePollInterval { get; set; }

		/// <summary>
		/// Gets or sets the options used when listing the FTP folder
		/// Default is <see cref="FtpListOption.Modify"/> and <see cref="FtpListOption.Size"/>
		/// </summary>
		/// <remarks>Setting this property will reset the change tracking, i.e. all existing files are assumed added</remarks>
		public FtpListOption Options {
			get => _options;
			set {
				_options = value;
				_lastListing.Clear();
				_unstableFiles.Clear();
			}
		}

		/// <summary>
		/// Sets the handler that is called when changes are detected in the monitored folder
		/// </summary>
		/// <param name="handler">The handler to call</param>	
		public void SetHandler(Func<AsyncFtpMonitor, AsyncFtpMonitorEventArgs, Task> handler) => _handler = handler;

		/// <summary>
		/// Starts monitoring the FTP folder until the token is cancelled or an exception occurs
		/// </summary>
		public async Task Start(CancellationToken token) {
			while (true) {
				try {
					var startTimeUtc = DateTime.UtcNow;

					await PollFolder(token).ConfigureAwait(false);

					var pollInterval = _unstableFiles.Count > 0 && UnstablePollInterval != null ? UnstablePollInterval.Value : PollInterval;
					var waitTime = pollInterval - (DateTime.UtcNow - startTimeUtc);

					if (waitTime > TimeSpan.Zero) {
						await Task.Delay(waitTime, token).ConfigureAwait(false);
					}
					else {
						token.ThrowIfCancellationRequested();
					}
				}
				catch (OperationCanceledException)
					when (token.IsCancellationRequested) {
					break;
				}
			}
		}

		public void Dispose() {
			_ftpClient?.Dispose();
		}

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		public async ValueTask DisposeAsync() {
			if (_ftpClient != null) {
				await _ftpClient.DisposeAsync().ConfigureAwait(false);
			}
		}
#endif

		/// <summary>
		/// Polls the FTP folder for changes
		/// </summary>
		private async Task PollFolder(CancellationToken token) {
			// Step 1: Get the current listing
			var currentListing = await GetCurrentListing(token).ConfigureAwait(false);

			// Step 2: Handle unstable files if WaitTillFileFullyUploaded is true
			if (WaitTillFileFullyUploaded) {
				currentListing = StableItems(currentListing);
			}

			// Step 3: Compare current listing to last listing
			var itemsAdded = new List<FtpListItem>();
			var itemsChanged = new List<FtpListItem>();

			foreach (var file in currentListing) {
				if (!_lastListing.TryGetValue(file.Key, out var lastItem)) {
					itemsAdded.Add(file.Value);
				}
				else if (lastItem.Size != file.Value.Size || lastItem.Modified != file.Value.Modified) {
					itemsChanged.Add(file.Value);
				}
			}

			var itemsDeleted = _lastListing.Where(x => !currentListing.ContainsKey(x.Key))
										   .Select(x => x.Value)
										   .ToList();

			// Step 4: Update last listing
			_lastListing = currentListing;

			if (itemsAdded.Count == 0 && itemsChanged.Count == 0 && itemsDeleted.Count == 0) {
				return;
			}

			// Step 5: Raise event
			var handler = _handler;
			if (handler == null) {
				return;
			}

			try {
				var args = new AsyncFtpMonitorEventArgs(FolderPath, itemsAdded, itemsChanged, itemsDeleted, _ftpClient, token);
				await handler(this, args).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
				when (token.IsCancellationRequested) {
			}
		}

		private Dictionary<string, FtpListItem> StableItems(Dictionary<string, FtpListItem> currentListing) {
			var stableItems = new Dictionary<string, FtpListItem>();

			foreach (var file in currentListing) {
				if (_unstableFiles.TryGetValue(file.Key, out long previousSize)) {
					if (previousSize == file.Value.Size) {
						// File size is stable, move to stable files
						stableItems[file.Key] = file.Value;
						_unstableFiles.Remove(file.Key);
					}
					else {
						// File size is still changing, update unstable files
						_unstableFiles[file.Key] = file.Value.Size;
					}
				}
				else if (!_lastListing.ContainsKey(file.Key)) {
					// New file, add to unstable files
					_unstableFiles[file.Key] = file.Value.Size;
				}
				else {
					// Existing file, add to stable files
					stableItems[file.Key] = file.Value;
				}
			}

			// Remove any unstable files that are no longer present
			var missingFiles = _unstableFiles.Keys.Except(currentListing.Keys).ToList();
			foreach (var file in missingFiles) {
				_unstableFiles.Remove(file);
			}

			return stableItems;
		}

		/// <summary>
		/// Gets the current listing of files from the FTP server
		/// </summary>
		private async Task<Dictionary<string, FtpListItem>> GetCurrentListing(CancellationToken token) {
			FtpListOption options = GetListingOptions(_ftpClient.Capabilities);

			var files = await _ftpClient.GetListing(FolderPath, options, token).ConfigureAwait(false);
			return files.ToDictionary(f => f.FullName);
		}

		private FtpListOption GetListingOptions(List<FtpCapability> caps) {
			FtpListOption options = Options;

			if (caps.Contains(FtpCapability.STAT)) {
				options |= FtpListOption.UseStat;
			}

			return options;
		}
	}
}
