using System;

namespace FluentFTP {
	/// <summary>
	/// Type of data transfer to do
	/// </summary>
	public enum FtpDataType {
		/// <summary>
		/// ASCII transfer
		/// </summary>
		ASCII,

		/// <summary>
		/// Binary transfer
		/// </summary>
		Binary,

		/// <summary>
		/// Not known yet
		/// </summary>
		Unknown

	}
}