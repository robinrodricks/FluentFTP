using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Exceptions;
using FluentFTP.Proxy;
using FluentFTP.Helpers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region Get File Info

		/// <summary>
		/// Returns information about a file system object. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefulness of this method for checking for the existence of an object.
		/// </summary>
		/// <param name="path">The path of the file or folder</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <returns>A FtpListItem object</returns>
		public FtpListItem GetObjectInfo(string path, bool dateModified = false) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetObjectInfo), new object[] { path, dateModified });

			FtpReply reply;
			string[] res;

			var supportsMachineList = HasFeature(FtpCapability.MLSD);

			FtpListItem result = null;

			if (supportsMachineList) {
				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = Execute("MLST " + path)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						var info = new StringBuilder();

						for (var i = 1; i < res.Length; i++) {
							info.Append(res[i]);
						}

						result = m_listParser.ParseSingleLine(null, info.ToString(), m_capabilities, true);
					}
				}
				else {
					LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
				}
			}
			else {
				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				var dirPath = path.GetFtpDirectoryName();
				var dirItems = GetListing(dirPath);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)) {
				var alternativeModifiedDate = GetModifiedTime(path);
				if (alternativeModifiedDate != default) {
					result.Modified = alternativeModifiedDate;
				}
			}

			return result;
		}

#if ASYNC
		/// <summary>
		/// Return information about a remote file system object asynchronously. 
		/// </summary>
		/// <remarks>
		/// You should check the <see cref="Capabilities"/> property for the <see cref="FtpCapability.MLSD"/> 
		/// flag before calling this method. Failing to do so will result in an InvalidOperationException
		/// being thrown when the server does not support machine listings. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefulness of this method for checking for the existence of an object.</remarks>
		/// <param name="path">Path of the item to retrieve information about</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <exception cref="InvalidOperationException">Thrown if the server does not support this Capability</exception>
		/// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public async Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			path = path.GetFtpPath();

			LogFunc(nameof(GetObjectInfo), new object[] { path, dateModified });

			FtpReply reply;
			string[] res;

			var supportsMachineList = HasFeature(FtpCapability.MLSD);

			FtpListItem result = null;

			if (supportsMachineList) {
				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = await ExecuteAsync("MLST " + path, token)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						var info = new StringBuilder();

						for (var i = 1; i < res.Length; i++) {
							info.Append(res[i]);
						}

						result = m_listParser.ParseSingleLine(null, info.ToString(), m_capabilities, true);
					}
				}
				else {
					LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
				}
			}
			else {
				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				var dirPath = path.GetFtpDirectoryName();
				var dirItems = await GetListingAsync(dirPath, token);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				LogStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)) {
				var alternativeModifiedDate = await GetModifiedTimeAsync(path, token);
				if (alternativeModifiedDate != default) {
					result.Modified = alternativeModifiedDate;
				}
			}

			return result;
		}
#endif

		#endregion

		#region Get Listing

		/// <summary>
		/// Gets a file listing from the server from the current working directory. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <returns>An array of FtpListItem objects</returns>
		public FtpListItem[] GetListing() {
			return GetListing(null, 0);
		}

		/// <summary>
		/// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>An array of FtpListItem objects</returns>
		public FtpListItem[] GetListing(string path) {
			return GetListing(path, 0);
		}

		/// <summary>
		/// Gets a file listing from the server. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <returns>An array of FtpListItem objects</returns>
		public FtpListItem[] GetListing(string path, FtpListOption options) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				return GetListingRecursive(GetAbsolutePath(path), options);
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetListing), new object[] { path, options });

			FtpListItem item = null;
			var lst = new List<FtpListItem>();
			List<string> rawlisting = null;
			string listcmd = null;
			string buf = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isDerefLinks = options.HasFlag(FtpListOption.DerefLinks);
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = GetAbsolutePath(path);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

#if !CORE14
			lock (m_lock) {
#endif
				rawlisting = GetListingInternal(listcmd, options, true);
#if !CORE14
			}
