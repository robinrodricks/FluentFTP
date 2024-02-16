using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;
using System.Security.Authentication;
using FluentFTP.Proxy.AsyncProxy;

namespace FluentFTP {
	public partial class AsyncFtpClient {

#if NET5_0_OR_GREATER
		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public async IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, FtpListOption options, CancellationToken token = default(CancellationToken), [EnumeratorCancellation] CancellationToken enumToken = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				await foreach (FtpListItem i in GetListingRecursiveEnumerable(await GetAbsolutePathAsync(path, token), options, token, enumToken)) {
					yield return i;
				}

				yield break;
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunction(nameof(GetListing), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(path);
			var autoRestore = Config.ShouldAutoRestore(path);

			if (autoNav) { 
				options = options | FtpListOption.NoPath;
			}

			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			if (autoNav) {
				pwdSave = await GetWorkingDirectory(token);
				if (pwdSave != path) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + path + "\"");
					await SetWorkingDirectory(path);
				}
			}

			rawlisting = await GetListingInternal(listcmd, options, true, token);

			if (autoRestore) {
				if (pwdSave != await GetWorkingDirectory(token)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
					await SetWorkingDirectory(pwdSave);
				}
			}

			FtpListItem item = null;

			for (var i = 0; i < rawlisting.Count; i++) {
				string rawEntry = rawlisting[i];

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				if (!isNameList) {

					// load basic information available within the file listing
					if (!LoadBasicListingInfo(ref path, ref item, lst, rawlisting, ref i, listcmd, rawEntry, isRecursive, isIncludeSelf, machineList)) {

						// skip unwanted listings
						continue;
					}

				}

				item = await GetListingProcessItemAsync(item, lst, rawEntry, listcmd, token,
					isIncludeSelf, isNameList, isRecursive, isGetModified, isGetSize
				);
				if (item != null) {
					yield return item;
				}
			}
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public IAsyncEnumerable<FtpListItem> GetListingEnumerable(string path, CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingEnumerable(path, 0, token, enumToken);
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <returns>An array of items retrieved in the listing</returns>
		public IAsyncEnumerable<FtpListItem> GetListingEnumerable(CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingEnumerable(null, token, enumToken);
		}

#endif

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public async Task<FtpListItem[]> GetListing(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				return await GetListingRecursive(await GetAbsolutePathAsync(path, token), options, token);
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunction(nameof(GetListing), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(path);
			var autoRestore = Config.ShouldAutoRestore(path);

			if (autoNav) { 
				options = options | FtpListOption.NoPath;
			}

			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			if (autoNav) {
				pwdSave = await GetWorkingDirectory(token);
				if (pwdSave != path) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + path + "\"");
					await SetWorkingDirectory(path);
				}
			}

			rawlisting = await GetListingInternal(listcmd, options, true, token);

			if (autoRestore) {
				if (pwdSave != await GetWorkingDirectory(token)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
					await SetWorkingDirectory(pwdSave);
				}
			}

			FtpListItem item = null;

			for (var i = 0; i < rawlisting.Count; i++) {
				string rawEntry = rawlisting[i];

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				if (!isNameList) {

					// load basic information available within the file listing
					if (!LoadBasicListingInfo(ref path, ref item, lst, rawlisting, ref i, listcmd, rawEntry, isRecursive, isIncludeSelf, machineList)) {

						// skip unwanted listings
						continue;
					}

				}

				item = await GetListingProcessItemAsync(item, lst, rawEntry, listcmd, token,
					isIncludeSelf, isNameList, isRecursive, isGetModified, isGetSize
				);

			}
			return lst.ToArray();
		}

