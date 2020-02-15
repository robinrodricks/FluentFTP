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

		private bool m_isDisposed = false;

		/// <summary>
		/// Gets a value indicating if this object has already been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get => m_isDisposed;
			private set => m_isDisposed = value;
		}

		public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			try
			{
				if (sourceFtpClient.IsConnected)
				{
					sourceFtpClient.Disconnect();
				}
			}
			catch (Exception ex)
			{
			}

			try
			{
				if (destinationFtpClient.IsConnected)
				{
					destinationFtpClient.Disconnect();
				}
			}
			catch (Exception ex)
			{
			}


			IsDisposed = true;
			GC.SuppressFinalize(this);

		}
	}
}
