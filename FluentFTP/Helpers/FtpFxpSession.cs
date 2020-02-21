using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP {
	public class FtpFxpSession : IDisposable {
		/// <summary>
		/// A connection to the FTP server where the file or folder is currently stored
		/// </summary>
		public FtpClient SourceServer;

		/// <summary>
		/// A connection to the destination FTP server where you want to create the file or folder
		/// </summary>
		public FtpClient TargetServer;

		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		public void Dispose() {
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;
			GC.SuppressFinalize(this);

		}
	}
}