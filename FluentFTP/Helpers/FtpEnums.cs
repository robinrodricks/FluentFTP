using System;

namespace FluentFTP {

	/// <summary>
	/// Defines the type of encryption to use
	/// </summary>
	public enum FtpEncryptionMode {
		/// <summary>
		/// Plain text.
		/// </summary>
		None,
		/// <summary>
		/// FTPS encryption is used from the start of the connection, port 990.
		/// </summary>
		Implicit,
		/// <summary>
		/// Connection starts in plain text and FTPS encryption is enabled
		/// with the AUTH command immediately after the server greeting.
		/// </summary>
		Explicit
	}

	/// <summary>
	/// The type of response the server responded with
	/// </summary>
	public enum FtpResponseType : int {
		/// <summary>
		/// No response
		/// </summary>
		None = 0,
		/// <summary>
		/// Success
		/// </summary>
		PositivePreliminary = 1,
		/// <summary>
		/// Success
		/// </summary>
		PositiveCompletion = 2,
		/// <summary>
		/// Success
		/// </summary>
		PositiveIntermediate = 3,
		/// <summary>
		/// Temporary failure
		/// </summary>
		TransientNegativeCompletion = 4,
		/// <summary>
		/// Permanent failure
		/// </summary>
		PermanentNegativeCompletion = 5
	}

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

	/// <summary>
	/// Different types of hashing algorithms for computing checksums.
	/// </summary>
	[Flags]
	public enum FtpHashAlgorithm : int {
		/// <summary>
		/// HASH command is not supported
		/// </summary>
		NONE = 0,
		/// <summary>
		/// SHA-1
		/// </summary>
		SHA1 = 1,
		/// <summary>
		/// SHA-256
		/// </summary>
		SHA256 = 2,
		/// <summary>
		/// SHA-512
		/// </summary>
		SHA512 = 4,
		/// <summary>
		/// MD5
		/// </summary>
		MD5 = 8,
		/// <summary>
		/// CRC
		/// </summary>
		CRC = 16
	}

	/// <summary>
	/// IP Versions to allow when connecting
	/// to a server.
	/// </summary>
	[Flags]
	public enum FtpIpVersion : int {
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

	/// <summary>
	/// Data connection type
	/// </summary>
	public enum FtpDataConnectionType {
		/// <summary>
		/// This type of data connection attempts to use the EPSV command
		/// and if the server does not support EPSV it falls back to the
		/// PASV command before giving up unless you are connected via IPv6
		/// in which case the PASV command is not supported.
		/// </summary>
		AutoPassive,
		/// <summary>
		/// Passive data connection. EPSV is a better
		/// option if it's supported. Passive connections
		/// connect to the IP address dictated by the server
		/// which may or may not be accessible by the client
		/// for example a server behind a NAT device may
		/// give an IP address on its local network that
		/// is inaccessible to the client. Please note that IPv6
		/// does not support this type data connection. If you
		/// ask for PASV and are connected via IPv6 EPSV will
		/// automatically be used in its place.
		/// </summary>
		PASV,
		/// <summary>
		/// Same as PASV except the host supplied by the server is ignored
		/// and the data connection is made to the same address that the control
		/// connection is connected to. This is useful in scenarios where the
		/// server supplies a private/non-routable network address in the
		/// PASV response. It's functionally identical to EPSV except some
		/// servers may not implement the EPSV command. Please note that IPv6
		/// does not support this type data connection. If you
		/// ask for PASV and are connected via IPv6 EPSV will
		/// automatically be used in its place.
		/// </summary>
		PASVEX,
		/// <summary>
		/// Extended passive data connection, recommended. Works
		/// the same as a PASV connection except the server
		/// does not dictate an IP address to connect to, instead
		/// the passive connection goes to the same address used
		/// in the control connection. This type of data connection
		/// supports IPv4 and IPv6.
		/// </summary>
		EPSV,
		/// <summary>
		/// This type of data connection attempts to use the EPRT command
		/// and if the server does not support EPRT it falls back to the
		/// PORT command before giving up unless you are connected via IPv6
		/// in which case the PORT command is not supported.
		/// </summary>
		AutoActive,
		/// <summary>
		/// Active data connection, not recommended unless
		/// you have a specific reason for using this type.
		/// Creates a listening socket on the client which
		/// requires firewall exceptions on the client system
		/// as well as client network when connecting to a
		/// server outside of the client's network. In addition
		/// the IP address of the interface used to connect to the
		/// server is the address the server is told to connect to
		/// which, if behind a NAT device, may be inaccessible to
		/// the server. This type of data connection is not supported
		/// by IPv6. If you specify PORT and are connected via IPv6
		/// EPRT will automatically be used instead.
		/// </summary>
		PORT,
		/// <summary>
		/// Extended active data connection, not recommended
		/// unless you have a specific reason for using this
		/// type. Creates a listening socket on the client
		/// which requires firewall exceptions on the client
		/// as well as client network when connecting to a 
		/// server outside of the client's network. The server
		/// connects to the IP address it sees the client coming
		/// from. This type of data connection supports IPv4 and IPv6.
		/// </summary>
		EPRT
	}

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
		Binary
	}

