using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		void IInternalFtpClient.DisconnectInternal() {
			((IFtpClient)this).Disconnect();
		}

		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
		async Task IInternalFtpClient.DisconnectInternal(CancellationToken token) {
			await ((IAsyncFtpClient)this).Disconnect(token);
		}

	}
}
