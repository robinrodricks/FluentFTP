using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="defaultValue">Value to return if there was an error obtaining the file size, or if the file does not exist</param>
		/// <returns>The size of the file, or defaultValue if there was a problem.</returns>
		public virtual long GetFileSize(string path, long defaultValue = -1) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(GetFileSize), new object[] { path });

			// execute server-specific file size fetching logic, if any
			if (ServerHandler != null && ServerHandler.IsCustomFileSize()) {
				return ServerHandler.GetFileSize(this, path);
			}

			if (!HasFeature(FtpCapability.SIZE)) {
				return defaultValue;
			}

			var sizeReply = new FtpSizeReply();
			GetFileSizeInternal(path, sizeReply, defaultValue);

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
		protected void GetFileSizeInternal(string path, FtpSizeReply sizeReply, long defaultValue) {
			long length = defaultValue;

			path = path.GetFtpPath();

			if (Status.FileSizeASCIINotSupported) {
				SetDataType(FtpDataType.Binary);
			}

			// execute the SIZE command
			var reply = Execute("SIZE " + path);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				length = defaultValue;

				// 550 SIZE not allowed in ASCII mode
				// Checking for the text "ascii" works in all languages seen up to now
				if (!Status.FileSizeASCIINotSupported && reply.Code == "550" && reply.Message.ToLower().Contains("ascii")) {
					// set the flag so mode switching is always done
					Status.FileSizeASCIINotSupported = true;

					// retry getting the file size
					GetFileSizeInternal(path, sizeReply, defaultValue);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = defaultValue;
			}

			sizeReply.FileSize = length;
		}


	}
}