	/// <summary>
	/// Type of file system of object
	/// </summary>
	public enum FtpFileSystemObjectType {
		/// <summary>
		/// A file
		/// </summary>
		File,
		/// <summary>
		/// A directory
		/// </summary>
		Directory,
		/// <summary>
		/// A symbolic link
		/// </summary>
		Link
	}

	/// <summary>
	/// Types of file permissions
	/// </summary>
	[Flags]
	public enum FtpPermission : uint {
		/// <summary>
		/// No access
		/// </summary>
		None = 0,
		/// <summary>
		/// Executable
		/// </summary>
		Execute = 1,
		/// <summary>
		/// Writable
		/// </summary>
		Write = 2,
		/// <summary>
		/// Readable
		/// </summary>
		Read = 4
	}

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

	/// <summary>
	/// The type of response the server responded with
	/// </summary>
	public enum FtpParser : int {
		/// <summary>
		/// Use the legacy parser (for older projects that depend on the pre-2017 parser routines).
		/// </summary>
		Legacy = -1,
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
		/// File listing parser for IBM OS400.
		/// </summary>
		IBM = 6,
		/// <summary>
		/// File listing parser for Tandem/Nonstop Guardian OS.
		/// </summary>
		NonStop = 7
	}

	/// <summary>
	/// Flags that can dictate how a file listing is performed
	/// </summary>
	[Flags]
	public enum FtpListOption {
		/// <summary>
		/// Tries machine listings (MDTM command) if supported,
		/// and if not then falls back to OS-specific listings (LIST command)
		/// </summary>
		Auto = 0,
		/// <summary>
		/// Load the modify date using MDTM when it could not
		/// be parsed from the server listing. This only pertains
		/// to servers that do not implement the MLSD command.
		/// </summary>
		Modify = 1,
		/// <summary>
		/// Load the file size using the SIZE command when it
		/// could not be parsed from the server listing. This
		/// only pertains to servers that do not support the
		/// MLSD command.
		/// </summary>
		Size = 2,
		/// <summary>
		/// Combines the Modify and Size flags
		/// </summary>
		SizeModify = Modify | Size,
		/// <summary>
		/// Show hidden/dot files. This only pertains to servers
		/// that do not support the MLSD command. This option
		/// makes use the non standard -a parameter to LIST to
		/// tell the server to show hidden files. Since it's a
		/// non-standard option it may not always work. MLSD listings
		/// have no such option and whether or not a hidden file is
		/// shown is at the discretion of the server.
		/// </summary>
		AllFiles = 4,
		/// <summary>
		/// Force the use of OS-specific listings (LIST command) even if
		/// machine listings (MLSD command) are supported by the server
		/// </summary>
		ForceList = 8,
		/// <summary>
		/// Use the NLST command instead of LIST for a reliable file listing
		/// </summary>
		NameList = 16,
		/// <summary>
		/// Force the use of the NLST command (the slowest mode) even if machine listings
		/// and OS-specific listings are supported by the server
		/// </summary>
		ForceNameList = ForceList | NameList,
		/// <summary>
		/// Try to dereference symbolic links, and stored the linked file/directory in FtpListItem.LinkObject
		/// </summary>
		DerefLinks = 32,
		/// <summary>
		/// Sets the ForceList flag and uses `LS' instead of `LIST' as the
		/// command for getting a directory listing. This option overrides
		/// ForceNameList and ignores the AllFiles flag.
		/// </summary>
		UseLS = 64 | ForceList,
		/// <summary>
		/// Gets files within subdirectories as well. Adds the -r option to the LIST command.
		/// Some servers may not support this feature.
		/// </summary>
		Recursive = 128,
		/// <summary>
		/// Do not retrieve path when no path is supplied to GetListing(),
		/// instead just execute LIST with no path argument.
		/// </summary>
		NoPath = 256,
		/// <summary>
		/// Include two extra items into the listing, for the current directory (".")
		/// and the parent directory (".."). Meaningless unless you want these two
		/// items for some reason.
		/// </summary>
		IncludeSelfAndParent = 512
	}

