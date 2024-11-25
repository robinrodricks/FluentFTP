namespace FluentFTP {

	using System;
	using System.Collections.Generic;
	using System.Threading;

	public class FtpMonitorEventArgs : EventArgs {
		public FtpMonitorEventArgs(string[] folderPaths,
										List<FtpListItem> added,
										List<FtpListItem> changed,
										List<FtpListItem> deleted,
										IAsyncFtpClient ftpClient,
										CancellationToken cancellationToken) {
			FolderPaths = folderPaths;
			Added = added;
			Changed = changed;
			Deleted = deleted;
			FtpClient = ftpClient;
			CancellationToken = cancellationToken;
		}

		/// <summary>
		/// Gets the monitored FTP folder path(s)
		/// </summary>
		public string[] FolderPaths { get; }

		/// <summary>
		/// Gets the list items that were added
		/// </summary>
		public List<FtpListItem> Added { get; }

		/// <summary>
		/// Gets the list items that were changed
		/// </summary>
		public List<FtpListItem> Changed { get; }

		/// <summary>
		/// Gets the list items that were deleted
		/// </summary>
		public List<FtpListItem> Deleted { get; }

		/// <summary>
		/// Gets the active FTP client
		/// </summary>
		public IAsyncFtpClient FtpClient { get; }

		/// <summary>
		/// The cancellation token for closing the monitor
		/// </summary>
		public CancellationToken CancellationToken { get; }

		public override string ToString() {
			return $"FolderPaths = \"{string.Join("\",\"", FolderPaths)}\" Added: {Added.Count} Changed: {Changed.Count} Deleted: {Deleted.Count}";
		}
	}
}