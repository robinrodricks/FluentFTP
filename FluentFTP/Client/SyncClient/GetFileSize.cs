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
		/// </summary>
		protected void GetFileSizeInternal(string path, FtpSizeReply sizeReply, long defaultValue) {
			long length = defaultValue;

			path = path.GetFtpPath();

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (Status.FileSizeASCIINotSupported) {
				SetDataType(FtpDataType.Binary);
			}

			// execute the SIZE command
			var reply = Execute("SIZE " + path);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				length = defaultValue;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!Status.FileSizeASCIINotSupported && reply.Message.ContainsAnyCI(ServerStringModule.fileSizeNotInASCII)) {
					// set the flag so mode switching is done
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
