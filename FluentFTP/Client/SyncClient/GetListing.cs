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
using FluentFTP.Proxy.SyncProxy;

namespace FluentFTP {
	public partial class FtpClient {


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

			LogFunction(nameof(GetListing), new object[] { path, options });

			FtpListItem item = null;
			var lst = new List<FtpListItem>();
			List<string> rawlisting = null;
			string listcmd = null;
			string buf = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = GetAbsolutePath(path);

			string pwdSave = string.Empty;

			var autoNav = Config.ShouldAutoNavigate(path);
			var autoRestore = Config.ShouldAutoRestore(path);

			if (autoNav) {
				options = options | FtpListOption.NoPath;
			}

			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			if (autoNav) {
				pwdSave = GetWorkingDirectory();
				if (pwdSave != path) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate to: \"" + path + "\"");
					SetWorkingDirectory(path);
				}
			}

			rawlisting = GetListingInternal(listcmd, options, true);

			if (autoRestore) {
				if (pwdSave != GetWorkingDirectory()) {
					LogWithPrefix(FtpTraceLevel.Verbose, "AutoNavigate-restore to: \"" + pwdSave + "\"");
					SetWorkingDirectory(pwdSave);
				}
			}

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

		/// <summary>
		/// Get the records of a file listing and retry if temporary failure.
		/// </summary>
		protected List<string> GetListingInternal(string listcmd, FtpListOption options, bool retry) {
			var rawlisting = new List<string>();
			var isUseStat = options.HasFlag(FtpListOption.UseStat);

			// Get the file listing in the desired format
			SetDataType(Config.ListingDataType);

			try {
				// read in raw file listing from control stream
				if (isUseStat) {
					var reply = Execute(listcmd);
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
						using (var stream = OpenDataStream(listcmd, 0)) {
							try {
								if (this is FtpClientSocks4Proxy || this is FtpClientSocks4aProxy) {
									// first 6 bytes contains 2 bytes of unknown (to me) purpose and 4 ip address bytes
									// we need to skip them otherwise they will be downloaded to the file
									// moreover, these bytes cause "Failed to get the EPSV port" error
									stream.Read(new byte[6], 0, 6);
								}

								Log(FtpTraceLevel.Verbose, "+---------------------------------------+");

								if (Config.BulkListing) {
									// increases performance of GetListing by reading multiple lines of the file listing at once
									foreach (var line in stream.ReadAllLines(Encoding, Config.BulkListingLength)) {
										if (!Strings.IsNullOrWhiteSpace(line)) {
											rawlisting.Add(line);
											Log(FtpTraceLevel.Verbose, "Listing:  " + line);
										}
									}
								}
								else {
									// GetListing will read file listings line-by-line (actually byte-by-byte)
									string buf;
									while ((buf = stream.ReadLine(Encoding)) != null) {
										if (buf.Length > 0) {
											rawlisting.Add(buf);
											Log(FtpTraceLevel.Verbose, "Listing:  " + buf);
										}
									}
								}

								Log(FtpTraceLevel.Verbose, "-----------------------------------------");
							}
							finally {
								stream.Close();
							}
						}
					}
					catch (AuthenticationException) {
						FtpReply reply = ((IInternalFtpClient)this).GetReplyInternal(listcmd, false, -1); // no exhaustNoop, but non-blocking
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
						return GetListingInternal(listcmd, options, false);
					}
					else {
						throw;
					}
				}

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.ContainsAnyCI(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					Log(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to unexpected EOF");
					return GetListingInternal(listcmd, options, false);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}


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
