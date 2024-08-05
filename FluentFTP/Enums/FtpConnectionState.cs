using System;

namespace FluentFTP {

	/// <summary>
	/// Actual connection state from the FTP client to the FTP server, as determined by the NOOP Deamon.
	/// </summary>
	public enum FtpConnectionState {

		/// <summary>
		/// Unknown state. NOOP Deamon will determine the state in a short while.
		/// </summary>
		Unknown,

		/// <summary>
		/// Not a good state and it will be brought down, closed and disposed soon.
		/// </summary>
		PendingDisconnect,

		/// <summary>
		/// Closed and disposed.
		/// </summary>
		Disconnected,

		/// <summary>
		/// Connected to the FTP server, at least the last time the NOOP daemon checked the connection.
		/// </summary>
		Connected,

	};

}