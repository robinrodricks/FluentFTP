using System;
using FluentFTP.Servers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif

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
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		public FtpListItem DereferenceLink(FtpListItem item) {
			return DereferenceLink(item, MaximumDereferenceCount);
		}

		/// <summary>
		/// Recursively dereferences a symbolic link
		/// </summary>
		/// <param name="item">The symbolic link</param>
		/// <param name="recMax">The maximum depth of recursion that can be performed before giving up.</param>
		/// <returns>FtpListItem, null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
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
		/// <example><code source="..\Examples\DereferenceLink.cs" lang="cs" /></example>
		private FtpListItem DereferenceLink(FtpListItem item, int recMax, ref int count) {
			if (item.Type != FtpFileSystemObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}

			foreach (var obj in GetListing(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList)) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
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

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
						obj.Size = GetFileSize(obj.FullName);
					}

					return obj;
				}
			}

			return null;
		}

#if !ASYNC
		private delegate FtpListItem AsyncDereferenceLink(FtpListItem item, int recMax);

		/// <summary>
		/// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="recMax">Maximum recursive calls</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, int recMax, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncDereferenceLink func;

			lock (m_asyncmethods) {
				ar = (func = DereferenceLink).BeginInvoke(item, recMax, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Begins an asynchronous operation to dereference a <see cref="FtpListItem"/> object. See the
		/// <see cref="MaximumDereferenceCount"/> property for controlling
		/// how deep this method will recurse before giving up.
		/// </summary>
		/// <param name="item">The item to dereference</param>
		/// <param name="callback">AsyncCallback</param>
		/// <param name="state">State Object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public IAsyncResult BeginDereferenceLink(FtpListItem item, AsyncCallback callback, object state) {
			return BeginDereferenceLink(item, MaximumDereferenceCount, callback, state);
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginDereferenceLink"/>
		/// </summary>
		/// <param name="ar">IAsyncResult</param>
		/// <returns>A <see cref="FtpListItem"/>, or null if the link can't be dereferenced</returns>
		/// <example><code source="..\Examples\BeginDereferenceLink.cs" lang="cs" /></example>
		public FtpListItem EndDereferenceLink(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncDereferenceLink>(ar).EndInvoke(ar);
		}

#endif
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
			if (item.Type != FtpFileSystemObjectType.Link) {
				throw new FtpException("You can only dereference a symbolic link. Please verify the item type is Link.");
			}

			if (item.LinkTarget == null) {
				throw new FtpException("The link target was null. Please check this before trying to dereference the link.");
			}

			var listing = await GetListingAsync(item.LinkTarget.GetFtpDirectoryName(), FtpListOption.ForceList, token);

			foreach (FtpListItem obj in listing) {
				if (item.LinkTarget == obj.FullName) {
					if (obj.Type == FtpFileSystemObjectType.Link) {
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

					if (obj.Type == FtpFileSystemObjectType.File && obj.Size < 0 && HasFeature(FtpCapability.SIZE)) {
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
		/// <returns>-1 if the command fails, otherwise the file size</returns>
		/// <example><code source="..\Examples\GetFileSize.cs" lang="cs" /></example>
		public virtual long GetFileSize(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetFileSize), new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) {
				return -1;
			}

			var sizeReply = new FtpSizeReply();
#if !CORE14
			lock (m_lock) {
#endif
				GetFileSizeInternal(path, sizeReply);
#if !CORE14
			}
#endif
			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		private void GetFileSizeInternal(string path, FtpSizeReply sizeReply) {
			long length = -1;

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (_FileSizeASCIINotSupported) {
				SetDataTypeNoLock(FtpDataType.Binary);
			}

			// execute the SIZE command
			var reply = Execute("SIZE " + path.GetFtpPath());
			sizeReply.Reply = reply;
			if (!reply.Success) {
				length = -1;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!_FileSizeASCIINotSupported && reply.Message.IsKnownError(FtpServerStrings.fileSizeNotInASCII)) {
					// set the flag so mode switching is done
					_FileSizeASCIINotSupported = true;

					// retry getting the file size
					GetFileSizeInternal(path, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = -1;
			}

			sizeReply.FileSize = length;
		}

#if !ASYNC
		private delegate long AsyncGetFileSize(string path);

		/// <summary>
		/// Begins an asynchronous operation to retrieve the size of a remote file
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public IAsyncResult BeginGetFileSize(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetFileSize func;

			lock (m_asyncmethods) {
				ar = (func = GetFileSize).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetFileSize"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetFileSize"/></param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		/// <example><code source="..\Examples\BeginGetFileSize.cs" lang="cs" /></example>
		public long EndGetFileSize(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetFileSize>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Asynchronously gets the size of a remote file, in bytes.
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The size of the file, -1 if there was a problem.</returns>
		public async Task<long> GetFileSizeAsync(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetFileSizeAsync), new object[] { path });

			if (!HasFeature(FtpCapability.SIZE)) {
				return -1;
			}

			FtpSizeReply sizeReply = new FtpSizeReply();
			await GetFileSizeInternalAsync(path, token, sizeReply);

			return sizeReply.FileSize;
		}

		/// <summary>
		/// Gets the file size of an object, without locking
		/// </summary>
		private async Task GetFileSizeInternalAsync(string path, CancellationToken token, FtpSizeReply sizeReply) {
			long length = -1;

			// Fix #137: Switch to binary mode since some servers don't support SIZE command for ASCII files.
			if (_FileSizeASCIINotSupported) {
				await SetDataTypeNoLockAsync(FtpDataType.Binary, token);
			}

			// execute the SIZE command
			var reply = await ExecuteAsync("SIZE " + path.GetFtpPath(), token);
			sizeReply.Reply = reply;
			if (!reply.Success) {
				sizeReply.FileSize = -1;

				// Fix #137: FTP server returns 'SIZE not allowed in ASCII mode'
				if (!_FileSizeASCIINotSupported && reply.Message.IsKnownError(FtpServerStrings.fileSizeNotInASCII)) {
					// set the flag so mode switching is done
					_FileSizeASCIINotSupported = true;

					// retry getting the file size
					await GetFileSizeInternalAsync(path, token, sizeReply);
					return;
				}
			}
			else if (!long.TryParse(reply.Message, out length)) {
				length = -1;
			}

			sizeReply.FileSize = length;

			return;
		}


#endif

		#endregion

		#region Get Modified Time

		/// <summary>
		/// Gets the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\GetModifiedTime.cs" lang="cs" /></example>
		public virtual DateTime GetModifiedTime(string path, FtpDate type = FtpDate.Original) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetModifiedTime), new object[] { path, type });

			var date = DateTime.MinValue;
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif

				// get modified date of a file
				if ((reply = Execute("MDTM " + path.GetFtpPath())).Success) {
					date = reply.Message.GetFtpDate(TimeConversion);

					// convert server timezone to UTC, based on the TimeOffset property
					if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
						date = date - m_listParser.TimeOffset;
					}

					// convert to local time if wanted
#if !CORE
					if (type == FtpDate.Local) {
						date = TimeZone.CurrentTimeZone.ToLocalTime(date);
					}

#endif
				}

#if !CORE14
			}
#endif

			return date;
		}

#if !ASYNC
		private delegate DateTime AsyncGetModifiedTime(string path, FtpDate type);

		/// <summary>
		/// Begins an asynchronous operation to get the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public IAsyncResult BeginGetModifiedTime(string path, FtpDate type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetModifiedTime func;

			lock (m_asyncmethods) {
				ar = (func = GetModifiedTime).BeginInvoke(path, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetModifiedTime"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetModifiedTime"/></param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		/// <example><code source="..\Examples\BeginGetModifiedTime.cs" lang="cs" /></example>
		public DateTime EndGetModifiedTime(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetModifiedTime>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="type">Return the date in local timezone or UTC?  Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public async Task<DateTime> GetModifiedTimeAsync(string path, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetModifiedTimeAsync), new object[] { path, type });

			var date = DateTime.MinValue;
			FtpReply reply;

			// get modified date of a file
			if ((reply = await ExecuteAsync("MDTM " + path.GetFtpPath(), token)).Success) {
				date = reply.Message.GetFtpDate(TimeConversion);

				// convert server timezone to UTC, based on the TimeOffset property
				if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
					date = date - m_listParser.TimeOffset;
				}

				// convert to local time if wanted
#if !CORE
				if (type == FtpDate.Local) {
					date = TimeZone.CurrentTimeZone.ToLocalTime(date);
				}

#endif
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
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		public virtual void SetModifiedTime(string path, DateTime date, FtpDate type = FtpDate.Original) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			LogFunc(nameof(SetModifiedTime), new object[] { path, date, type });

			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif

				// convert local to UTC if wanted
#if !CORE
				if (type == FtpDate.Local) {
					date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
				}
#endif

				// convert UTC to server timezone, based on the TimeOffset property
				if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
					date = date + m_listParser.TimeOffset;
				}

				// set modified date of a file
				var timeStr = date.ToString("yyyyMMddHHmmss");
				if ((reply = Execute("MFMT " + timeStr + " " + path.GetFtpPath())).Success) {
				}

#if !CORE14
			}

#endif
		}

#if !ASYNC
		private delegate void AsyncSetModifiedTime(string path, DateTime date, FtpDate type);

		/// <summary>
		/// Begins an asynchronous operation to get the modified time of a remote file
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginSetModifiedTime(string path, DateTime date, FtpDate type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetModifiedTime func;

			lock (m_asyncmethods) {
				ar = (func = SetModifiedTime).BeginInvoke(path, date, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginSetModifiedTime"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetModifiedTime"/></param>
		/// <returns>The modified time, or <see cref="DateTime.MinValue"/> if there was a problem</returns>
		public void EndSetModifiedTime(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetModifiedTime>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Gets the modified time of a remote file asynchronously
		/// </summary>
		/// <param name="path">The full path to the file</param>
		/// <param name="date">The new modified date/time value</param>
		/// <param name="type">Is the date provided in local timezone or UTC? Use FtpDate.Original to disable timezone conversion.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		public async Task SetModifiedTimeAsync(string path, DateTime date, FtpDate type = FtpDate.Original, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			if (date == null) {
				throw new ArgumentException("Required parameter is null or blank.", "date");
			}

			LogFunc(nameof(SetModifiedTimeAsync), new object[] { path, date, type });

			FtpReply reply;

			// convert local to UTC if wanted
#if !CORE
			if (type == FtpDate.Local) {
				date = TimeZone.CurrentTimeZone.ToUniversalTime(date);
			}
#endif

			// convert UTC to server timezone, based on the TimeOffset property
			if (type != FtpDate.Original && m_listParser.HasTimeOffset) {
				date = date + m_listParser.TimeOffset;
			}

			// set modified date of a file
			var timeStr = date.ToString("yyyyMMddHHmmss");
			if ((reply = await ExecuteAsync("MFMT " + timeStr + " " + path.GetFtpPath(), token)).Success) {
			}
		}
#endif

		#endregion

	}
}
