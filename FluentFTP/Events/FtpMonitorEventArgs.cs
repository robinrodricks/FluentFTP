namespace FluentFTP {

	using System;
	using System.Collections.Generic;
	using System.Threading;

	public class FtpMonitorEventArgs : EventArgs {
		public FtpMonitorEventArgs(List<string> added,
										List<string> changed,
										List<string> deleted,
										AsyncFtpClient asyncFtpClient = null,
										FtpClient ftpClient = null,
										CancellationToken cancellationToken = default) {
			Added = added;
			Changed = changed;
			Deleted = deleted;
			AsyncFtpClient = asyncFtpClient;
			FtpClient = ftpClient;
			CancellationToken = cancellationToken;
		}

		/// <summary>
		/// Gets the files that are added (newly added files that did not exist at the last check).
		/// </summary>
		public List<string> Added { get; }

		/// <summary>
		/// Gets the files that are changed (when the file size changes).
		/// </summary>
		public List<string> Changed { get; }

		/// <summary>
		/// Gets the files that were deleted (if a file is missing, which existed on the server before)
		/// </summary>
		public List<string> Deleted { get; }

		/// <summary>
		/// Gets the active FTP client when using the async monitor.
		/// </summary>
		public AsyncFtpClient AsyncFtpClient { get; }

		/// <summary>
		/// Gets the active FTP client when using the synchronous monitor.
		/// </summary>
		public FtpClient FtpClient { get; }

		/// <summary>
		/// The cancellation token for closing the monitor (async monitor only).
		/// </summary>
		public CancellationToken CancellationToken { get; }

		public override string ToString() {
			return $"Added: {Added.Count} Changed: {Changed.Count} Deleted: {Deleted.Count}";
		}
	}
}