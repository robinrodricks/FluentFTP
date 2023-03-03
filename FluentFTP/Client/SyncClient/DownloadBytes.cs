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
		/// <param name="stopPosition">The last byte index that should be downloaded, or 0 if the entire file should be downloaded.</param>
		/// <returns>If true then the file was downloaded, false otherwise.</returns>
		public bool DownloadBytes(out byte[] outBytes, string remotePath, long restartPosition = 0, Action<FtpProgress> progress = null, long stopPosition = 0) {
			// verify args
			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(DownloadBytes), new object[] { remotePath });

			outBytes = null;

			// download the file from the server
			bool ok;
			using (var outStream = new MemoryStream()) {
				ok = DownloadFileInternal(null, remotePath, outStream, restartPosition, progress, new FtpProgress(1, 0), 0, false, stopPosition);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}

			return ok;
		}

	}
}
