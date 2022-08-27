using FluentFTP.Client.Modules;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Load the given connection profile and configure the FTP client instance accordingly.
		/// </summary>
		/// <param name="profile">Connection profile. Not modified.</param>
		public void LoadProfile(FtpProfile profile) {
			ConnectModule.LoadProfile(this, profile);
		}

	}
}
