using System.Collections.Generic;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Load the given connection profile and configure the FTP client instance accordingly.
		/// </summary>
		/// <param name="profile">Connection profile. Not modified.</param>
		public void LoadProfile(FtpProfile profile) {
			ConnectModule.LoadProfile(this, profile);
		}
	}
}
