using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {

	/// <summary>
	/// An async FTP folder monitor that monitors specified remote folder(s) on the FTP server.
	/// It triggers events when list items are added, changed or removed.
	/// Internally it polls the remote folder(s) every <see cref="M:PollInterval"/> and checks for changed list items.
	/// If `WaitTillFileFullyUploaded` is true, then the list items is only detected as an added when the size is stable.
	/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	// IAsyncDisposable can be used
	public sealed class AsyncFtpMonitor : IDisposable, IAsyncDisposable {
#else
	// IAsyncDisposable is not available
	public sealed class AsyncFtpMonitor : IDisposable {
#endif
		private readonly IAsyncFtpClient _ftpClient;

		private readonly Dictionary<string, long> _unstableListItems = new Dictionary<string, long>();

		private Dictionary<string, FtpListItem> _lastListing = new Dictionary<string, FtpListItem>();

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
		public AsyncFtpMonitor(IAsyncFtpClient ftpClient, params string[] folderPaths) {
			_ftpClient = ftpClient ?? throw new ArgumentNullException(nameof(ftpClient));
			if (folderPaths == null || folderPaths.Length == 0) {
				throw new ArgumentNullException(nameof(folderPaths));
			}
			FolderPaths = folderPaths;
		}

		/// <summary>
		/// Gets the monitored FTP folder path(s)
		/// </summary>
		public string[] FolderPaths { get; }

		/// <summary>
		/// Gets or sets the polling interval. Default is 10 minutes.
		/// </summary>
		public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(10);

		/// <summary>
		/// Gets or sets whether to wait for list items to have stable size before reporting them as added.
		/// </summary>
		public bool WaitTillFileFullyUploaded { get; set; } = true;

		/// <summary>
		/// Gets or sets the polling interval to check for stable list items sizes
		/// when <see cref="P:WaitTillFileFullyUploaded"/> is <see langword="true"/>.
		/// <see langword="null"/> (default) to use the <see cref="P:PollInterval"/> as the unstable poll interval.
		/// </summary>
		public TimeSpan? UnstablePollInterval { get; set; }

		/// <summary>
		/// Gets or sets the options used when listing the FTP folder
		/// Default is <see cref="F:FluentFTP.FtpListOption.Modify"/> and <see cref="F:FluentFTP.FtpListOption.Size"/>
		/// </summary>
		/// <remarks>Setting this property will reset the change tracking, i.e. all existing list items are assumed added</remarks>
		/// <example><code lang="cs">
		/// monitor.Options |= FtpListOption.Recursive;
		/// </code></example>
		public FtpListOption Options {
			get => _options;
			set {
				_options = value;
				_lastListing.Clear();
				_unstableListItems.Clear();
			}
		}

		/// <summary>
		/// Sets the handler that is called when changes are detected in the monitored folder(s)
		/// </summary>
		/// <param name="handler">The handler to call</param>	
		public void SetHandler(Func<AsyncFtpMonitor, AsyncFtpMonitorEventArgs, Task> handler) => _handler = handler;

		/// <summary>
		/// Monitor the FTP folder(s) until the token is cancelled
		/// or an exception occurs in the FtpClient or the handler
		/// </summary>
		public async Task Start(CancellationToken token) {
			while (true) {
				try {
					var startTimeUtc = DateTime.UtcNow;

					await PollFolder(token).ConfigureAwait(false);

					var pollInterval = _unstableListItems.Count > 0 && UnstablePollInterval != null ? UnstablePollInterval.Value : PollInterval;
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
		public override string ToString() {
			return $"FolderPaths = \"{string.Join("\",\"", FolderPaths)}\" PollInterval = {PollInterval} WaitTillFileFullyUploaded = {WaitTillFileFullyUploaded}";
		}

		/// <summary>
		/// Polls the FTP folder(s) for changes
		/// </summary>
		private async Task PollFolder(CancellationToken token) {
			// Step 1: Get the current listing
			var currentListing = await GetCurrentListing(token).ConfigureAwait(false);

			// Step 2: Handle unstable list items if WaitTillFileFullyUploaded is true
			if (WaitTillFileFullyUploaded) {
				currentListing = StableListItems(currentListing);
			}

			// Step 3: Compare current listing to last listing
			var changes = ListItemStatus(currentListing, _lastListing);

			// Step 4: Update last listing
			_lastListing = currentListing;

			if (changes.Added.Count == 0 && changes.Changed.Count == 0 && changes.Deleted.Count == 0) {
				return;
			}

			// Step 5: Raise event
			var handler = _handler;
			if (handler == null) {
				return;
			}

			try {
				var args = new AsyncFtpMonitorEventArgs(FolderPaths, changes.Added, changes.Changed, changes.Deleted, _ftpClient, token);
				await handler(this, args).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
				when (token.IsCancellationRequested) {
			}
		}

		private static ListItemChanges ListItemStatus(Dictionary<string, FtpListItem> currentListing,
		                                              Dictionary<string, FtpListItem> lastListing)
		{
			var listItemsAdded = new List<FtpListItem>();
			var listItemsChanged = new List<FtpListItem>();

			foreach (var listItem in currentListing) {
				if (!lastListing.TryGetValue(listItem.Key, out var lastItem)) {
					listItemsAdded.Add(listItem.Value);
				}
				else if (lastItem.Size != listItem.Value.Size || lastItem.Modified != listItem.Value.Modified) {
					listItemsChanged.Add(listItem.Value);
				}
			}

			var listItemsDeleted = lastListing.Where(x => !currentListing.ContainsKey(x.Key))
			                                  .Select(x => x.Value)
			                                  .ToList();

			return new ListItemChanges(added: listItemsAdded, changed: listItemsChanged, deleted: listItemsDeleted);
		}

		private Dictionary<string, FtpListItem> StableListItems(Dictionary<string, FtpListItem> currentListing) {
			var stableListItems = new Dictionary<string, FtpListItem>();

			foreach (var listItem in currentListing) {
				if (_unstableListItems.TryGetValue(listItem.Key, out long previousSize)) {
					if (previousSize == listItem.Value.Size) {
						// Size has not changed, add to stable
						stableListItems.Add(listItem.Key, listItem.Value);
						_unstableListItems.Remove(listItem.Key);
					}
					else {
						// Size is still changing, update unstable
						_unstableListItems[listItem.Key] = listItem.Value.Size;
					}
				}
				else if (!_lastListing.ContainsKey(listItem.Key)) {
					// New listItem, add to unstable
					_unstableListItems.Add(listItem.Key, listItem.Value.Size);
				}
				else {
					// Existing unchanged list item, add to stable
					stableListItems.Add(listItem.Key, listItem.Value);
				}
			}

			// Remove any unstable that are no longer present
			var missingListItems = _unstableListItems.Keys.Except(currentListing.Keys).ToList();
			foreach (var listItem in missingListItems) {
				_unstableListItems.Remove(listItem);
			}

			return stableListItems;
		}

		/// <summary>
		/// Gets the current list items from the FTP server
		/// </summary>
		private async Task<Dictionary<string, FtpListItem>> GetCurrentListing(CancellationToken token) {
			FtpListOption options = Options;

			if (_ftpClient.Capabilities.Contains(FtpCapability.STAT)) {
				options |= FtpListOption.UseStat;
			}

			var listItems = new Dictionary<string, FtpListItem>();
			foreach (var folderPath in FolderPaths) {
				var folderListItems = await _ftpClient.GetListing(folderPath, options, token).ConfigureAwait(false);
				foreach (var folderListItem in folderListItems) {
					listItems[folderListItem.FullName] = folderListItem;
				}
			}

			return listItems;
		}

		// Tuples are not supported in oldest dotnet version supported
		private readonly struct ListItemChanges {
			public ListItemChanges(List<FtpListItem> added, List<FtpListItem> changed, List<FtpListItem> deleted) {
				Added = added;
				Changed = changed;
				Deleted = deleted;
			}

			public List<FtpListItem> Added { get; }

			public List<FtpListItem> Changed { get; }

			public List<FtpListItem> Deleted { get; }
		}
	}
}
