using System;

namespace FluentFTP {
	/// <summary>
	/// Determines how SSL Buffering is handled
	/// </summary>
	public enum FtpsBuffering {
		/// <summary>
		/// Enables buffering in all cases except when using FTP proxies.
		/// </summary>
		Auto,

		/// <summary>
		/// Always disables SSL Buffering to reduce FTPS connectivity issues.
		/// </summary>
		Off,

		/// <summary>
		/// Always enables SSL Buffering to massively speed up FTPS operations.
		/// </summary>
		On
	}
}