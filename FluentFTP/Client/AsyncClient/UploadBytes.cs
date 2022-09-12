using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentFTP.Streams;
using FluentFTP.Helpers;
using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Uploads the specified byte array as a file onto the server asynchronously.
		/// High-level API that takes care of various edge cases internally.
		/// Supports very large files since it uploads data in chunks.
		/// </summary>
		/// <param name="fileData">The full data of the file, as a byte array</param>
		/// <param name="remotePath">The full or relative path to the file on the server</param>
		/// <param name="existsMode">What to do if the file already exists? Skip, overwrite or append? Set this to <see cref="FtpRemoteExists.NoCheck"/> for fastest performance,
		///  but only if you are SURE that the files do not exist on the server.</param>
		/// <param name="createRemoteDir">Create the remote directory if it does not exist. Slows down upload due to additional checks required.</param>
		/// <param name="token">The token that can be used to cancel the entire process.</param>
		/// <param name="progress">Provide an implementation of IProgress to track upload progress.</param>
		/// <returns>FtpStatus flag indicating if the file was uploaded, skipped or failed to transfer.</returns>
		public async Task<FtpStatus> UploadBytes(byte[] fileData, string remotePath, FtpRemoteExists existsMode = FtpRemoteExists.Overwrite, bool createRemoteDir = false, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (fileData == null) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(fileData));
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			remotePath = remotePath.GetFtpPath();

			LogFunction(nameof(UploadBytes), new object[] { remotePath, existsMode, createRemoteDir });

			// write the file onto the server
			using (var ms = new MemoryStream(fileData)) {
				ms.Position = 0;
				return await UploadFileInternalAsync(ms, null, remotePath, createRemoteDir, existsMode, false, false, progress, token, new FtpProgress(1, 0));
			}
		}

	}
}
