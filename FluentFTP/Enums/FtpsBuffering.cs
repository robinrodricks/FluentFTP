using System;

namespace FluentFTP {
	/// <summary>
	/// Determines how SSL Buffering is handled
	/// </summary>
	public enum FtpsBuffering {
		/// <summary>
		/// Enables SSL Buffering to massively speed up FTPS operations except when:
		/// Under .NET 5.0 and later due to platform issues (see issue 682 in FluentFTP issue tracker).
		/// On the control connection
		/// For proxy connections
		/// If NOOPs are configured to be used
		/// </summary>
		Auto,

		/// <summary>
		/// Always disables SSL Buffering to reduce FTPS connectivity issues.
		/// </summary>
		Off,

		/// <summary>
		/// Same as "Auto"
		/// </summary>
		On
	}
}