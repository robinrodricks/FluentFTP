using System;

namespace FluentFTP {
	/// <summary>
	/// Defines the level of the tracing message.  Depending on the framework version this is translated
	/// to an equivalent logging level in System.Diagnostices (if available)
	/// </summary>
	public enum FtpTraceLevel {
		/// <summary>
		/// Used for logging Debug or Verbose level messages
		/// </summary>
		Verbose,

		/// <summary>
		/// Used for logging Informational messages
		/// </summary>
		Info,

		/// <summary>
		/// Used for logging non-fatal or ignorable error messages
		/// </summary>
		Warn,

		/// <summary>
		/// Used for logging Error messages that may need investigation 
		/// </summary>
		Error
	}
}