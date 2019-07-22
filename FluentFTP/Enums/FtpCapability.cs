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
		NONE = 0b0000_0000_0000_0000,

		/// <summary>
		/// Supports the MLST command
		/// </summary>
		MLSD = 0b0000_0000_0000_0001,

		/// <summary>
		/// Supports the SIZE command
		/// </summary>
		SIZE = 0b0000_0000_0000_0010,

		/// <summary>
		/// Supports the MDTM command
		/// </summary>
		MDTM = 0b0000_0000_0000_0100,

		/// <summary>
		/// Supports download/upload stream resumes
		/// </summary>
		REST = 0b0000_0000_0000_1000,

		/// <summary>
		/// Supports UTF8
		/// </summary>
		UTF8 = 0b0000_0000_0001_0000,

		/// <summary>
		/// PRET Command used in distributed ftp server software DrFTPD
		/// </summary>
		PRET = 0b0000_0000_0010_0000,

		/// <summary>
		/// Server supports the MFMT command for setting the
		/// modified date of an object on the server
		/// </summary>
		MFMT = 0b0000_0000_0100_0000,

		/// <summary>
		/// Server supports the MFCT command for setting the
		/// created date of an object on the server
		/// </summary>
		MFCT = 0b0000_0000_1000_0000,

		/// <summary>
		/// Server supports the MFF command for setting certain facts
		/// about file system objects. If you need this command, it would
		/// probably be handy to query FEAT your self and have a look at
		/// the FtpReply.InfoMessages property to see which facts the server
		/// allows you to modify.
		/// </summary>
		MFF = 0b0000_0001_0000_0000,

		/// <summary>
		/// Server supports the STAT command
		/// </summary>
		STAT = 0b0000_0010_0000_0000,

		/// <summary>
		/// Support for the HASH command
		/// </summary>
		HASH = 0b0000_0100_0000_0000,

		/// <summary>
		/// Support for the non-standard MD5 command
		/// </summary>
		MD5 = 0b0000_1000_0000_0000,

		/// <summary>
		/// Support for the non-standard XMD5 command
		/// </summary>
		XMD5 = 0b0001_0000_0000_0000,

		/// <summary>
		/// Support for the non-standard XCRC command
		/// </summary>
		XCRC = 0b0010_0000_0000_0000,

		/// <summary>
		/// Support for the non-standard XSHA1 command
		/// </summary>
		XSHA1 = 0b0100_0000_0000_0000,

		/// <summary>
		/// Support for the non-standard XSHA256 command
		/// </summary>
		XSHA256 = 0b1000_0000_0000_0000,

		/// <summary>
		/// Support for the non-standard XSHA512 command
		/// </summary>
		XSHA512 = 0b0000_0001_0000_0000_0000_0000,

		/// <summary>
		/// Support for the EPSV file-transfer command
		/// </summary>
		EPSV = 0b0000_0010_0000_0000_0000_0000,

		/// <summary>
		/// Support for the CPSV command
		/// </summary>
		CPSV = 0b0000_0100_0000_0000_0000_0000,

		/// <summary>
		/// Support for the NOOP command
		/// </summary>
		NOOP = 0b0000_1000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the CLNT command
		/// </summary>
		CLNT = 0b0001_0000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the SSCN command
		/// </summary>
		SSCN = 0b0010_0000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the SITE MKDIR server-specific command (ProFTPd)
		/// </summary>
		SITE_MKDIR = 0b0100_0000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the SITE RMDIR server-specific command (ProFTPd)
		/// </summary>
		SITE_RMDIR = 0b1000_0000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the SITE UTIME server-specific command (ProFTPd)
		/// </summary>
		SITE_UTIME = 0b0001_0000_0000_0000_0000_0000_0000,

		/// <summary>
		/// Support for the SITE SYMLINK server-specific command (ProFTPd)
		/// </summary>
		SITE_SYMLINK = 0b0010_0000_0000_0000_0000_0000_0000
	}
}