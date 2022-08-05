using System;
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Client.Modules;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif
using System.Linq;

namespace FluentFTP {
	public partial class FtpClient : IFtpClient, IDisposable {


		#region Dereference Link

		/// <summary>
		/// Recursively dereferences a symbolic link. See the
		/// MaximumDereferenceCount property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public FtpListItem DereferenceLink(FtpListItem item) {
			return DereferenceLink(item, MaximumDereferenceCount);
		}

		/// <summary>
		/// Recursively dereferences a symbolic link
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <param name="recMax">The maximum depth of recursion that can be performed before giving up.</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public FtpListItem DereferenceLink(FtpListItem item, int recMax) {
			LogFunc(nameof(DereferenceLink), new object[] { item.FullName, recMax });

			var count = 0;
			return DereferenceLink(item, recMax, ref count);
		}

		/// <summary>
		/// Dereference a FtpListItem object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		private FtpListItem DereferenceLink(FtpListItem item, int recMax, ref int count) {
			if (item.Type != FtpObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}

			foreach (var obj in GetListing(item.LinkTarget.GetFtpDirectoryName())) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpObjectType.Link) {
						if (++count == recMax) {
							return null;
						}

						return DereferenceLink(obj, recMax, ref count);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						var modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue) {
							obj.Modified = modify;
						}
					}

					if (obj.Type == FtpObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
						obj.Size = GetFileSize(obj.FullName);
					}

					return obj;
				}
			}

			return null;
		}

#if ASYNC
		/// <summary>
		/// Dereference a FtpListItem object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="count">Counter</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		private async Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, IntRef count, CancellationToken token = default(CancellationToken)) {
			if (item.Type != FtpObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}
			var listing = await GetListingAsync(item.LinkTarget.GetFtpDirectoryName(), token);
			foreach (FtpListItem obj in listing) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpObjectType.Link) {
						if (++count.Value == recMax) {
							return null;
						}

						return await DereferenceLinkAsync(obj, recMax, count, token);
					}

					if (HasFeature(FtpCapability.MDTM)) {
						var modify = GetModifiedTime(obj.FullName);

						if (modify != DateTime.MinValue) {
							obj.Modified = modify;
						}
					}

					if (obj.Type == FtpObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
						obj.Size = GetFileSize(obj.FullName);
					}

					return obj;
				}
			}

			return null;
		}

		/// <summary>
		/// Dereference a <see cref="FtpListItem"/> object asynchronously
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, int recMax, CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(DereferenceLinkAsync), new object[] { item.FullName, recMax });

			var count = new IntRef { Value = 0 };
			return DereferenceLinkAsync(item, recMax, count, token);
		}

		/// <summary>
		/// Dereference a <see cref="FtpListItem"/> object asynchronously
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		public Task<FtpListItem> DereferenceLinkAsync(FtpListItem item, CancellationToken token = default(CancellationToken)) {
			return DereferenceLinkAsync(item, MaximumDereferenceCount, token);
		}
#endif

		#endregion

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
			if (ServerHandler != null && ServerHandler.IsCustomFileSize())	{
				return ServerHandler.GetFileSize(this, path);
			}

			if (!HasFeature(FtpCapability.SIZE)) {
				return defaultValue;
			}

			var sizeReply = new FtpSizeReply();
#if !CORE14
			lock (m_lock) {
#endif
				GetFileSizeInternal(path, sizeReply, defaultValue);
#if !CORE14
			}
#endif
			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		private void GetFileSizeInternal(string path, FtpSizeReply sizeReply, long defaultValue) {
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
		private async Task GetFileSizeInternalAsync(string path, long defaultValue, CancellationToken token, FtpSizeReply sizeReply) {
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

#if !CORE14
			lock (m_lock) {
#endif

				// get modified date of a file
				if ((reply = Execute("MDTM " + path)).Success) {
					date = reply.Message.ParseFtpDate(this);
					date = ConvertDate(date);
				}

#if !CORE14
			}
#endif

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

#if !CORE14
			lock (m_lock) {
#endif

				// calculate the final date string with the timezone conversion
				date = ConvertDate(date, true);
				var timeStr = date.GenerateFtpDate();

				// set modified date of a file
				if ((reply = Execute("MFMT " + timeStr + " " + path)).Success) {
				}

#if !CORE14
			}

#endif
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
