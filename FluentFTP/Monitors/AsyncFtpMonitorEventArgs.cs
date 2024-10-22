namespace FluentFTP.Monitors;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

[DebuggerDisplay("Folder = {Folder} Added = {Added.Count] Changed = {Changed.Count] Deleted = {Deleted.Count]")]
public class AsyncFtpMonitorEventArgs : EventArgs {
	public AsyncFtpMonitorEventArgs(string folder,
	                                List<FtpListItem> added,
	                                List<FtpListItem> changed,
	                                List<FtpListItem> deleted,
	                                IAsyncFtpClient ftpClient,
	                                CancellationToken cancellationToken) {
		Folder = folder;
		Added = added;
		Changed = changed;
		Deleted = deleted;
		FtpClient = ftpClient;
		CancellationToken = cancellationToken;
	}

	/// <summary>
	/// Gets the monitored FTP folder path
	/// </summary>
	public string Folder { get; }

	/// <summary>
	/// Gets the list of files that were added
	/// </summary>
	public List<FtpListItem> Added { get; }

	/// <summary>
	/// Gets the list of files that were changed
	/// </summary>
	public List<FtpListItem> Changed { get; }

	/// <summary>
	/// Gets the list of files that were deleted
	/// </summary>
	public List<FtpListItem> Deleted { get; }

	/// <summary>
	/// Active FTP client
	/// </summary>
	public IAsyncFtpClient FtpClient { get; }

	/// <summary>
	/// Cancellation token
	/// </summary>
	public CancellationToken CancellationToken { get; }
}