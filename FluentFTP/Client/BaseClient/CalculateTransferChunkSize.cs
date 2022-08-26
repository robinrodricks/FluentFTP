using System;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Calculate transfer chunk size taking rate control into account
		/// </summary>
		protected int CalculateTransferChunkSize(Int64 rateLimitBytes, int rateControlResolution) {
			int chunkSize = TransferChunkSize;

			// if user has not specified a TransferChunkSize and rate limiting is enabled
			if (m_transferChunkSize == null && rateLimitBytes > 0) {

				// reduce chunk size to optimize rate control
				const int chunkSizeMin = 64;
				while (chunkSize > chunkSizeMin) {
					var chunkLenInMs = 1000L * chunkSize / rateLimitBytes;
					if (chunkLenInMs <= rateControlResolution) {
						break;
					}

					chunkSize = Math.Max(chunkSize >> 1, chunkSizeMin);
				}
			}
			return chunkSize;
		}
	}
}
