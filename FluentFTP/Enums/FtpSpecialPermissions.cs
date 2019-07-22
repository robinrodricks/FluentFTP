using System;

namespace FluentFTP {
	/// <summary>
	/// Types of special UNIX permissions
	/// </summary>
	[Flags]
	public enum FtpSpecialPermissions : int {
		/// <summary>
		/// No special permissions are set
		/// </summary>
		None = 0,

		/// <summary>
		/// Sticky bit is set
		/// </summary>
		Sticky = 1,

		/// <summary>
		/// SGID bit is set
		/// </summary>
		SetGroupID = 2,

		/// <summary>
		/// SUID bit is set
		/// </summary>
		SetUserID = 4
	}
}