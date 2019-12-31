using System;

namespace FluentFTP {
	/// <summary>
	/// Defines the operating system of the FTP server.
	/// </summary>
	public enum FtpOperatingSystem {
		/// <summary>
		/// Unknown operating system
		/// </summary>
		Unknown,

		/// <summary>
		/// Definitely Windows or Windows Server
		/// </summary>
		Windows,

		/// <summary>
		/// Definitely Unix or AIX-based server
		/// </summary>
		Unix,

		/// <summary>
		/// Definitely VMS or OpenVMS server
		/// </summary>
		VMS,

		/// <summary>
		/// Definitely IBM OS/400 server
		/// </summary>
		IBMOS400,

		/// <summary>
		/// Definitely IBM z/OS server
		/// </summary>
		IBMzOS,

		/// <summary>
		/// Definitely SUN OS/Solaris server
		/// </summary>
		SunOS,
	}
}