		/// <summary>
		/// Process the output of the listing command
		/// </summary>
		protected async Task<FtpListItem> GetListingProcessItemAsync(FtpListItem item, List<FtpListItem> lst, string rawEntry, string listcmd, CancellationToken token, bool isIncludeSelf, bool isNameList, bool isRecursive, bool isGetModified, bool isGetSize) {

			if (isNameList) {
				// if NLST was used we only have a file name so
				// there is nothing to parse.
				item = new FtpListItem() {
					FullName = rawEntry
				};

				if (await DirectoryExists(item.FullName, token)) {
					item.Type = FtpObjectType.Directory;
				}
				else {
					item.Type = FtpObjectType.File;
				}
				lst.Add(item);
			}

			// load extended information that wasn't available if the list options flags say to do so.
			if (item != null) {

				// if need to get file modified date
				if (isGetModified && HasFeature(FtpCapability.MDTM)) {
					// if the modified date was not loaded or the modified date is more than a day in the future 
					// and the server supports the MDTM command, load the modified date.
					// most servers do not support retrieving the modified date
					// of a directory but we try any way.
					if (item.Modified == DateTime.MinValue || listcmd.StartsWith("LIST")) {
						DateTime modify;

						if (item.Type == FtpObjectType.Directory) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Trying to retrieve modification time of a directory, some servers don't like this...");
						}

						if ((modify = await GetModifiedTime(item.FullName, token: token)) != DateTime.MinValue) {
							item.Modified = modify;
						}
					}
				}

				// if need to get file size
				if (isGetSize && HasFeature(FtpCapability.SIZE)) {
					// if no size was parsed, the object is a file and the server
					// supports the SIZE command, then load the file size
					if (item.Size == -1) {
						if (item.Type != FtpObjectType.Directory) {
							item.Size = await GetFileSize(item.FullName, -1, token);
						}
						else {
							item.Size = 0;
						}
					}
				}
			}

