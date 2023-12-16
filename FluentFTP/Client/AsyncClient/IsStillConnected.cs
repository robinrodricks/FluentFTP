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
		public async Task<bool> IsStillConnected() {
			if (IsConnected && IsAuthenticated) {
				if (Noop()) {
					if (GetReply().Success) {
						return true;
					}
				}
			}
			return false;
		}

	}
}
