using System;

namespace FluentFTP {
	/// <summary>
	/// </summary>
	[Flags]
	public enum FtpNavigate:uint {
		/// <summary>
		/// </summary>
		Manual = 0,

		/// <summary>
		/// </summary>
		Auto = 1,

		/// <summary>
		/// </summary>
		SemiAuto = 2,

		/// <summary>
		/// </summary>
		Conditional = 255,

	}
}