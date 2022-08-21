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
		public bool EPSVNotSupported = false;

		/// <summary>
		/// Used to improve performance of GetFileSize.
		/// SIZE command is tried, and if the server cannot send it in ASCII mode, we switch to binary each time you call GetFileSize.
		/// However most servers will support ASCII, so we can get the file size without switching to binary, improving performance.
		/// </summary>
		public bool FileSizeASCIINotSupported = false;

		/// <summary>
		/// Used to improve performance of GetListing.
		/// You can set this to true by setting the RecursiveList property.
		/// </summary>
		public bool RecursiveListSupported = false;

		/// <summary>
		/// Used to automatically dispose cloned connections after FXP transfer has ended.
		/// </summary>
		public bool AutoDispose = false;

		/// <summary>
		/// Cached value of the last read working directory (absolute path).
		/// </summary>
		public string LastWorkingDir = null;

		/// <summary>
		/// Cached value of the last set hash algorithm.
		/// </summary>
		public FtpHashAlgorithm LastHashAlgo = FtpHashAlgorithm.NONE;

		/// <summary>
		/// Did the FTPS connection fail during the last Connect/ConnectAsync attempt?
		/// </summary>
		public bool ConnectionFTPSFailure = false;

		/// <summary>
		/// Did the UTF8 encoding setting work during the last Connect/ConnectAsync attempt?
		/// </summary>
		public bool ConnectionUTF8Success = false;

		/// <summary>
		/// Allow checking for stale data on socket?
		/// </summary>
		public bool AllowCheckStaleData = false;

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
		}

		/// <summary>
		/// These flags must be copied when we quickly clone the connection.
		/// </summary>
		public void CopyFrom(FtpClientState original) {
			EPSVNotSupported = original.EPSVNotSupported;
			FileSizeASCIINotSupported = original.FileSizeASCIINotSupported;
			RecursiveListSupported = original.RecursiveListSupported;
		}


	}
}
