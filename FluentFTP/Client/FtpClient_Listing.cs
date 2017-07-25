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
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX)
using System.Threading;
#endif
#if NETFX45
using System.Threading.Tasks;
#endif

namespace FluentFTP {

	/// <summary>
	/// FTP Control Connection. Speaks the FTP protocol with the server and
	/// provides facilities for performing transactions.
	/// 
	/// Debugging problems with FTP transactions is much easier to do when
	/// you can see exactly what is sent to the server and the reply 
	/// FluentFTP gets in return. Please review the Debug example
	/// below for information on how to add <see cref="System.Diagnostics.TraceListener"/>s for capturing
	/// the conversation between FluentFTP and the server.
	/// </summary>
	/// <example>The following example illustrates how to assist in debugging
	/// FluentFTP by getting a transaction log from the server.
	/// <code source="..\Examples\Debug.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates adding a custom file
	/// listing parser in the event that you encounter a list format
	/// not already supported.
	/// <code source="..\Examples\CustomParser.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to validate
	/// a SSL certificate when using SSL/TLS.
	/// <code source="..\Examples\ValidateCertificate.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file.
	/// <code source="..\Examples\OpenRead.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to download a file
	/// using a URI object.
	/// <code source="..\Examples\OpenReadURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file.
	/// <code source="..\Examples\OpenWrite.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to upload a file
	/// using a URI object.
	/// <code source="..\Examples\OpenWriteURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file.
	/// <code source="..\Examples\OpenAppend.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to append to a file
	/// using a URI object.
	/// <code source="..\Examples\OpenAppendURI.cs" lang="cs" />
	/// </example>
	/// <example>The following example demonstrates how to get a file
	/// listing from the server.
	/// <code source="..\Examples\GetListing.cs" lang="cs" />
	/// </example>
	public partial class FtpClient : IDisposable {

		#region Properties

		private FtpParser m_parser = FtpParser.Auto;
		/// <summary>
		/// File listing parser to be used. 
		/// Automatically calculated based on the type of the server, unless changed.
		/// </summary>
		public FtpParser ListingParser {
			get { return m_parser; }
			set {
				m_parser = value;

				// configure parser
				m_listParser.parser = value;
				m_listParser.parserConfirmed = false;
			}
		}

		private CultureInfo m_parserCulture = CultureInfo.InvariantCulture;
		/// <summary>
		/// Culture used to parse file listings
		/// </summary>
		public CultureInfo ListingCulture {
			get { return m_parserCulture; }
			set {
				m_parserCulture = value;

				// configure parser
				m_listParser.parserCulture = value;
			}
		}

		private double m_timeDiff = 0;
		/// <summary>
		/// Time difference between server and client, in hours.
		/// If the server is located in New York and you are in London then the time difference is -5 hours.
		/// </summary>
		public double TimeOffset {
			get { return m_timeDiff; }
			set {
				m_timeDiff = value;

				// configure parser
				int hours = (int)Math.Floor(m_timeDiff);
				int mins = (int)Math.Floor((m_timeDiff - Math.Floor(m_timeDiff)) * 60);
				m_listParser.timeOffset = new TimeSpan(hours, mins, 0);
				m_listParser.hasTimeOffset = m_timeDiff != 0;
			}
		}

		private bool m_recursiveList = true;

		/// <summary>
		/// Check if your server supports a recursive LIST command (LIST -R).
		/// If you know for sure that this is unsupported, set it to false.
		/// </summary>
		public bool RecursiveList {
			get {
				if (SystemType.StartsWith("Windows_CE")) {
					return false;
				}
				return m_recursiveList;
			}
			set {
				m_recursiveList = value;
			}
		}

		private bool m_bulkListing = true;

		/// <summary>
		/// If true, increases performance of GetListing by reading multiple lines
		/// of the file listing at once. If false then GetListing will read file
		/// listings line-by-line. If GetListing is having issues with your server,
		/// set it to false.
		/// 
		/// The number of bytes read is based upon <see cref="BulkListingLength"/>.
		/// </summary>
		public bool BulkListing {
			get {
				return m_bulkListing;
			}
			set {
				m_bulkListing = value;
			}
		}

		private int m_bulkListingLength = 128;

		/// <summary>
		/// Bytes to read during GetListing. Only honored if <see cref="BulkListing"/> is true.
		/// </summary>
		public int BulkListingLength {
			get {
				return m_bulkListingLength;
			}
			set {
				m_bulkListingLength = value;
			}
		}

		#endregion

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
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");
			
			FtpTrace.WriteFunc("GetObjectInfo", new object[] { path, dateModified });

			FtpReply reply;
			string[] res;

