using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP
{
	public class FtpFxpSession: IDisposable
	{
		/// <summary>
		/// A connection to the server where the file or folder is currently stored
		/// </summary>
		public FtpClient SourceClient;

		/// <summary>
		/// A connection to the destination server where you want to create the file or folder
		/// </summary>
		public FtpClient TargetClient;

        /// <summary>
        /// Gets a value indicating if this object has already been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			if (SourceClient.IsConnected)
			{
				SourceClient.Disconnect();
			}

			if (TargetClient.IsConnected)
			{
				TargetClient.Disconnect();
			}

			IsDisposed = true;
			GC.SuppressFinalize(this);

		}
	}
}