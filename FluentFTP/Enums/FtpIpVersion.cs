using System;

namespace FluentFTP {
	/// <summary>
	/// IP Versions to allow when connecting
	/// to a server.
	/// </summary>
	[Flags]
	public enum FtpIpVersion : int {

		/// <summary>
		/// Unknown protocol.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Internet Protocol Version 4
		/// </summary>
		IPv4 = 1,

		/// <summary>
		/// Internet Protocol Version 6
		/// </summary>
		IPv6 = 2,

		/// <summary>
		/// Allow any supported version
		/// </summary>
		ANY = IPv4 | IPv6
	}
}