			bool supportsMachineList = (Capabilities & FtpCapability.MLSD) == FtpCapability.MLSD;

			FtpListItem result = null;

			if (supportsMachineList) {

				// USE MACHINE LISTING TO GET INFO FOR A SINGLE FILE

				if ((reply = Execute("MLST " + path)).Success) {
					res = reply.InfoMessages.Split('\n');
					if (res.Length > 1) {
						string info = "";

						for (int i = 1; i < res.Length; i++) {
							info += res[i];
						}

						result = m_listParser.ParseSingleLine(null, info, m_caps, true);
					}
				} else {
					FtpTrace.WriteStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " with error " + reply.ErrorMessage);
				}
			} else {

				// USE GETLISTING TO GET ALL FILES IN DIR .. SLOWER BUT AT LEAST IT WORKS

				string dirPath = path.GetFtpDirectoryName();
				FtpListItem[] dirItems = GetListing(dirPath);

				foreach (var dirItem in dirItems) {
					if (dirItem.FullName == path) {
						result = dirItem;
						break;
					}
				}

				FtpTrace.WriteStatus(FtpTraceLevel.Warn, "Failed to get object info for path " + path + " since MLST not supported and GetListing() fails to list file/folder.");
			}

			// Get the accurate date modified using another MDTM command
			if (result != null && dateModified && HasFeature(FtpCapability.MDTM)) {
				result.Modified = GetModifiedTime(path);
			}

			return result;
		}

#if !CORE
		delegate FtpListItem AsyncGetObjectInfo(string path, bool dateModified);

		/// <summary>
		/// Begins an asynchronous operation to return information about a remote file system object. 
		/// </summary>
		/// <remarks>
		/// You should check the <see cref="Capabilities"/> property for the <see cref="FtpCapability.MLSD"/> 
		/// flag before calling this method. Failing to do so will result in an InvalidOperationException
		///  being thrown when the server does not support machine listings. Returns null if the server response can't
		/// be parsed or the server returns a failure completion code. The error for a failure
		/// is logged with FtpTrace. No exception is thrown on error because that would negate
		/// the usefulness of this method for checking for the existence of an object.
		/// </remarks>
		/// <param name="path">Path of the file or folder</param>
		/// <param name="dateModified">Get the accurate modified date using another MDTM command</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		public IAsyncResult BeginGetObjectInfo(string path, bool dateModified, AsyncCallback callback, object state) {

			IAsyncResult ar;
			AsyncGetObjectInfo func;

			ar = (func = new AsyncGetObjectInfo(GetObjectInfo)).BeginInvoke(path, dateModified, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginGetObjectInfo"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginGetObjectInfo"/></param>
		/// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public FtpListItem EndGetObjectInfo(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetObjectInfo>(ar).EndInvoke(ar);
		}

