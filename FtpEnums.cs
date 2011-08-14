using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
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
	/// The of data channel to be used
	/// </summary>
	public enum FtpDataChannelType {
        /// <summary>
        /// PORT Command
        /// </summary>
		Active,
        /// <summary>
        /// EPRT Command
        /// </summary>
        ExtendedActive,
        /// <summary>
        /// PASV Command
        /// </summary>
		Passive,
        /// <summary>
        /// EPSV Command
        /// </summary>
        ExtendedPassive
	}

	/// <summary>
	/// Transfer data over data channel in ASCII or binary.
	/// </summary>
	public enum FtpDataType {
		/// <summary>
		/// Transfer data as ASCII
		/// </summary>
		ASCII,
		/// <summary>
		/// Transfer data as binary
		/// </summary>
		Binary
	}

	/// <summary>
	/// Indicates the mode to use for transfering
	/// data between the client and server.
	/// </summary>
	public enum FtpDataMode {
		/// <summary>
		/// Default, opens a socket, transfers data and
		/// socket is closed to indicate eof. Can leave a lot
		/// of sockets in linger state on large transfers.
		/// </summary>
		Stream,
		/// <summary>
		/// Not implemented
		/// </summary>
		Block
	}

	/// <summary>
	/// The type of structure to use when transferring the file.
	/// Currently only file structure is supported, others will
	/// be added as necessary.
	/// </summary>
	public enum FtpDataStructure {
		/// <summary>
		/// Default, no special structure, sequential bytes
		/// </summary>
		File
	}

	/// <summary>
	/// The list command to be used on the server
	/// </summary>
	public enum FtpListType {
		/// <summary>
		/// Standard hard to parse file listing
		/// </summary>
		LIST,
		/// <summary>
		/// Newer easier to parse file listing
		/// </summary>
		MLSD,
		/// <summary>
		/// Newer easier to parse file listing that returns info on a single
		/// object over the command channel (no data channel required)
		/// </summary>
		MLST
	}

	/// <summary>
	/// Server features
	/// </summary>
    [Flags]
	public enum FtpCapability : int {
		/// <summary>
		/// Features haven't been loaded yet
		/// </summary>
		EMPTY = -1,
		/// <summary>
		/// This server said it doesn't support anything!
		/// </summary>
		NONE = 0,
		/// <summary>
		/// Supports the MLST command
		/// </summary>
		MLST = 1,
		/// <summary>
		/// Supports the MLSD command
		/// </summary>
		MLSD = 2,
		/// <summary>
		/// Supports the SIZE command
		/// </summary>
		SIZE = 4,
		/// <summary>
		/// Supports the MDTM command
		/// </summary>
		MDTM = 8,
		/// <summary>
		/// Supports download/upload stream resumes
		/// </summary>
		REST = 16,
		/// <summary>
		/// Supports the EPSV command
		/// </summary>
		EPSV = 32,
		/// <summary>
		/// Supports the EPRT command
		/// </summary>
		EPRT = 64,
		/// <summary>
		/// Supports retrieving modification times on directories
		/// </summary>
		MDTMDIR = 128
	}

	/// <summary>
	/// Indicate if we're using IPv4 or IPv6
	/// </summary>
	public enum FtpProtocolType : int {
		/// <summary>
		/// Use IPv4
		/// </summary>
		IPV4 = 1,
		/// <summary>
		/// Use IPv6 (this is not used anywhere in the code as of right now). It's reserved
		/// for the future when IPv6 finally replaces IPv4
		/// </summary>
		IPV6 = 2
	}

	/// <summary>
	/// File system object type
	/// </summary>
	public enum FtpObjectType {
		/// <summary>
		/// A directory.
		/// </summary>
		Directory,
		/// <summary>
		/// A file.
		/// </summary>
		File,
		/// <summary>
		/// No idea.
		/// </summary>
		Unknown
	}

	/// <summary>
	/// Indicates if the transfer in progress is an upload or a download
	/// </summary>
	public enum FtpTransferType {
		/// <summary>
		/// The transfer is an upload.
		/// </summary>
		Upload,
		/// <summary>
		/// The transfer is a download.
		/// </summary>
		Download
	}

	/// <summary>
	/// Indicates the type of SSL connection to use, if any.
	/// </summary>
	public enum FtpSslMode {
		/// <summary>
		/// Do not use SSL
		/// </summary>
		None,
		/// <summary>
		/// A SSL or TLS session is negotiated after the initial
		/// unencrypted connection, before credentials are sent.
		/// </summary>
		Explicit,
		/// <summary>
		/// SSL/TLS is implied upon the initial connection.
		/// </summary>
		Implicit
	}
}
