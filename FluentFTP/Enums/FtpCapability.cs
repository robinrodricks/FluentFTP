using System;

namespace FluentFTP {

	/// <summary>
	/// Server features
	/// </summary>
	[Flags]
	public enum FtpCapability : int {
		/// <summary>
		/// This server said it doesn't support anything!
		/// </summary>
		NONE = 0,
		/// <summary>
		/// Supports the MLST command
		/// </summary>
		MLSD = 1,
		/// <summary>
		/// Supports the SIZE command
		/// </summary>
		SIZE = 2,
		/// <summary>
		/// Supports the MDTM command
		/// </summary>
		MDTM = 4,
		/// <summary>
		/// Supports download/upload stream resumes
		/// </summary>
		REST = 8,
		/// <summary>
		/// Supports UTF8
		/// </summary>
		UTF8 = 16,
		/// <summary>
		/// PRET Command used in distributed ftp server software DrFTPD
		/// </summary>
		PRET = 32,
		/// <summary>
		/// Server supports the MFMT command for setting the
		/// modified date of an object on the server
		/// </summary>
		MFMT = 64,
		/// <summary>
		/// Server supports the MFCT command for setting the
		/// created date of an object on the server
		/// </summary>
		MFCT = 128,
		/// <summary>
		/// Server supports the MFF command for setting certain facts
		/// about file system objects. If you need this command, it would
		/// probably be handy to query FEAT your self and have a look at
		/// the FtpReply.InfoMessages property to see which facts the server
		/// allows you to modify.
		/// </summary>
		MFF = 256,
		/// <summary>
		/// Server supports the STAT command
		/// </summary>
		STAT = 512,
		/// <summary>
		/// Support for the HASH command
		/// </summary>
		HASH = 1024,
		/// <summary>
		/// Support for the non-standard MD5 command
		/// </summary>
		MD5 = 2048,
		/// <summary>
		/// Support for the non-standard XMD5 command
		/// </summary>
		XMD5 = 4096,
		/// <summary>
		/// Support for the non-standard XCRC command
		/// </summary>
		XCRC = 8192,
		/// <summary>
		/// Support for the non-standard XSHA1 command
		/// </summary>
		XSHA1 = 16384,
		/// <summary>
		/// Support for the non-standard XSHA256 command
		/// </summary>
		XSHA256 = 32768,
		/// <summary>
		/// Support for the non-standard XSHA512 command
		/// </summary>
		XSHA512 = 65536
	}

}