#endif
#if NETFX45
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
		/// <exception cref="InvalidOperationException">Thrown if the server does not support this Capability</exception>
		/// <returns>A <see cref="FtpListItem"/> if the command succeeded, or null if there was a problem.</returns>
		public async Task<FtpListItem> GetObjectInfoAsync(string path, bool dateModified = false) {

			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, bool, FtpListItem>(
				(p, dm, ac, s) => BeginGetObjectInfo(p, dm, ac, s),
				ar => EndGetObjectInfo(ar),
				path, dateModified, null);
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
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
		public FtpListItem[] GetListing() {
			return GetListing(null);
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
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
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
		/// <param name="options">Options that dictacte how a list is performed and what information is gathered.</param>
		/// <returns>An array of FtpListItem objects</returns>
		/// <example><code source="..\Examples\GetListing.cs" lang="cs" /></example>
		public FtpListItem[] GetListing(string path, FtpListOption options) {

			FtpTrace.WriteFunc("GetListing", new object[] { path, options });

			FtpListItem item = null;
			List<FtpListItem> lst = new List<FtpListItem>();
			List<string> rawlisting = new List<string>();
			string listcmd = null;
			string buf = null;

			// read flags
			bool isIncludeSelf = (options & FtpListOption.IncludeSelfAndParent) == FtpListOption.IncludeSelfAndParent;
			bool isForceList = (options & FtpListOption.ForceList) == FtpListOption.ForceList;
			bool isNoPath = (options & FtpListOption.NoPath) == FtpListOption.NoPath;
			bool isNameList = (options & FtpListOption.NameList) == FtpListOption.NameList;
			bool isUseLS = (options & FtpListOption.UseLS) == FtpListOption.UseLS;
			bool isAllFiles = (options & FtpListOption.AllFiles) == FtpListOption.AllFiles;
			bool isRecursive = (options & FtpListOption.Recursive) == FtpListOption.Recursive && RecursiveList;
			bool isDerefLinks = (options & FtpListOption.DerefLinks) == FtpListOption.DerefLinks;
			bool isGetModified = (options & FtpListOption.Modify) == FtpListOption.Modify;
			bool isGetSize = (options & FtpListOption.Size) == FtpListOption.Size;

			// calc path to request
			path = GetAbsolutePath(path);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList = false;
			if ((!isForceList || m_parser == FtpParser.Machine) && HasFeature(FtpCapability.MLSD)) {
				listcmd = "MLSD";
				machineList = true;
			} else {
				if (isUseLS) {
					listcmd = "LS";
				} else if (isNameList) {
					listcmd = "NLST";
				} else {
					string listopts = "";

					listcmd = "LIST";

					if (isAllFiles)
						listopts += "a";

					if (isRecursive)
						listopts += "R";

					if (listopts.Length > 0)
						listcmd += " -" + listopts;
				}
			}

			if (!isNoPath) {
				listcmd = (listcmd + " " + path.GetFtpPath());
			}

#if !CORE14
			lock (m_lock) {
#endif
				Execute("TYPE I");

				// read in raw file listing
				using (FtpDataStream stream = OpenDataStream(listcmd, 0)) {
					try {
						FtpTrace.WriteLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

						if (this.BulkListing) {

							// increases performance of GetListing by reading multiple lines of the file listing at once
							foreach (var line in stream.ReadAllLines(Encoding, this.BulkListingLength)) {
								if (!FtpExtensions.IsNullOrWhiteSpace(line)) {
									rawlisting.Add(line);
									FtpTrace.WriteLine(FtpTraceLevel.Verbose, "Listing:  " + line);
								}
							}

						} else {

							// GetListing will read file listings line-by-line (actually byte-by-byte)
							while ((buf = stream.ReadLine(Encoding)) != null) {
								if (buf.Length > 0) {
									rawlisting.Add(buf);
									FtpTrace.WriteLine(FtpTraceLevel.Verbose, "Listing:  " + buf);
								}
							}
						}

						FtpTrace.WriteLine(FtpTraceLevel.Verbose, "-----------------------------------------");

					} finally {
						stream.Close();
					}
				}
#if !CORE14
			}
#endif

			for (int i = 0; i < rawlisting.Count; i++) {
				buf = rawlisting[i];

				if (isNameList) {

					// if NLST was used we only have a file name so
					// there is nothing to parse.
					item = new FtpListItem() {
						FullName = buf
					};

					if (DirectoryExists(item.FullName))
						item.Type = FtpFileSystemObjectType.Directory;
					else
						item.Type = FtpFileSystemObjectType.File;

					lst.Add(item);

				} else {

					// if this is a result of LIST -R then the path will be spit out
					// before each block of objects
					if (listcmd.StartsWith("LIST") && isRecursive) {
						if (buf.StartsWith("/") && buf.EndsWith(":")) {
							path = buf.TrimEnd(':');
							continue;
						}
					}

					// if the next line in the listing starts with spaces
					// it is assumed to be a continuation of the current line
					if (i + 1 < rawlisting.Count && (rawlisting[i + 1].StartsWith("\t") || rawlisting[i + 1].StartsWith(" ")))
						buf += rawlisting[++i];

					try {
						item = m_listParser.ParseSingleLine(path, buf, m_caps, machineList);
					} catch (FtpListParser.CriticalListParseException) {
						FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Restarting parsing from first entry in list");
						i = -1;
						lst.Clear();
						continue;
					}

					// FtpListItem.Parse() returns null if the line
					// could not be parsed
					if (item != null) {
						if (isIncludeSelf || !(item.Name == "." || item.Name == "..")) {
							lst.Add(item);
						} else {
							//FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Skipped self or parent item: " + item.Name);
						}
					} else {
						FtpTrace.WriteStatus(FtpTraceLevel.Warn, "Failed to parse file listing: " + buf);
					}
				}

				// load extended information that wasn't available if the list options flags say to do so.
				if (item != null) {

					// try to dereference symbolic links if the appropriate list
					// option was passed
					if (item.Type == FtpFileSystemObjectType.Link && isDerefLinks) {
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

							if (item.Type == FtpFileSystemObjectType.Directory)
								FtpTrace.WriteStatus(FtpTraceLevel.Verbose, "Trying to retrieve modification time of a directory, some servers don't like this...");

							if ((modify = GetModifiedTime(item.FullName)) != DateTime.MinValue)
								item.Modified = modify;
						}
					}

					// if need to get file size
					if (isGetSize && HasFeature(FtpCapability.SIZE)) {

						// if no size was parsed, the object is a file and the server
						// supports the SIZE command, then load the file size
						if (item.Size == -1) {
							if (item.Type != FtpFileSystemObjectType.Directory) {
								item.Size = GetFileSize(item.FullName);
							} else {
								item.Size = 0;
							}
						}
					}
				}
			}

			return lst.ToArray();
		}

#if !CORE
		/// <summary>
		/// Begins an asynchronous operation to get a file listing from the server. 
		/// Each <see cref="FtpListItem"/> object returned contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(AsyncCallback callback, Object state) {
			return BeginGetListing(null, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to get a file listing from the server. 
		/// Each <see cref="FtpListItem"/> object returned contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(string path, AsyncCallback callback, Object state) {
			return BeginGetListing(path, FtpListOption.Modify | FtpListOption.Size, callback, state);
		}

		delegate FtpListItem[] AsyncGetListing(string path, FtpListOption options);

		/// <summary>
		/// Gets a file listing from the server asynchronously
		/// </summary>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="callback">AsyncCallback method</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetListing(string path, FtpListOption options, AsyncCallback callback, Object state) {
			IAsyncResult ar;
			AsyncGetListing func;

			ar = (func = new AsyncGetListing(GetListing)).BeginInvoke(path, options, callback, state);
			lock (m_asyncmethods) {
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginGetListing"/>
		/// </summary>
		/// <param name="ar">IAsyncResult return from <see cref="o:BeginGetListing"/></param>
		/// <returns>An array of items retrieved in the listing</returns>
		/// <example><code source="..\Examples\BeginGetListing.cs" lang="cs" /></example>
		public FtpListItem[] EndGetListing(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetListing>(ar).EndInvoke(ar);
		}

#endif
#if NETFX45
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
		/// <returns>An array of items retrieved in the listing</returns>
		public async Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options) {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, FtpListOption, FtpListItem[]>(
				(p, o, ac, s) => BeginGetListing(p, o, ac, s),
				ar => EndGetListing(ar),
				path, options, null);
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
		/// <returns>An array of items retrieved in the listing</returns>
		public async Task<FtpListItem[]> GetListingAsync(string path) {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, FtpListItem[]>(
				(p, ac, s) => BeginGetListing(p, ac, s),
				ar => EndGetListing(ar),
				path, null);
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
		public async Task<FtpListItem[]> GetListingAsync() {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<FtpListItem[]>(
				(ac, s) => BeginGetListing(ac, s),
				ar => EndGetListing(ar),
				null);
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
		/// <example><code source="..\Examples\GetNameListing.cs" lang="cs" /></example>
		public string[] GetNameListing(string path) {

			FtpTrace.WriteFunc("GetNameListing", new object[] { path });

			List<string> listing = new List<string>();

			// calc path to request
			path = GetAbsolutePath(path);

#if !CORE14
			lock (m_lock) {
#endif
				// always get the file listing in binary
				// to avoid any potential character translation
				// problems that would happen if in ASCII.
				Execute("TYPE I");

				using (FtpDataStream stream = OpenDataStream(("NLST " + path.GetFtpPath()), 0)) {
					string buf;

					try {
						while ((buf = stream.ReadLine(Encoding)) != null)
							listing.Add(buf);
					} finally {
						stream.Close();
					}
				}
#if !CORE14
			}
#endif

			return listing.ToArray();
		}

#if !CORE
		delegate string[] AsyncGetNameListing(string path);

		/// <summary>
		/// Begin an asynchronous operation to return a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetNameListing(string path, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncGetNameListing func;

			ar = (func = new AsyncGetNameListing(GetNameListing)).BeginInvoke(path, callback, state);
			lock (m_asyncmethods) {
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
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public IAsyncResult BeginGetNameListing(AsyncCallback callback, object state) {
			return BeginGetNameListing(null, callback, state);
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginGetNameListing"/>
		/// </summary>
		/// <param name="ar">IAsyncResult object returned from <see cref="o:BeginGetNameListing"/></param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		/// <example><code source="..\Examples\BeginGetNameListing.cs" lang="cs" /></example>
		public string[] EndGetNameListing(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncGetNameListing>(ar).EndInvoke(ar);
		}

#endif
#if NETFX45
		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public async Task<string[]> GetNameListingAsync(string path) {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string, string[]>(
				(p, ac, s) => BeginGetNameListing(p, ac, s),
				ar => EndGetNameListing(ar),
				path, null);
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public async Task<string[]> GetNameListingAsync() {
			//TODO:  Rewrite as true async method with cancellation support
			return await Task.Factory.FromAsync<string[]>(
				(ac, s) => BeginGetNameListing(ac, s),
				ar => EndGetNameListing(ar),
				null);
		}
#endif

		#endregion

	}
}