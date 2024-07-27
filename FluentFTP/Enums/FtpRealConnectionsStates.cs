using System;

namespace FluentFTP {
	/// <summary>
	/// Real transitional connection states
	/// </summary>
	public enum FtpRealConnectionStates {
		/// <summary>
		/// Deamon will determine the state in a short while
		/// </summary>
		Unknown,

		/// <summary>
		/// Not a good state and it will be brought down, closed and disposed soon
		/// </summary>
		PendingDown,

		/// <summary>
		/// Closed, disposed
		/// </summary>
		Down,

		/// <summary>
		/// Connected, at least the last time the NOOP daemon checked the connection
		/// </summary>
		Up,

	};

}