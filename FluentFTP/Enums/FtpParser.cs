using System;

namespace FluentFTP {
	/// <summary>
	/// The type of response the server responded with
	/// </summary>
	public enum FtpParser : int {
		/// <summary>
		/// Use the custom parser that you have set on the FtpClient object (ListingCustomParser property)
		/// </summary>
		Custom = -1,

		/// <summary>
		/// Automatically detect the file listing parser to use based on the FTP server (SYST command).
		/// </summary>
		Auto = 0,

		/// <summary>
		/// Machine listing parser, works on any FTP server supporting the MLST/MLSD commands.
		/// </summary>
		Machine = 1,

		/// <summary>
		/// File listing parser for Windows/IIS.
		/// </summary>
		Windows = 2,

		/// <summary>
		/// File listing parser for Unix.
		/// </summary>
		Unix = 3,

		/// <summary>
		/// Alternate parser for Unix. Use this if the default one does not work.
		/// </summary>
		UnixAlt = 4,

		/// <summary>
		/// File listing parser for Vax/VMS/OpenVMS.
		/// </summary>
		VMS = 5,

		/// <summary>
		/// File listing parser for IBM z/OS
		/// </summary>
		IBMzOS = 6,

		/// <summary>
		/// File listing parser for IBM OS/400.
		/// </summary>
		IBMOS400 = 7,

		/// <summary>
		/// File listing parser for Tandem/Nonstop Guardian OS.
		/// </summary>
		NonStop = 8
	}
}
