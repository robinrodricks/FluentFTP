using System;
using FluentFTP.Helpers;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		#region Get File Size

		/// <summary>
		/// Gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="defaultValue">Value to return if there was an error obtaining the file size, or if the file does not exist</param>
		/// <returns>The size of the file, or defaultValue if there was a problem.</returns>
		public virtual long GetFileSize(string path, long defaultValue = -1) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetFileSize), new object[] { path });

			// execute server-specific file size fetching logic, if any
			if (ServerHandler != null && ServerHandler.IsCustomFileSize()) {
				return ServerHandler.GetFileSize(this, path);
			}

			if (!HasFeature(FtpCapability.SIZE)) {
				return defaultValue;
			}

			var sizeReply = new FtpSizeReply();
			lock (m_lock) {
				GetFileSizeInternal(path, sizeReply, defaultValue);
			}
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
				SetDataTypeNoLock(FtpDataType.Binary);
			}

			// execute the SIZE command
			var reply = Execute("SIZE " + path);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				length = defaultValue;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!Status.FileSizeASCIINotSupported && reply.Message.IsKnownError(ServerStringModule.fileSizeNotInASCII)) {
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

#if ASYNC
		/// <summary>
		/// Asynchronously gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="defaultValue">Value to return if there was an error obtaining the file size, or if the file does not exist</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The size of the file, or defaultValue if there was a problem.</returns>
		public async Task<long> GetFileSizeAsync(string path, long defaultValue = -1, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetFileSizeAsync), new object[] { path, defaultValue });

			// execute server-specific file size fetching logic, if any
			if (ServerHandler != null && ServerHandler.IsCustomFileSize()) {
				return await ServerHandler.GetFileSizeAsync(this, path, token);
			}

			if (!HasFeature(FtpCapability.SIZE)) {
				return defaultValue;
			}

			FtpSizeReply sizeReply = new FtpSizeReply();
			await GetFileSizeInternalAsync(path, defaultValue, token, sizeReply);

			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		protected async Task GetFileSizeInternalAsync(string path, long defaultValue, CancellationToken token, FtpSizeReply sizeReply) {
			long length = defaultValue;

			path = path.GetFtpPath();

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (Status.FileSizeASCIINotSupported) {
				await SetDataTypeNoLockAsync(FtpDataType.Binary, token);
			}

			// execute the SIZE command
			var reply = await ExecuteAsync("SIZE " + path, token);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				sizeReply.FileSize = defaultValue;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!Status.FileSizeASCIINotSupported && reply.Message.IsKnownError(ServerStringModule.fileSizeNotInASCII)) {
					// set the flag so mode switching is done
					Status.FileSizeASCIINotSupported = true;

					// retry getting the file size
					await GetFileSizeInternalAsync(path, defaultValue, token, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = defaultValue;
			}

			sizeReply.FileSize = length;

			return;
		}


#endif
		#endregion

		#region Get Modified Time

		/// <summary>
		/// Gets the modified time of a remote file.
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public virtual DateTime GetModifiedTime(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetModifiedTime), new object[] { path });

			var date = DateTime.MinValue;
			FtpReply reply;

			lock (m_lock) {

				// get modified date of a file
				if ((reply = Execute("MDTM " + path)).Success) {
					date = reply.Message.ParseFtpDate(this);
					date = ConvertDate(date);
				}

			}
			return date;
		}

#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public async Task<DateTime> GetModifiedTimeAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetModifiedTimeAsync), new object[] { path });

			var date = DateTime.MinValue;
			FtpReply reply;

			// get modified date of a file
			if ((reply = await ExecuteAsync("MDTM " + path, token)).Success) {
				date = reply.Message.ParseFtpDate(this);
				date = ConvertDate(date);
			}

			return date;
		}
#endif

		#endregion

		#region Set Modified Time

		/// <summary>
		/// Changes the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		public virtual void SetModifiedTime(string path, DateTime date) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(SetModifiedTime), new object[] { path, date });

			FtpReply reply;

			lock (m_lock) {

				// calculate the final date string with the timezone conversion
				date = ConvertDate(date, true);
				var timeStr = date.GenerateFtpDate();

				// set modified date of a file
				if ((reply = Execute("MFMT " + timeStr + " " + path)).Success) {
				}

			}
		}

#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetModifiedTimeAsync(string path, DateTime date, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(SetModifiedTimeAsync), new object[] { path, date });

			FtpReply reply;

			// calculate the final date string with the timezone conversion
			date = ConvertDate(date, true);
			var timeStr = date.GenerateFtpDate();

			// set modified date of a file
			if ((reply = await ExecuteAsync("MFMT " + timeStr + " " + path, token)).Success) {
			}
		}
#endif

		#endregion

	}
}
