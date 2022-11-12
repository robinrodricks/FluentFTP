using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP {
	public class FtpClientState {


		/// <summary>
		/// Used to improve performance of OpenPassiveDataStream.
		/// Enhanced-passive mode is tried once, and if not supported, is not tried again.
		/// </summary>
		public bool EPSVNotSupported { get; set; } = false;

		/// <summary>
		/// Used to improve performance of GetFileSize.
		/// SIZE command is tried, and if the server cannot send it in ASCII mode, we switch to binary each time you call GetFileSize.
		/// However most servers will support ASCII, so we can get the file size without switching to binary, improving performance.
		/// </summary>
		public bool FileSizeASCIINotSupported { get; set; } = false;

		/// <summary>
		/// Used to improve performance of GetListing.
		/// You can set this to true by setting the RecursiveList property.
		/// </summary>
		public bool RecursiveListSupported { get; set; } = false;

		/// <summary>
		/// Used to automatically dispose cloned connections after FXP transfer has ended.
		/// </summary>
		public bool AutoDispose { get; set; } = false;

		/// <summary>
		/// Cached value of the last read working directory (absolute path).
		/// </summary>
		public string LastWorkingDir { get; set; } = null;

		/// <summary>
		/// Cached value of the last set hash algorithm.
		/// </summary>
		public FtpHashAlgorithm LastHashAlgo { get; set; } = FtpHashAlgorithm.NONE;

		/// <summary>
		/// Did the FTPS connection fail during the last Connect/ConnectAsync attempt?
		/// </summary>
		public bool ConnectionFTPSFailure { get; set; } = false;

		/// <summary>
		/// Did the UTF8 encoding setting work during the last Connect/ConnectAsync attempt?
		/// </summary>
		public bool ConnectionUTF8Success { get; set; } = false;

		/// <summary>
		/// Store the current data type setting
		/// </summary>
		public FtpDataType CurrentDataType { get; set; } = FtpDataType.Unknown;

		/// <summary>
		/// Allow checking for stale data on socket?
		/// </summary>
		public bool AllowCheckStaleData { get; set; } = false;

		/// <summary>
		/// These flags must be reset every time we connect, to allow for users to connect to
		/// different FTP servers with the same client object.
		/// </summary>
		public void Reset() {
			EPSVNotSupported = false;
			FileSizeASCIINotSupported = false;
			RecursiveListSupported = false;
			LastWorkingDir = null;
			LastHashAlgo = FtpHashAlgorithm.NONE;
			ConnectionFTPSFailure = false;
			ConnectionUTF8Success = false;
			AllowCheckStaleData = false;
			CurrentDataType = FtpDataType.Unknown;
		}

		/// <summary>
		/// These flags must be copied when we quickly clone the connection.
		/// </summary>
		public void CopyFrom(FtpClientState original) {
			EPSVNotSupported = original.EPSVNotSupported;
			FileSizeASCIINotSupported = original.FileSizeASCIINotSupported;
			RecursiveListSupported = original.RecursiveListSupported;
		}

		/// <summary>
		/// During and after a z/OS GetListing(), this value stores the
		/// z/OS filesystem realm that was encountered.
		/// The value is used internally to control the list parse mode
		/// </summary>
		public FtpZOSListRealm zOSListingRealm { get; set; }

		/// <summary>
		/// During and after a z/OS GetListing(), this value stores the
		/// the LRECL that was encountered (for a realm = Member only).
		/// The value is used internally to calculate member sizes
		/// </summary>
		public ushort zOSListingLRECL { get; set; }

	}
}