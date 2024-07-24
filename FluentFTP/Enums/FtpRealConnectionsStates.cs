using System;

namespace FluentFTP {
	/// <summary>
	/// Real transitional connection states
	/// </summary>
	[Flags]
	public enum FtpRealConnectionStates {
		/// <summary>
		/// Deamon will determine the state
		/// </summary>
		Unknown,

		/// <summary>
		/// Not good state and it will be brought down, closed, disposed
		/// </summary>
		PendingDown,

		/// <summary>
		/// Closed, disposed
		/// </summary>
		Down,

		/// <summary>
		/// Connected, at least the last time the NOOP daemon checked the connection, or
		/// the POLL daemon checked the connection, it was ok.
		/// The POLL daemon checks control and data connections, the NOOP daemon only checks
		/// control connections.
		/// </summary>
		Up,

	};

}