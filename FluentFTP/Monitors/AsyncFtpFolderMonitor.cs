using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Monitors {
	/// <summary>
	/// An async FTP folder monitor that monitors a specific remote folder on the FTP server.
	/// It triggers events when files are added or removed.
	/// Internally it polls the remote folder every so often and checks for changed files.
	/// If `WaitTillFileFullyUploaded` is true, then the file is only detected as an added file if the file size is stable.
	/// </summary>
	public class AsyncFtpFolderMonitor : BaseFtpMonitor {
		private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
		private AsyncFtpClient _ftpClient;

		private readonly List<Func<List<string>, CancellationToken, Task>> _filesChangedHandlers = new List<Func<List<string>, CancellationToken, Task>>();
		private readonly List<Func<List<string>, CancellationToken, Task>> _filesAddedHandlers = new List<Func<List<string>, CancellationToken, Task>>();
		private readonly List<Func<List<string>, CancellationToken, Task>> _filesDeletedHandlers = new List<Func<List<string>, CancellationToken, Task>>();
		private readonly List<Func<List<string>, CancellationToken, Task>> _changeDetectedHandlers = new List<Func<List<string>, CancellationToken, Task>>();

		/// <summary>
		/// Create a new FTP monitor.
		/// Provide a valid FTP client, and then do not use this client for any other purpose.
		/// This FTP client would then be owned and controlled by this class.
		/// </summary>
		public AsyncFtpFolderMonitor(AsyncFtpClient ftpClient, string folderPath) {
			_ftpClient = ftpClient;
			FolderPath = folderPath;
		}

		/// <summary>
		/// Starts monitoring the FTP folder
		/// </summary>
		public async Task StartAsync(CancellationToken ct) {
			while (true) {
				try {
					var startTimeUtc = DateTime.UtcNow;

					await PollFolderAsync(ct).ConfigureAwait(false);

					var pollInterval = _unstableFiles.Count > 0 ? UnstablePollInterval : PollInterval;
					var waitTime = pollInterval - (DateTime.UtcNow - startTimeUtc);

					if (waitTime > TimeSpan.Zero) {
						await Task.Delay(waitTime, ct).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException)
					when (ct.IsCancellationRequested) {
					break;
				}
			}
		}

		/// <summary>
		/// Event triggered when files are changed (when the file size changes).
		/// </summary>
		public void FilesChanged(Func<List<string>, CancellationToken, Task> handler) => AddHandler(_filesChangedHandlers, handler);
		public void FilesChanged(Func<List<string>, Task> handler) => FilesChanged((list, _) => handler(list));
		public void FilesChanged(Action<List<string>> handler) => FilesChanged((list, _) => { handler(list); return Task.CompletedTask; });

		/// <summary>
		/// Event triggered when files are added (if a new file exists, that was not on the server before).
		/// </summary>
		public void FilesAdded(Func<List<string>, CancellationToken, Task> handler) => AddHandler(_filesAddedHandlers, handler);
		public void FilesAdded(Func<List<string>, Task> handler) => FilesAdded((list, _) => handler(list));
		public void FilesAdded(Action<List<string>> handler) => FilesAdded((list, _) => { handler(list); return Task.CompletedTask; });

		/// <summary>
		/// Event triggered when files are deleted (if a file is missing, which existed on the server before)
		/// </summary>
		public void FilesDeleted(Func<List<string>, CancellationToken, Task> handler) => AddHandler(_filesDeletedHandlers, handler);
		public void FilesDeleted(Func<List<string>, Task> handler) => FilesDeleted((list, _) => handler(list));
		public void FilesDeleted(Action<List<string>> handler) => FilesDeleted((list, _) => { handler(list); return Task.CompletedTask; });

		/// <summary>
		/// Event triggered when any change is detected
		/// </summary>
		public void ChangesDetected(Func<List<string>, CancellationToken, Task> handler) => AddHandler(_changeDetectedHandlers, handler);
		public void ChangesDetected(Func<List<string>, Task> handler) => ChangesDetected((list, _) => handler(list));
		public void ChangesDetected(Action<List<string>> handler) => ChangesDetected((list, _) => { handler(list); return Task.CompletedTask; });

		private void AddHandler<T>(List<T> handlers, T handler) {
			_lock.Wait();
			try {
				handlers.Add(handler);
			}
			finally {
				_lock.Release();
			}
		}


		private async Task<List<Exception>> RunHandlers(List<Func<List<string>, CancellationToken, Task>> handlers, List<string> files, CancellationToken ct) {
			await _lock.WaitAsync(ct).ConfigureAwait(false);

			try {
				var exceptions = new List<Exception>();
				foreach (var handler in handlers) {
					try {
						ct.ThrowIfCancellationRequested();
						await handler(files, ct).ConfigureAwait(false);
					}
					catch (OperationCanceledException)
						when (ct.IsCancellationRequested) {
						throw;
					}
					catch (Exception ex) {
						exceptions.Add(ex);
					}
				}

				return exceptions;
			}
			finally {
				_lock.Release();
			}
		}


		/// <summary>
		/// Polls the FTP folder for changes
		/// </summary>
		private async Task PollFolderAsync(CancellationToken token) {
			// Step 1: Get the current listing
			var currentListing = await GetCurrentListing(token).ConfigureAwait(false);

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

			var exceptions = new List<Exception>();
			if (filesAdded.Count > 0 && _filesAddedHandlers.Count > 0) {
				exceptions.AddRange(await RunHandlers(_filesAddedHandlers, filesAdded, token).ConfigureAwait(false));
			}
			if (filesChanged.Count > 0 && _filesChangedHandlers.Count > 0) {
				exceptions.AddRange(await RunHandlers(_filesChangedHandlers, filesChanged, token).ConfigureAwait(false));
			}
			if (filesDeleted.Count > 0 && _filesDeletedHandlers.Count > 0) {
				exceptions.AddRange(await RunHandlers(_filesDeletedHandlers, filesDeleted, token).ConfigureAwait(false));
			}
			if (filesAdded.Count > 0 || filesChanged.Count > 0 || filesDeleted.Count > 0) {
				var allChanges = filesAdded.Concat(filesChanged).Concat(filesDeleted).ToList();
				exceptions.AddRange(await RunHandlers(_changeDetectedHandlers, allChanges, token).ConfigureAwait(false));
			}

			if (exceptions.Count > 0) {
				throw new AggregateException("One or more handlers failed", exceptions);
			}

			// Step 4: Update last listing
			_lastListing = currentListing;
		}

		/// <summary>
		/// Gets the current listing of files from the FTP server
		/// </summary>
		private async Task<Dictionary<string, long>> GetCurrentListing(CancellationToken token) {
			FtpListOption options = GetListingOptions(_ftpClient.Capabilities);

			var files = await _ftpClient.GetListing(FolderPath, options, token).ConfigureAwait(false);
			return files.Where(f => f.Type == FtpObjectType.File)
						.ToDictionary(f => f.FullName, f => f.Size);
		}

		/// <summary>
		/// Releases the resources used by the FtpFolderMonitor
		/// </summary>
		public void Dispose() {
			_ftpClient?.Dispose();
			_ftpClient = null;
		}
	}
}