	/// <summary>
	/// Defines the behavior for uploading/downloading files that already exist
	/// </summary>
	public enum FtpExists {
		/// <summary>
		/// Do not check if the file exists. A bit faster than the other options.
		/// Only use this if you are SURE that the file does not exist on the server.
		/// Otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		NoCheck,
		/// <summary>
		/// Skip the file if it exists, without any more checks.
		/// </summary>
		Skip,
		/// <summary>
		/// Overwrite the file if it exists.
		/// </summary>
		Overwrite,
		/// <summary>
		/// Append to the file if it exists, by checking the length and adding the missing data.
		/// </summary>
		Append,
		/// <summary>
		/// Append to the file, but don't check if it exists and add missing data.
		/// This might be required if you don't have permissions on the server to list files in the folder.
		/// Only use this if you are SURE that the file does not exist on the server otherwise it can cause the UploadFile method to hang due to filesize mismatch.
		/// </summary>
		AppendNoCheck
	}

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

	/// <summary>
	/// Defines how multi-file processes should handle a processing error.
	/// </summary>
	/// <remarks><see cref="FtpError.Stop"/> &amp; <see cref="FtpError.Throw"/> Cannot Be Combined</remarks>
	[Flags]
	public enum FtpError {
		/// <summary>
		/// No action is taken upon errors.  The method absorbs the error and continues.
		/// </summary>
		None = 0,
		/// <summary>
		/// If any files have completed successfully (or failed after a partial download/upload) then should be deleted.  
		/// This will simulate an all-or-nothing transaction downloading or uploading multiple files.  If this option is not
		/// combined with <see cref="FtpError.Stop"/> or <see cref="FtpError.Throw"/> then the method will
		/// continue to process all items whether if they are successful or not and then delete everything if a failure was
		/// encountered at any point.
		/// </summary>
		DeleteProcessed = 1,
		/// <summary>
		/// The method should stop processing any additional files and immediately return upon encountering an error.
		/// Cannot be combined with <see cref="FtpError.Throw"/>
		/// </summary>
		Stop = 2,
		/// <summary>
		/// The method should stop processing any additional files and immediately throw the current error.
		/// Cannot be combined with <see cref="FtpError.Stop"/>
		/// </summary>
		Throw = 4,

	}

