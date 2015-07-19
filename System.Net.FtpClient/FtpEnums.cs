using System;

namespace System.Net.FtpClient {
    /// <summary>
    /// Defines the type of encryption to use
    /// </summary>
    public enum FtpEncryptionMode {
        /// <summary>
        /// Plain text.
        /// </summary>
        None,
        /// <summary>
        /// Encryption is used from the start of the connection, port 990
        /// </summary>
        Implicit,
        /// <summary>
        /// Connection starts in plain text and encryption is enabled
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
        /// Successs
        /// </summary>
        PositiveCompletion = 2,
        /// <summary>
        /// Succcess
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
        /// modifid date of an object on the server
        /// </summary>
        MFMT = 64,
        /// <summary>
        /// Server supports the MFCT command for setting the
        /// created date of an object on the server
        /// </summary>
        MFCT = 128,
        /// <summary>
        /// Server supports the MFF command for setting certain facts
        /// about file sytem objects. If you need this command, it would
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
        /// connect to the IP address dicated by the server
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
        /// and the data conncetion is made to the same address that the control
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
        /// connects to the IP address it sees the client comming
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
        /// Writeable
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
    /// Flags that can dicate how a file listing is performed
    /// </summary>
    [Flags]
    public enum FtpListOption {
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
        /// Force the use of the NLST command even if MLSD
        /// is supported by the server
        /// </summary>
        ForceList = 8,
        /// <summary>
        /// Use the NLST command instead of LIST for a reliable file listing
        /// </summary>
        NameList = 16,
        /// <summary>
        /// Combines the ForceList and NameList flags
        /// </summary>
        ForceNameList = ForceList | NameList,
        /// <summary>
        /// Try to dereference symbolic links
        /// </summary>
        DerefLinks = 32,
        /// <summary>
        /// Sets the ForceList flag and uses `LS' instead of `LIST' as the
        /// command for getting a directory listing. This option overrides
        /// ForceNameList and ignores the AllFiles flag.
        /// </summary>
        UseLS = 64 | ForceList,
        /// <summary>
        /// Adds the -r option to the list command. Some servers may not
        /// support this feature.
        /// </summary>
        Recursive = 128,
        /// <summary>
        /// Do not retrieve path when no path is supplied to GetListing(),
        /// instead just execute LIST with no path argument.
        /// </summary>
        NoPath = 256
    }
}
