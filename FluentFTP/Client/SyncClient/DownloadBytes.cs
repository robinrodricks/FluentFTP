using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadBytes), new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (var outStream = new MemoryStream()) {
				ok = DownloadFileInternal(null, remotePath, outStream, restartPosition, progress, new FtpProgress(1, 0), 0, false);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}

			return ok;
		}


#if ASYNC
		/// <summary>
		/// Downloads the specified file and return the raw byte array.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadBytesAsync(string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "remotePath");
			}

			remotePath = remotePath.GetFtpPath();

			LogFunc(nameof(DownloadBytesAsync), new object[] { remotePath });

			// download the file from the server
			using (var outStream = new MemoryStream()) {
				var ok = await DownloadFileInternalAsync(null, remotePath, outStream, restartPosition, progress, token, new FtpProgress(1, 0), 0, false);
				return ok ? outStream.ToArray() : null;
			}
		}

		/// <summary>
		/// Downloads the specified file and return the raw byte array asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadBytesAsync(string remotePath, CancellationToken token = default(CancellationToken)) {
			// download the file from the server
			return await DownloadBytesAsync(remotePath, 0, null, token);
		}
#endif
	}
}
