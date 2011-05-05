using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public enum FtpResponseType : int {
		None = 0,
		PositivePreliminary = 1,
		PositiveCompletion = 2,
		PositiveIntermediate = 3,
		TransientNegativeCompletion = 4,
		PermanentNegativeCompletion = 5
	}

	public enum FtpDataMode {
		Active,
		Passive
	}

	public enum FtpTransferMode {
		ASCII,
		Binary
	}

	public enum FtpListType {
		LIST,
		MLSD,
		MLST
	}

	public enum FtpCapability : int {
		EMPTY = -1,
		NONE = 0,
		MLST = 1,
		MLSD = 2,
		SIZE = 4,
		MDTM = 8,
		REST = 16,
		EPSV = 32,
		EPRT = 64,
		MDTMDIR = 128
	}

	public enum FtpProtocolType : int {
		IPV4 = 1,
		IPV6 = 2
	}

	public enum FtpObjectType {
		Directory,
		File,
		Unknown
	}

	public enum FtpTransferType {
		Upload,
		Download
	}

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
