using FluentFTP.Client.BaseClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFTP.Client.Modules {
	internal static class FileTransferModule {

		/// <summary>
		/// Calculate the position from which to append.
		/// </summary>
		public static long CalculateAppendLocalPosition(string remotePath, FtpRemoteExists existsMode, long remotePosition) {

			long localPosition = 0;

			// resume - start the local file from the same position as the remote file
			if (existsMode is FtpRemoteExists.Resume or FtpRemoteExists.ResumeNoCheck) {
				localPosition = remotePosition;
			}

			// append to end - start from the beginning of the local file
			else if (existsMode is FtpRemoteExists.AddToEnd or FtpRemoteExists.AddToEndNoCheck) {
				localPosition = 0;
			}

			return localPosition;
		}

		/// <summary>
		/// Calculate transfer chunk size taking rate control into account
		/// </summary>
		public static int CalculateTransferChunkSize(BaseFtpClient client, Int64 rateLimitBytes, int rateControlResolution) {
			int chunkSize = client.Config.TransferChunkSize;

			// if user has not specified a TransferChunkSize and rate limiting is enabled
			if (client.Config.TransferChunkSize == 65536 && rateLimitBytes > 0) {

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
