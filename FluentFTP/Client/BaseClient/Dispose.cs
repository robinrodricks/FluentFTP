using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		// Some BaseFtpClient methods refer to a "Dispose". It is necessary to route these
		// to the appropriate sync or async dispose, depending on FtpClient or AsyncFtpClient
		// having inherited BaseFtpClient

		/// <summary>
		/// Disconnects from the server
		/// </summary>
		void IInternalFtpClient.DisposeInternal() {
			// sync: This dispose percolates down to the BaseFtpClient.Dispose
			((IFtpClient)this).Dispose();
		}

		/// <summary>
		/// Disconnects from the server asynchronously
		/// </summary>
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
		async ValueTask IInternalFtpClient.DisposeInternal(CancellationToken token) {
			// async: This dispose handled in the AsyncFtpClient
			await ((IAsyncFtpClient)this).DisposeAsync();
		}
#else
		async Task IInternalFtpClient.DisposeInternal(CancellationToken token) {
			// async: This dispose handled in the AsyncFtpClient
			await ((IAsyncFtpClient)this).DisposeAsync();
		}
#endif

	}
}
