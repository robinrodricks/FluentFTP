using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Asynchronously gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="defaultValue">Value to return if there was an error obtaining the file size, or if the file does not exist</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The size of the file, or defaultValue if there was a problem.</returns>
		public async Task<long> GetFileSize(string path, long defaultValue = -1, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetFileSize), new object[] { path, defaultValue });

			// execute server-specific file size fetching logic, if any
			if (ServerHandler != null && ServerHandler.IsCustomFileSize()) {
				return await ServerHandler.GetFileSizeAsync(this, path, token);
			}

			if (!HasFeature(FtpCapability.SIZE)) {
				return defaultValue;
			}

			FtpSizeReply sizeReply = new FtpSizeReply();
			await GetFileSizeInternal(path, defaultValue, token, sizeReply);

			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		protected async Task GetFileSizeInternal(string path, long defaultValue, CancellationToken token, FtpSizeReply sizeReply) {
			long length = defaultValue;

			path = path.GetFtpPath();

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (Status.FileSizeASCIINotSupported) {
				await SetDataTypeNoLockAsync(FtpDataType.Binary, token);
			}

			// execute the SIZE command
			var reply = await Execute("SIZE " + path, token);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				sizeReply.FileSize = defaultValue;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!Status.FileSizeASCIINotSupported && reply.Message.ContainsAnyCI(ServerStringModule.fileSizeNotInASCII)) {
					// set the flag so mode switching is done
					Status.FileSizeASCIINotSupported = true;

					// retry getting the file size
					await GetFileSizeInternal(path, defaultValue, token, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = defaultValue;
			}

			sizeReply.FileSize = length;

			return;
		}

	}
}
