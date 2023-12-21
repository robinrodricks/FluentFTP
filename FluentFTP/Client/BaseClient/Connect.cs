using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Connect to the server
		/// </summary>
		/// <param name="reConnect"> true indicates that we want a reconnect to take place.</param>
		void IInternalFtpClient.ConnectInternal(bool reConnect) {
			((IFtpClient)this).Connect(reConnect);
		}

		/// <summary>
		/// Connect to the server
		/// </summary>
		/// <param name="reConnect"> true indicates that we want a reconnect to take place.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		async Task IInternalFtpClient.ConnectInternal(bool reConnect, CancellationToken token) {
			await ((IAsyncFtpClient)this).Connect(reConnect, token);
		}

	}
}