	/// <summary>
	/// Defines if additional verification and actions upon failure that 
	/// should be performed when uploading/downloading files using the high-level APIs.  Ignored if the 
	/// FTP server does not support any hashing algorithms.
	/// </summary>
	[Flags]
	public enum FtpVerify {
		/// <summary>
		/// No verification of the file is performed
		/// </summary>
		None = 0,
		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then we retry the download/upload
		/// a specified amount of times before giving up. (See <see cref="FtpClient.RetryAttempts"/>)
		/// </summary>
		Retry = 1,
		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then the failed file will be deleted.
		/// If combined with <see cref="FtpVerify.Retry"/>, then
		/// the deletion will occur if it fails upon the final retry.
		/// </summary>
		Delete = 2,
		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then an exception will be thrown.
		/// If combined with <see cref="FtpVerify.Retry"/>, then the throw will
		/// occur upon the failure of the final retry, and/or if combined with <see cref="FtpVerify.Delete"/>
		/// the method will throw after the deletion is processed.
		/// </summary>
		Throw = 4,
		/// <summary>
		/// The checksum of the file is verified, if supported by the server.
		/// If the checksum comparison fails then the method returns false and no other action is taken.
		/// </summary>
		OnlyChecksum = 8,
	}

	/// <summary>
	/// Defines if additional verification and actions upon failure that 
	/// should be performed when uploading/downloading files using the high-level APIs.  Ignored if the 
	/// FTP server does not support any hashing algorithms.
	/// </summary>
	public enum FtpDate {
		/// <summary>
		/// The date is whatever the server returns, with no conversion performed.
		/// </summary>
		Original = 0,
#if !CORE
		/// <summary>
		/// The date is converted to the local timezone, based on the TimeOffset property in FtpClient.
		/// </summary>
		Local = 1,
#endif
		/// <summary>
		/// The date is converted to UTC, based on the TimeOffset property in FtpClient.
		/// </summary>
		UTC = 2,
	}

	/// <summary>
	/// Defines the type of the FTP server software.
	/// Add constants here as you add detection scripts for individual server types.
	/// </summary>
	public enum FtpServer {
		/// <summary>
		/// Unknown FTP server software
		/// </summary>
		Unknown,
		/// <summary>
		/// Definitely PureFTPd server
		/// </summary>
		PureFTPd,
		/// <summary>
		/// Definitely VsFTPd server
		/// </summary>
		VsFTPd,
		/// <summary>
		/// Definitely ProFTPD server
		/// </summary>
		ProFTPD,
		/// <summary>
		/// Definitely FileZilla server
		/// </summary>
		FileZilla,
		/// <summary>
		/// Definitely OpenVMS server
		/// </summary>
		OpenVMS,
		/// <summary>
		/// Definitely Windows CE FTP server
		/// </summary>
		WindowsCE,
		/// <summary>
		/// Definitely WuFTPd server
		/// </summary>
		WuFTPd,
		/// <summary>
		/// Definitely GlobalScape EFT server
		/// </summary>
		GlobalScapeEFT,
		/// <summary>
		/// Definitely HP NonStop/Tandem server
		/// </summary>
		NonStopTandem,
		/// <summary>
		/// Definitely Serv-U server
		/// </summary>
		ServU,
		/// <summary>
		/// Definitely Cerberus FTP server
		/// </summary>
		Cerberus,
		/// <summary>
		/// Definitely Windows Server/IIS FTP server
		/// </summary>
		WindowsServerIIS,
		/// <summary>
		/// Definitely CrushFTP server
		/// </summary>
		CrushFTP,
		/// <summary>
		/// Definitely glFTPd server
		/// </summary>
		glFTPd
	}

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
	}

	/// <summary>
	/// Determines how we handle partially downloaded files
	/// </summary>
	public enum FtpLocalExists {

		/// <summary>
		/// Restart the download of a file if it is partially downloaded.
		/// Overwrites the file if it exists on disk.
		/// </summary>
		Overwrite,

		/// <summary>
		/// Resume the download of a file if it is partially downloaded.
		/// Appends to the file if it exists, by checking the length and adding the missing data.
		/// If the file doesn't exist on disk, a new file is created.
		/// </summary>
		Append,

		/// <summary>
		/// Blindly skip downloading the file if it exists on disk, without any more checks.
		/// This is only included to be compatible with legacy behaviour.
		/// </summary>
		Skip
	}

}