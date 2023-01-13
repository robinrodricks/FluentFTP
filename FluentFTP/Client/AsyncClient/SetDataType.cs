using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Sets the data type of information sent over the data stream asynchronously
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task SetDataTypeAsync(FtpDataType type, CancellationToken token = default(CancellationToken)) {

			await SetDataTypeNoLockAsync(type, token);
		}

		/// <summary>
		/// Sets the data type of information sent over the data stream asynchronously
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task SetDataTypeNoLockAsync(FtpDataType type, CancellationToken token = default(CancellationToken)) {
			// FIX : #291 only change the data type if different
			if (Status.CurrentDataType != type) {
				FtpReply reply;
				switch (type) {
					case FtpDataType.ASCII:
						if (!(reply = await Execute("TYPE A", token)).Success) {
							throw new FtpCommandException(reply);
						}

						break;

					case FtpDataType.Binary:
						if (!(reply = await Execute("TYPE I", token)).Success) {
							throw new FtpCommandException(reply);
						}

						break;

					default:
						throw new FtpException("Unsupported data type: " + type.ToString());
				}

				Status.CurrentDataType = type;
			}
		}

	}
}