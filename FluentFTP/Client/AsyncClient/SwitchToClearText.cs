using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Send a 'CCC' comamnd to the FTP server and if successful closes the undelying ssl stream.
		/// </summary>
		public async Task SwitchToClearText() {

			var reply = await Execute("CCC");
			if (reply.Success) {
				m_stream.SwitchToUnsecuredMode();
				Config.EncryptionMode = FtpEncryptionMode.None;
			}
		}

	}
}
