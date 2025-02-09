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
		/// Note: Some servers do not support calculating the file size for a file that
		/// will be converted to ASCII on download - Pre-calculating that costs time and
		/// storage for the server, which can hurt performance server-side.
		/// The tradeoff is to issue a TYPE I first if needed, potentially causing overhead
		/// that detriments the transfer of multiple files.
		/// </summary>
		protected async Task GetFileSizeInternal(string path, long defaultValue, CancellationToken token, FtpSizeReply sizeReply) {
			long length = defaultValue;

			path = path.GetFtpPath();

			if (Status.FileSizeASCIINotSupported) {
				await SetDataTypeNoLockAsync(FtpDataType.Binary, token);
			}

			// execute the SIZE command
			var reply = await Execute("SIZE " + path, token);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				sizeReply.FileSize = defaultValue;

				// 550 SIZE not allowed in ASCII mode
				// Checking for the text "ascii" works in all languages seen up to now
				if (!Status.FileSizeASCIINotSupported && reply.Code == "550" && reply.Message.ToLower().Contains("ascii")) {
					// set the flag so mode switching is always done
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