			return item;
		}

		/// <summary>
		/// Get the records of a file listing and retry if temporary failure.
		/// </summary>
		protected async Task<List<string>> GetListingInternal(string listcmd, FtpListOption options, bool retry, CancellationToken token) {
			var rawlisting = new List<string>();
			var isUseStat = options.HasFlag(FtpListOption.UseStat);

			// Get the file listing in the desired format
			await SetDataTypeNoLockAsync(Config.ListingDataType, token);

			try {

				// read in raw file listing from control stream
				if (isUseStat) {
					var reply = await Execute(listcmd, token);
					if (reply.Success) {

						Log(FtpTraceLevel.Verbose, "+---------------------------------------+");

						if (reply.InfoMessages != null) {
							foreach (var line in reply.InfoMessages.Split('\n')) {
								if (!Strings.IsNullOrWhiteSpace(line)) {
									rawlisting.Add(line);
									Log(FtpTraceLevel.Verbose, "Listing:  " + line);
								}
							}
						}

						Log(FtpTraceLevel.Verbose, "-----------------------------------------");
					}
				}
				else {

					// read in raw file listing from data stream
					try {
						await using (FtpDataStream stream = await OpenDataStreamAsync(listcmd, 0, token)) {
							try {
								if (this is AsyncFtpClientSocks4Proxy || this is AsyncFtpClientSocks4aProxy) {
									// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
									// we need to skip them otherwise they will be downloaded to the file
									// moreover, these bytes cause "Failed to get the EPSV port" error
									await stream.ReadAsync(new byte[6], 0, 6);
								}

								Log(FtpTraceLevel.Verbose, "+---------------------------------------+");

								if (Config.BulkListing) {
									// increases performance of GetListing by reading multiple lines of the file listing at once
									foreach (var line in await stream.ReadAllLinesAsync(Encoding, Config.BulkListingLength, token)) {
										if (!Strings.IsNullOrWhiteSpace(line)) {
											rawlisting.Add(line);
											Log(FtpTraceLevel.Verbose, "Listing:  " + line);
										}
									}
								}
								else {
									// GetListing will read file listings line-by-line (actually byte-by-byte)
									string buf;
									while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
										if (buf.Length > 0) {
											rawlisting.Add(buf);
											Log(FtpTraceLevel.Verbose, "Listing:  " + buf);
										}
									}
								}

								Log(FtpTraceLevel.Verbose, "-----------------------------------------");
							}
							finally {
								await stream.CloseAsync(token);
							}
						}
					}
					catch (AuthenticationException) {
						FtpReply reply = await ((IInternalFtpClient)this).GetReplyInternal(token, listcmd, false, -1); // no exhaustNoop, but non-blocking
						if (!reply.Success) {
							throw new FtpCommandException(reply);
						}
						throw;
					}
				}
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when listing an empty directory
				// and the connection fails because no communication socket is provided by the server
			}
			catch (FtpCommandException ftpEx) {
				// Fix for #589 - CompletionCode is null
				if (ftpEx.CompletionCode == null) {
					throw new FtpException(ftpEx.Message + " - Try using FtpListOption.UseStat which might fix this.", ftpEx);
				}
				// Some FTP servers throw 550 for empty folders. Absorb these.
				if (!ftpEx.CompletionCode.StartsWith("550")) {
					throw ftpEx;
				}
			}
			catch (IOException ioEx) {
				// Some FTP servers forcibly close the connection, we absorb these errors,
				// unless we have lost the control connection itself
				if (m_stream.IsConnected == false) {
					if (retry) {
						// retry once more, but do not go into a infinite recursion loop here
						// note: this will cause an automatic reconnect in Execute(...)
						Log(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to control connection disconnect");
						return await GetListingInternal(listcmd, options, false, token);
					}
					else {
						throw;
					}
				}

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.ContainsAnyCI(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					Log(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to unexpected EOF");
					return await GetListingInternal(listcmd, options, false, token);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public Task<FtpListItem[]> GetListing(string path, CancellationToken token = default(CancellationToken)) {
			return GetListing(path, 0, token);
		}


		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <returns>An array of items retrieved in the listing</returns>
		public Task<FtpListItem[]> GetListing(CancellationToken token = default(CancellationToken)) {
			return GetListing(null, token);
		}

#if NET5_0_OR_GREATER
		/// <summary>
		/// Recursive method of GetListingAsync, to recurse through directories on servers that do not natively support recursion.
		/// Automatically called by GetListingAsync where required.
		/// Uses flat recursion instead of head recursion.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <param name="token"></param>
		/// <param name="enumToken"></param>
		/// <returns>An array of FtpListItem objects</returns>

		protected async IAsyncEnumerable<FtpListItem> GetListingRecursiveEnumerable(string path, FtpListOption options, CancellationToken token, [EnumeratorCancellation] CancellationToken enumToken = default) {
			// remove the recursive flag
			options &= ~FtpListOption.Recursive;

			// add initial path to list of folders to explore
			var stack = new Stack<string>();
			stack.Push(path);
			var allFiles = new List<FtpListItem>();

			// explore folders
			while (stack.Count > 0) {
				// get path of folder to list
				var currentPath = stack.Pop();
				if (!currentPath.EndsWith("/")) {
					currentPath += "/";
				}

				// extract the directories
				await foreach (var item in GetListingEnumerable(currentPath, options, token)) {
					// break if task is cancelled
					token.ThrowIfCancellationRequested();

					if (item.Type == FtpObjectType.Directory && item.Name is not ("." or "..")) {
						stack.Push(item.FullName);
					}

					yield return item;
				}

				// recurse
			}
		}
#endif

		/// <summary>
		/// Recursive method of GetListingAsync, to recurse through directories on servers that do not natively support recursion.
		/// Automatically called by GetListingAsync where required.
		/// Uses flat recursion instead of head recursion.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <param name="token"></param>
		/// <returns>An array of FtpListItem objects</returns>
		protected async Task<FtpListItem[]> GetListingRecursive(string path, FtpListOption options, CancellationToken token) {

			// remove the recursive flag
			options &= ~FtpListOption.Recursive;

			// add initial path to list of folders to explore
			var stack = new Stack<string>();
			stack.Push(path);
			var allFiles = new List<FtpListItem>();

			// explore folders
			while (stack.Count > 0) {
				// get path of folder to list
				var currentPath = stack.Pop();
				if (!currentPath.EndsWith("/")) {
					currentPath += "/";
				}

				// list it
				FtpListItem[] items = await GetListing(currentPath, options, token);

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				// add it to the final listing
				allFiles.AddRange(items);

				// extract the directories
				foreach (var item in items) {
					if (item.Type == FtpObjectType.Directory && item.Name is not ("." or "..")) {
						stack.Push(item.FullName);
					}
				}

				items = null;

				// recurse
			}

			// final list of all files and dirs
			return allFiles.ToArray();
		}

	}
}
