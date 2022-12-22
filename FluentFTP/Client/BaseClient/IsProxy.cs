using System.Net;
using FluentFTP.Proxy.AsyncProxy;
using FluentFTP.Proxy.SyncProxy;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Checks if this FTP/FTPS connection is made through a proxy.
		/// </summary>
		public bool IsProxy() {
			return this is FtpClientProxy or AsyncFtpClientProxy;
		}

	}
}
