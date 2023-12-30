using System;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Performs a series of tests to check if we are still connected to the FTP server.
		/// More thourough than IsConnected.
		/// </summary>
		/// <param name="timeout"/>How to wait for connection confirmation
		/// <returns>bool connection status</returns>
		public bool IsStillConnected(int timeout = 10000) {
			LogFunction(nameof(IsStillConnected), new object[] { timeout });

			return ((IInternalFtpClient)this).IsStillConnectedInternal(timeout);
		}

	}
}