#endif

			for (var i = 0; i < rawlisting.Count; i++) {
				buf = rawlisting[i];

				if (isNameList) {
					// if NLST was used we only have a file name so
					// there is nothing to parse.
					item = new FtpListItem() {
						FullName = buf
					};

					if (DirectoryExists(item.FullName)) {
						item.Type = FtpObjectType.Directory;
					}
					else {
						item.Type = FtpObjectType.File;
					}

					lst.Add(item);
				}
				else {

					// load basic information available within the file listing
					if (!LoadBasicListingInfo(ref path, ref item, lst, rawlisting, ref i, listcmd, buf, isRecursive, isIncludeSelf, machineList)) {

						// skip unwanted listings
						continue;
					}
				}

				// load extended information that wasn't available if the list options flags say to do so.
				if (item != null) {
					// try to dereference symbolic links if the appropriate list
					// option was passed
					if (item.Type == FtpObjectType.Link && isDerefLinks) {
						item.LinkObject = DereferenceLink(item);
					}

					// if need to get file modified date
					if (isGetModified && HasFeature(FtpCapability.MDTM)) {
						// if the modified date was not loaded or the modified date is more than a day in the future 
						// and the server supports the MDTM command, load the modified date.
						// most servers do not support retrieving the modified date
						// of a directory but we try any way.
						if (item.Modified == DateTime.MinValue || listcmd.StartsWith("LIST")) {
							DateTime modify;

							if (item.Type == FtpObjectType.Directory) {
								LogStatus(FtpTraceLevel.Verbose, "Trying to retrieve modification time of a directory, some servers don't like this...");
							}

							if ((modify = GetModifiedTime(item.FullName)) != DateTime.MinValue) {
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
								item.Size = GetFileSize(item.FullName);
							}
							else {
								item.Size = 0;
							}
						}
					}
				}
			}

			return lst.ToArray();
		}

		private bool LoadBasicListingInfo(ref string path, ref FtpListItem item, List<FtpListItem> lst, List<string> rawlisting, ref int i, string listcmd, string buf, bool isRecursive, bool isIncludeSelf, bool machineList) {

			// if this is a result of LIST -R then the path will be spit out
			// before each block of objects
			if (listcmd.StartsWith("LIST") && isRecursive) {
				if (buf.StartsWith("/") && buf.EndsWith(":")) {
					path = buf.TrimEnd(':');
					return false;
				}
			}

			// if the next line in the listing starts with spaces
			// it is assumed to be a continuation of the current line
			if (i + 1 < rawlisting.Count && (rawlisting[i + 1].StartsWith("\t") || rawlisting[i + 1].StartsWith(" "))) {
				buf += rawlisting[++i];
			}

			try {
				item = m_listParser.ParseSingleLine(path, buf, m_capabilities, machineList);
			}
			catch (FtpListParseException) {
				LogStatus(FtpTraceLevel.Verbose, "Restarting parsing from first entry in list");
				i = -1;
				lst.Clear();
				return false;
			}

			// FtpListItem.Parse() returns null if the line
			// could not be parsed
			if (item != null) {
				if (isIncludeSelf || !IsItemSelf(path, item)) {
					lst.Add(item);
				}
				else {
					//this.LogStatus(FtpTraceLevel.Verbose, "Skipped self or parent item: " + item.Name);
				}
			}
			else if (ServerHandler != null && !ServerHandler.SkipParserErrorReport()) {
				LogStatus(FtpTraceLevel.Warn, "Failed to parse file listing: " + buf);
			}
			return true;
		}

		private bool IsItemSelf(string path, FtpListItem item) {
			return item.Name == "." ||
				item.Name == ".." ||
				item.SubType == FtpObjectSubType.ParentDirectory ||
				item.SubType == FtpObjectSubType.SelfDirectory ||
				item.FullName.EnsurePostfix("/") == path;
		}

		private void CalculateGetListingCommand(string path, FtpListOption options, out string listcmd, out bool machineList) {

			// read flags
			var isForceList = options.HasFlag(FtpListOption.ForceList);
			var isUseStat = options.HasFlag(FtpListOption.UseStat);
			var isNoPath = options.HasFlag(FtpListOption.NoPath);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isUseLS = options.HasFlag(FtpListOption.UseLS);
			var isAllFiles = options.HasFlag(FtpListOption.AllFiles);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;

			machineList = false;

			// use stat listing if forced
			if (isUseStat) {
				listcmd = "STAT -l";
			}
			else {
				// use machine listing if supported by the server
				if (!isForceList && ListingParser == FtpParser.Machine && HasFeature(FtpCapability.MLSD)) {
					listcmd = "MLSD";
					machineList = true;
				}
				else {
					// otherwise use one of the legacy name listing commands
					if (isUseLS) {
						listcmd = "LS";
					}
					else if (isNameList) {
						listcmd = "NLST";
					}
					else {
						var listopts = "";

						listcmd = "LIST";

						// add option flags
						if (isAllFiles) {
							listopts += "a";
						}

						if (isRecursive) {
							listopts += "R";
						}

						if (listopts.Length > 0) {
							listcmd += " -" + listopts;
						}
					}
				}
			}

			if (!isNoPath) {
				listcmd = listcmd + " " + path.GetFtpPath();
			}
		}

		private bool IsServerSideRecursionSupported(FtpListOption options) {

			// Fix #539: Correctly calculate if server-side recursion is supported else fallback to manual recursion

			// check if the connected FTP server supports recursion in the first place
			if (RecursiveList) {

				// read flags
				var isForceList = options.HasFlag(FtpListOption.ForceList);
				var isUseStat = options.HasFlag(FtpListOption.UseStat);
				var isNameList = options.HasFlag(FtpListOption.NameList);
				var isUseLS = options.HasFlag(FtpListOption.UseLS);

				// if not using STAT listing
				if (!isUseStat) {

					// if not using machine listing (MSLD)
					if ((!isForceList || ListingParser == FtpParser.Machine) && HasFeature(FtpCapability.MLSD)) {
					}
					else {

						// if not using legacy list (LS) and name listing (NSLT)
						if (!isUseLS && !isNameList) {

							// only supported if using LIST
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Get the records of a file listing and retry if temporary failure.
		/// </summary>
		private List<string> GetListingInternal(string listcmd, FtpListOption options, bool retry) {
			var rawlisting = new List<string>();
			var isUseStat = options.HasFlag(FtpListOption.UseStat);

			// always get the file listing in binary to avoid character translation issues with ASCII.
			SetDataTypeNoLock(ListingDataType);

			try {
				// read in raw file listing from control stream
				if (isUseStat) {
					var reply = Execute(listcmd);
					if (reply.Success) {
						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

						foreach (var line in reply.InfoMessages.Split('\n')) {
							if (!Strings.IsNullOrWhiteSpace(line)) {
								rawlisting.Add(line);
								LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
							}
						}

						LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
					}
				}
				else {
					// read in raw file listing from data stream
					using (var stream = OpenDataStream(listcmd, 0)) {
						try {
							LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

							if (BulkListing) {
								// increases performance of GetListing by reading multiple lines of the file listing at once
								foreach (var line in stream.ReadAllLines(Encoding, BulkListingLength)) {
									if (!Strings.IsNullOrWhiteSpace(line)) {
										rawlisting.Add(line);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
									}
								}
							}
							else {
								// GetListing will read file listings line-by-line (actually byte-by-byte)
								string buf;
								while ((buf = stream.ReadLine(Encoding)) != null) {
									if (buf.Length > 0) {
										rawlisting.Add(buf);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + buf);
									}
								}
							}

							LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
						}
						finally {
							stream.Close();
						}
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
				// Some FTP servers forcibly close the connection, we absorb these errors

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.IsKnownError(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					LogLine(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to unexpected EOF");
					return GetListingInternal(listcmd, options, false);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}

#if NET50_OR_LATER
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
		public async IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, FtpListOption options, CancellationToken token = default(CancellationToken), [EnumeratorCancellation] CancellationToken enumToken = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				await foreach (FtpListItem i in GetListingRecursiveAsyncEnumerable(GetAbsolutePath(path), options, token, enumToken)) {
					yield return i;
				}

				yield break;
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetListingAsync), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isDerefLinks = options.HasFlag(FtpListOption.DerefLinks);
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			// read in raw file listing
			rawlisting = await GetListingInternalAsync(listcmd, options, true, token);

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
					isIncludeSelf, isNameList, isRecursive, isDerefLinks, isGetModified, isGetSize
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
		public IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingAsyncEnumerable(path, 0, token, enumToken);
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
		public IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingAsyncEnumerable(null, token, enumToken);
		}

#endif


#if ASYNC
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
		public async Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				return await GetListingRecursiveAsync(GetAbsolutePath(path), options, token);
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetListingAsync), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isDerefLinks = options.HasFlag(FtpListOption.DerefLinks);
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			// read in raw file listing
			rawlisting = await GetListingInternalAsync(listcmd, options, true, token);

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
					isIncludeSelf, isNameList, isRecursive, isDerefLinks, isGetModified, isGetSize
				);

			}
			return lst.ToArray();
		}

		private async Task<FtpListItem> GetListingProcessItemAsync(FtpListItem item, List<FtpListItem> lst, string rawEntry, string listcmd, CancellationToken token, bool isIncludeSelf, bool isNameList, bool isRecursive, bool isDerefLinks, bool isGetModified, bool isGetSize) {

			if (isNameList) {
				// if NLST was used we only have a file name so
				// there is nothing to parse.
				item = new FtpListItem() {
					FullName = rawEntry
				};

				if (await DirectoryExistsAsync(item.FullName, token)) {
					item.Type = FtpObjectType.Directory;
				}
				else {
					item.Type = FtpObjectType.File;
				}
				lst.Add(item);
			}

			// load extended information that wasn't available if the list options flags say to do so.
			if (item != null) {
				// try to dereference symbolic links if the appropriate list
				// option was passed
				if (item.Type == FtpObjectType.Link && isDerefLinks) {
					item.LinkObject = await DereferenceLinkAsync(item, token);
				}

				// if need to get file modified date
				if (isGetModified && HasFeature(FtpCapability.MDTM)) {
					// if the modified date was not loaded or the modified date is more than a day in the future 
					// and the server supports the MDTM command, load the modified date.
					// most servers do not support retrieving the modified date
					// of a directory but we try any way.
					if (item.Modified == DateTime.MinValue || listcmd.StartsWith("LIST")) {
						DateTime modify;

						if (item.Type == FtpObjectType.Directory) {
							LogStatus(FtpTraceLevel.Verbose, "Trying to retrieve modification time of a directory, some servers don't like this...");
						}

						if ((modify = await GetModifiedTimeAsync(item.FullName, token: token)) != DateTime.MinValue) {
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
							item.Size = await GetFileSizeAsync(item.FullName, -1, token);
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
		private async Task<List<string>> GetListingInternalAsync(string listcmd, FtpListOption options, bool retry, CancellationToken token) {
			var rawlisting = new List<string>();
			var isUseStat = options.HasFlag(FtpListOption.UseStat);

			// always get the file listing in binary to avoid character translation issues with ASCII.
			await SetDataTypeNoLockAsync(ListingDataType, token);

			try {

				// read in raw file listing from control stream
				if (isUseStat) {
					var reply = await ExecuteAsync(listcmd, token);
					if (reply.Success) {

						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

						foreach (var line in reply.InfoMessages.Split('\n')) {
							if (!Strings.IsNullOrWhiteSpace(line)) {
								rawlisting.Add(line);
								LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
							}
						}

						LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
					}
				}
				else {

					// read in raw file listing from data stream
					using (FtpDataStream stream = await OpenDataStreamAsync(listcmd, 0, token)) {
						try {
							LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

							if (BulkListing) {
								// increases performance of GetListing by reading multiple lines of the file listing at once
								foreach (var line in await stream.ReadAllLinesAsync(Encoding, BulkListingLength, token)) {
									if (!Strings.IsNullOrWhiteSpace(line)) {
										rawlisting.Add(line);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
									}
								}
							}
							else {
								// GetListing will read file listings line-by-line (actually byte-by-byte)
								string buf;
								while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
									if (buf.Length > 0) {
										rawlisting.Add(buf);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + buf);
									}
								}
							}

							LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
						}
						finally {
							stream.Close();
						}
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
				// Some FTP servers forcibly close the connection, we absorb these errors

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.IsKnownError(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					LogLine(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to unexpected EOF");
					return await GetListingInternalAsync(listcmd, options, false, token);
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
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public Task<FtpListItem[]> GetListingAsync(string path, CancellationToken token = default(CancellationToken)) {
			return GetListingAsync(path, 0, token);
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
		public Task<FtpListItem[]> GetListingAsync(CancellationToken token = default(CancellationToken)) {
			return GetListingAsync(null, token);
		}

#endif

		#endregion

		#region Get Listing Recursive

		/// <summary>
		/// Recursive method of GetListing, to recurse through directories on servers that do not natively support recursion.
		/// Automatically called by GetListing where required.
		/// Uses flat recursion instead of head recursion.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <returns>An array of FtpListItem objects</returns>
		protected FtpListItem[] GetListingRecursive(string path, FtpListOption options) {
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
				var items = GetListing(currentPath, options);

				// add it to the final listing
				allFiles.AddRange(items);

				// extract the directories
				foreach (var item in items) {
					if (item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..") {
						stack.Push(item.FullName);
					}
				}

				items = null;

				// recurse
			}

			// final list of all files and dirs
			return allFiles.ToArray();
		}

#if NET50_OR_LATER
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

		protected async IAsyncEnumerable<FtpListItem> GetListingRecursiveAsyncEnumerable(string path, FtpListOption options, CancellationToken token, [EnumeratorCancellation] CancellationToken enumToken = default) {
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
				await foreach (var item in GetListingAsyncEnumerable(currentPath, options, token)) {
					// break if task is cancelled
					token.ThrowIfCancellationRequested();

					if (item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..") {
						stack.Push(item.FullName);
					}

					yield return item;
				}

				// recurse
			}
		}
#endif


#if ASYNC
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
		protected async Task<FtpListItem[]> GetListingRecursiveAsync(string path, FtpListOption options, CancellationToken token) {

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
				FtpListItem[] items = await GetListingAsync(currentPath, options, token);

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				// add it to the final listing
				allFiles.AddRange(items);

				// extract the directories
				foreach (var item in items) {
					if (item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..") {
						stack.Push(item.FullName);
					}
				}

				items = null;

				// recurse
			}

			// final list of all files and dirs
			return allFiles.ToArray();
		}
#endif

		#endregion

		#region Get Name Listing

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		public string[] GetNameListing() {
			return GetNameListing(null);
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		public string[] GetNameListing(string path) {

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetNameListing), new object[] { path });

			var listing = new List<string>();

			path = GetAbsolutePath(path);

#if !CORE14
			lock (m_lock) {
#endif

				// always get the file listing in binary to avoid character translation issues with ASCII.
				SetDataTypeNoLock(ListingDataType);

				// read in raw listing
				try {
					using (var stream = OpenDataStream("NLST " + path, 0)) {
						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
						string line;

						try {
							while ((line = stream.ReadLine(Encoding)) != null) {
								listing.Add(line);
								LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
							}
						}
						finally {
							stream.Close();
						}
						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
					}
				}
				catch (FtpMissingSocketException) {
					// Some FTP server does not send any response when listing an empty directory
					// and the connection fails because no communication socket is provided by the server
				}
				catch (FtpCommandException ftpEx) {
					// Some FTP servers throw 550 for empty folders. Absorb these.
					if (ftpEx.CompletionCode == null || !ftpEx.CompletionCode.StartsWith("550")) {
						throw ftpEx;
					}
				}
				catch (IOException) {
					// Some FTP servers forcibly close the connection, we absorb these errors
				}

#if !CORE14
			}
#endif

			return listing.ToArray();
		}

#if !ASYNC
		private delegate string[] AsyncGetNameListing(string path);

		/// <summary>
		/// Begin an asynchronous operation to return a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetNameListing(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetNameListing func;

			lock (m_asyncmethods) {
				ar = (func = new AsyncGetNameListing(GetNameListing)).BeginInvoke(path, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Begin an asynchronous operation to return a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetNameListing(AsyncCallback callback, object state) {
			return BeginGetNameListing(null, callback, state);
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginGetNameListing"/>
		/// </summary>
		/// <param name="ar">IAsyncResult object returned from <see cref="o:BeginGetNameListing"/></param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public string[] EndGetNameListing(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetNameListing>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public async Task<string[]> GetNameListingAsync(string path, CancellationToken token = default(CancellationToken)) {

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetNameListingAsync), new object[] { path });

			var listing = new List<string>();

			path = await GetAbsolutePathAsync(path, token);

			// always get the file listing in binary to avoid character translation issues with ASCII.
			await SetDataTypeNoLockAsync(ListingDataType, token);

			// read in raw listing
			try {
				using (FtpDataStream stream = await OpenDataStreamAsync("NLST " + path, 0, token)) {
					LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
					string line;

					try {
						while ((line = await stream.ReadLineAsync(Encoding, token)) != null) {
							listing.Add(line);
							LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
						}
					}
					finally {
						stream.Close();
					}
					LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
				}
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when listing an empty directory
				// and the connection fails because no communication socket is provided by the server
			}
			catch (FtpCommandException ftpEx) {
				// Some FTP servers throw 550 for empty folders. Absorb these.
				if (ftpEx.CompletionCode == null || !ftpEx.CompletionCode.StartsWith("550")) {
					throw ftpEx;
				}
			}
			catch (IOException) {
				// Some FTP servers forcibly close the connection, we absorb these errors
			}

			return listing.ToArray();
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public Task<string[]> GetNameListingAsync(CancellationToken token = default(CancellationToken)) {
			return GetNameListingAsync(null, token);
		}
#endif

		#endregion
	}
}