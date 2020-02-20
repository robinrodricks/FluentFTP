using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP
{
	public class FtpFxpSession: IDisposable
	{
		private FtpClient m_sourceFtpClient;

		public FtpClient sourceFtpClient
		{
			get => m_sourceFtpClient;
			set => m_sourceFtpClient = value;
		}

		private FtpClient m_destinationFtpClient;

		public FtpClient destinationFtpClient
		{
			get => m_destinationFtpClient;
			set => m_destinationFtpClient = value;
		}

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

			if (sourceFtpClient.IsConnected)
			{
				sourceFtpClient.Disconnect();
			}

			if (destinationFtpClient.IsConnected)
			{
				destinationFtpClient.Disconnect();
			}

			IsDisposed = true;
			GC.SuppressFinalize(this);

		}
	}
}