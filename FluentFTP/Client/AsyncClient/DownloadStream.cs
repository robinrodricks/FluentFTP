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
	public partial class AsyncFtpClient {

		/// <summary>
		/// Downloads the specified file into the specified stream asynchronously .
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it downloads data in chunks.
		/// </summary>
		/// <param name="outStream">The stream that the file will be written to. Provide a new MemoryStream if you only want to read the file into memory.</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="restartPosition">The size of the existing file in bytes, or 0 if unknown. The download restarts from this byte index.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <param name="stopPosition">The last byte index that should be downloaded, or 0 if the entire file should be downloaded.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public async Task<bool> DownloadStream(Stream outStream, string remotePath, long restartPosition = 0, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken), long stopPosition = 0) {
			// verify args
			if (outStream == null) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(outStream));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(DownloadStream), new object[] { remotePath });

			// download the file from the server
			return await DownloadFileInternalAsync(null, remotePath, outStream, restartPosition, progress, token, new FtpProgress(1, 0), 0, false, stopPosition);
		}

	}
}
