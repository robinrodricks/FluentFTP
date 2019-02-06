using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#if NET45
using System.Threading.Tasks;
#endif

namespace FluentFTP {

	/// <summary>
	/// Parses a line from a file listing using the first successful parser, or the specified parser.
	/// Returns an FtpListItem object representing the parsed line, or null if the line was unable to be parsed.
	/// </summary>
	public class FtpListParser {

		#region Constants

		// DATE/TIME FORMATS
		private string[] unixDateFormats1 = { "MMM'-'d'-'yyyy", "MMM'-'dd'-'yyyy" };
		private string[] unixDateFormats2 = { "MMM'-'d'-'yyyy'-'HH':'mm", "MMM'-'dd'-'yyyy'-'HH':'mm", "MMM'-'d'-'yyyy'-'H':'mm", "MMM'-'dd'-'yyyy'-'H':'mm", "MMM'-'dd'-'yyyy'-'H'.'mm" };
		private string[] unixAltDateFormats1 = { "MMM'-'d'-'yyyy", "MMM'-'dd'-'yyyy" };
		private string[] unixAltDateFormats2 = { "MMM'-'d'-'yyyy'-'HH':'mm:ss", "MMM'-'dd'-'yyyy'-'HH':'mm:ss", "MMM'-'d'-'yyyy'-'H':'mm:ss", "MMM'-'dd'-'yyyy'-'H':'mm:ss" };
		private string[] windowsDateFormats = { "MM'-'dd'-'yy hh':'mmtt", "MM'-'dd'-'yy HH':'mm", "MM'-'dd'-'yyyy hh':'mmtt" };
		private string[][] ibmDateFormats = { new string[] { "dd'/'MM'/'yy' 'HH':'mm':'ss", "dd'/'MM'/'yyyy' 'HH':'mm':'ss", "dd'.'MM'.'yy' 'HH':'mm':'ss" }, new string[] { "yy'/'MM'/'dd' 'HH':'mm':'ss", "yyyy'/'MM'/'dd' 'HH':'mm':'ss", "yy'.'MM'.'dd' 'HH':'mm':'ss" }, new string[] { "MM'/'dd'/'yy' 'HH':'mm':'ss", "MM'/'dd'/'yyyy' 'HH':'mm':'ss", "MM'.'dd'.'yy' 'HH':'mm':'ss" } };
		private string[] nonstopDateFormats = { "d'-'MMM'-'yy HH':'mm':'ss" };

		// FIELDS REQUIRED
		private static int MIN_EXPECTED_FIELD_COUNT_UNIX = 7;
		private static int MIN_EXPECTED_FIELD_COUNT_UNIXALT = 8;
		private static int MIN_EXPECTED_FIELD_COUNT_VMS = 4;
		private static int MIN_EXPECTED_FIELD_COUNT_OS400 = 5;
		private static int MIN_EXPECTED_FIELD_COUNT_TANDEM = 7;

		// UNIX
		private static string SYMLINK_ARROW = "->";
		private static char SYMLINK_CHAR = 'l';
		private static char ORDINARY_FILE_CHAR = '-';
		private static char DIRECTORY_CHAR = 'd';

		// WINDOWS
		private static string WIN_DIR = "<DIR>";
		private static char[] WIN_SEP = { ' ' };
		private static int MIN_EXPECTED_FIELD_COUNT_WIN = 4;

		// VMS
		private static string VMS_DIR = ".DIR";
		private static string VMS_HDR = "Directory";
		private static string VMS_TOTAL = "Total";
		private static int DEFAULT_BLOCKSIZE = 512 * 1024;

		// IBM
		private static string IBM_DIR = "*DIR";
		private static string IBM_DDIR = "*DDIR";
		private static string IBM_MEM = "*MEM";
	    private static string IBM_FILE = "*FILE";

        // NONSTOP
        private static char[] NONSTOP_TRIM = { '"' };

		#endregion

		#region API

		/// <summary>
		/// the FTP connection that owns this parser
		/// </summary>
		public FtpClient client;

		private static List<FtpParser> parsers = new List<FtpParser>{
			FtpParser.Unix, FtpParser.Windows, FtpParser.IBM, FtpParser.VMS, FtpParser.NonStop
		};

		/// <summary>
		/// which server type? (SYST)
		/// </summary>
		public string system;

		/// <summary>
		/// current parser, or parser set by user
		/// </summary>
		public FtpParser parser = FtpParser.Auto;

		/// <summary>
		/// parser calculated based on system type (SYST command)
		/// </summary>
		public FtpParser detectedParser = FtpParser.Auto;

		/// <summary>
		/// if we have detected that the current parser is valid
		/// </summary>
		public bool parserConfirmed = false;

		/// <summary>
		/// which culture to read filenames with?
		/// </summary>
		public CultureInfo parserCulture = CultureInfo.InvariantCulture;

		/// <summary>
		/// what is the time offset between server/client?
		/// </summary>
		public TimeSpan timeOffset = new TimeSpan();

		/// <summary>
		/// any time offset between server/client?
		/// </summary>
		public bool hasTimeOffset = false;

		/// <summary>
		/// VMS ONLY : the blocksize used to calculate the file
		/// </summary>
		public int vmsBlocksize = DEFAULT_BLOCKSIZE;

		/// <summary>
		/// Is the version number returned as part of the filename?
		/// 
		/// Some VMS FTP servers do not permit a file to be deleted unless
		/// the filename includes the version number. Note that directories are
		/// never returned with the version number.
		/// </summary>
		public bool vmsNameHasVersion = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpListParser"/> class.
        /// </summary>
        /// <param name="client">An existing <see cref="FtpClient"/> object</param>
		public FtpListParser(FtpClient client) {
			this.client = client;
		}

		/// <summary>
		/// Try to auto-detect which parser is suitable given a system string.
		/// </summary>
		/// <param name="system">result of SYST command</param>
		public void Init(string system) {

			parserConfirmed = false;
			this.system = system != null ? system.Trim() : null;

			if (system != null) {
				if (system.ToUpper().StartsWith("WINDOWS")) {
					this.client.LogStatus(FtpTraceLevel.Info, "Auto-detected Windows listing parser");
					parser = FtpParser.Windows;
				} else if (system.ToUpper().IndexOf("UNIX") >= 0 || system.ToUpper().IndexOf("AIX") >= 0) {
					this.client.LogStatus(FtpTraceLevel.Info, "Auto-detected UNIX listing parser");
					parser = FtpParser.Unix;
				} else if (system.ToUpper().IndexOf("VMS") >= 0) {
					this.client.LogStatus(FtpTraceLevel.Info, "Auto-detected VMS listing parser");
					parser = FtpParser.VMS;
				} else if (system.ToUpper().IndexOf("OS/400") >= 0) {
					this.client.LogStatus(FtpTraceLevel.Info, "Auto-detected OS/400 listing parser");
					parser = FtpParser.IBM;
				} else {
					parser = FtpParser.Unix;
				    this.client.LogStatus(FtpTraceLevel.Warn, "Cannot auto-detect listing parser for system '" + system + "', using Unix parser");
				}
			}

			detectedParser = parser;
		}

		/// <summary>
		/// Parse raw file list from server into file objects, using the currently active parser.
		/// </summary>
		public FtpListItem[] ParseMultiLine(string[] fileStrings, bool isMachineList) {

			FtpListItem[] files = new FtpListItem[fileStrings.Length];

			if (fileStrings.Length == 0) {
				return files;
			}

		    this.client.LogStatus(FtpTraceLevel.Verbose, "Parse() called using culture: " + parserCulture.EnglishName);

			ValidateParser(fileStrings);

			int count = 0;
			for (int i = 0; i < fileStrings.Length; i++) {
				if (fileStrings[i] == null || fileStrings[i].Trim().Length == 0)
					continue;

				try {

					// MULTI LINE LISTINGS
					FtpListItem file = null;
					if (IsMultiLine(parser)) {
						StringBuilder filename = new StringBuilder(fileStrings[i]);
						while (i + 1 < fileStrings.Length && fileStrings[i + 1].IndexOf(';') < 0) {
							filename.Append(" ").Append(fileStrings[i + 1]);
							i++;
						}
						file = ParseSingleLine(null, filename.ToString(), FtpCapability.NONE, isMachineList);
					} else {

						// SINGLE LINE LISTINGS
						file = ParseSingleLine(null, fileStrings[i], FtpCapability.NONE, isMachineList);
					}

					// skip blank lines
					if (file != null) {
						files[count++] = file;
					}

				} catch (CriticalListParseException) {
				    this.client.LogStatus(FtpTraceLevel.Verbose, "Restarting parsing from first entry in list");
					i = -1;
					count = 0;
					continue;
				}
			}
			FtpListItem[] result = new FtpListItem[count];
			Array.Copy(files, 0, result, 0, count);
			return result;
		}

		private bool IsMultiLine(FtpParser p) {
			return p == FtpParser.VMS;
		}

		/// <summary>
		/// Parse raw file from server into a file object, using the currently active parser.
		/// </summary>
		public FtpListItem ParseSingleLine(string path, string file, FtpCapability caps, bool isMachineList) {

			FtpListItem result = null;

			// force machine listing if it is
			if (isMachineList) {
				result = ParseMachineList(file, caps, client);
			} else {

				// use custom parser if given
				if (m_customParser != null) {
					result = m_customParser(file, caps, client);
				} else {

					if (IsWrongParser()) {
						ValidateParser(new string[] { file });
					}

					// use one of the in-built parsers
					switch (parser) {
						case FtpParser.Legacy:
							result = ParseLegacy(path, file, caps, client);
							break;
						case FtpParser.Machine:
							result = ParseMachineList(file, caps, client);
							break;
						case FtpParser.Windows:
							result = ParseWindows(file);
							break;
						case FtpParser.Unix:
							result = ParseUnix(file);
							break;
						case FtpParser.UnixAlt:
							result = ParseUnixAlt(file);
							break;
						case FtpParser.VMS:
							result = ParseVMS(file);
							break;
						case FtpParser.IBM:
							result = ParseIBM(file);
							break;
						case FtpParser.NonStop:
							result = ParseNonstop(file);
							break;
					}
				}
			}

			// if parsed file successfully
			if (result != null) {

				// apply time difference between server/client
				if (hasTimeOffset) {
					result.Modified = result.Modified - timeOffset;
				}

				// calc absolute file paths
				CalcFullPaths(result, path, false);

			}

			return result;
		}

		/// <summary>
		/// Validate if the current parser is correct, or if another parser seems more appropriate.
		/// </summary>
		private void ValidateParser(string[] files) {

			if (IsWrongParser()) {

				// by default use the UNIX parser, if none detected
				if (detectedParser == FtpParser.Auto) {
					detectedParser = FtpParser.Unix;
				}
				if (parser == FtpParser.Auto) {
					parser = detectedParser;
				}

				// if machine listings not supported, switch to UNIX parser
				if (IsWrongMachineListing()) {
					parser = detectedParser;
				}

				// use the initially set parser (from SYST)
				if (IsParserValid(parser, files)) {
				    this.client.LogStatus(FtpTraceLevel.Verbose, "Confirmed format " + parser.ToString());
					parserConfirmed = true;
					return;
				}
				foreach (FtpParser p in parsers) {
					if (IsParserValid(p, files)) {
						parser = p;
					    this.client.LogStatus(FtpTraceLevel.Verbose, "Detected format " + parser.ToString());
						parserConfirmed = true;
						return;
					}
				}
				parser = FtpParser.Unix;
			    this.client.LogStatus(FtpTraceLevel.Verbose, "Could not detect format. Using default " + parser.ToString());

			}
		}

		private bool IsWrongParser() {
			return parser == FtpParser.Auto || !parserConfirmed || IsWrongMachineListing();
		}
		private bool IsWrongMachineListing() {
			return parser == FtpParser.Machine && client != null && !client.HasFeature(FtpCapability.MLSD);
		}

		/// <summary>
		/// Validate if the current parser is correct
		/// </summary>
		private bool IsParserValid(FtpParser p, string[] files) {
			switch (p) {
				case FtpParser.Windows:
					return IsWindowsValid(files);
				case FtpParser.Unix:
					return IsUnixValid(files);
				case FtpParser.VMS:
					return IsVMSValid(files);
				case FtpParser.IBM:
					return IsIBMValid(files);
				case FtpParser.NonStop:
					return IsNonstopValid(files);
			}
			return false;
		}

		#endregion

		#region Legacy Parsers

		/// <summary>
		/// Parses a line from a file listing using the first successful match in the Parsers collection.
		/// </summary>
		/// <param name="path">The source path of the file listing</param>
		/// <param name="buf">A line from the file listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <returns>A FtpListItem object representing the parsed line, null if the line was
		/// unable to be parsed. If you have encountered an unsupported list type add a parser
		/// to the public static Parsers collection of FtpListItem.</returns>
		private static FtpListItem ParseLegacy(string path, string buf, FtpCapability capabilities, FtpClient client) {
			if (!string.IsNullOrEmpty(buf)) {
				FtpListItem item;

				foreach (Parser parser in Parsers) {
					if ((item = parser(buf, capabilities, client)) != null) {
						item.Input = buf;
						return item;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Used for synchronizing access to the Parsers collection
		/// </summary>
		private static Object m_parserLock = new Object();

		/// <summary>
		/// Initializes the default list of parsers
		/// </summary>
		private static void InitParsers() {
			lock (m_parserLock) {
				if (m_parsers == null) {
					m_parsers = new List<Parser>();
					m_parsers.Add(new Parser(ParseMachineList));
					m_parsers.Add(new Parser(ParseUnixList));
					m_parsers.Add(new Parser(ParseDosList));
					m_parsers.Add(new Parser(ParseVMSList));
				}
			}
		}

		private static List<Parser> m_parsers = null;
		/// <summary>
		/// Collection of parsers. Each parser object contains
		/// a regex string that uses named groups, i.e., (?&lt;group_name&gt;foobar).
		/// The support group names are modify for last write time, size for the
		/// size and name for the name of the file system object. Each group name is
		/// optional, if they are present then those values are retrieved from a 
		/// successful match. In addition, each parser contains a Type property
		/// which gets set in the FtpListItem object to distinguish between different
		/// types of objects.
		/// </summary>
		private static Parser[] Parsers {
			get {
				Parser[] parsers;

				lock (m_parserLock) {
					if (m_parsers == null)
						InitParsers();

					parsers = m_parsers.ToArray();
				}

				return parsers;
			}
		}

		private static Parser m_customParser;

		/// <summary>
		/// Adds a custom parser
		/// </summary>
		/// <param name="parser">The parser delegate to add</param>
		/// <example><code source="..\Examples\CustomParser.cs" lang="cs" /></example>
		public static void AddParser(Parser parser) {
			lock (m_parserLock) {
				if (m_parsers == null)
					InitParsers();

				m_parsers.Add(parser);
				m_customParser = parser;
			}
		}

		/// <summary>
		/// Removes all parser delegates
		/// </summary>
		public static void ClearParsers() {
			lock (m_parserLock) {
				if (m_parsers == null)
					InitParsers();

				m_parsers.Clear();
			}
		}

		/// <summary>
		/// Removes the specified parser
		/// </summary>
		/// <param name="parser">The parser delegate to remove</param>
		public static void RemoveParser(Parser parser) {
			lock (m_parserLock) {
				if (m_parsers == null)
					InitParsers();

				m_parsers.Remove(parser);
			}
		}

		/// <summary>
		/// Parses LIST format listings
		/// </summary>
		/// <param name="buf">A line from the listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private static FtpListItem ParseUnixList(string buf, FtpCapability capabilities, FtpClient client) {
			string regex =
				@"(?<permissions>.+)\s+" +
				@"(?<objectcount>\d+)\s+" +
				@"(?<user>.+)\s+" +
				@"(?<group>.+)\s+" +
				@"(?<size>\d+)\s+" +
				@"(?<modify>\w+\s+\d+\s+\d+:\d+|\w+\s+\d+\s+\d+)\s" +
				@"(?<name>.*)$";
			FtpListItem item = new FtpListItem();
			Match m;

			if (!(m = Regex.Match(buf, regex, RegexOptions.IgnoreCase)).Success)
				return null;

			// if this field is missing we can't determine
			// what the object is.
			if (m.Groups["permissions"].Value.Length == 0)
				return null;

			switch (m.Groups["permissions"].Value[0]) {
				case 'd':
					item.Type = FtpFileSystemObjectType.Directory;
					break;
				case '-':
				case 's':
					item.Type = FtpFileSystemObjectType.File;
					break;
				case 'l':
					item.Type = FtpFileSystemObjectType.Link;
					break;
				default:
					return null;
			}

			// if we can't determine a file name then
			// we are not considering this a successful parsing operation.
			if (m.Groups["name"].Value.Length < 1)
				return null;
			item.Name = m.Groups["name"].Value;

			switch (item.Type) {
				case FtpFileSystemObjectType.Directory:
					// ignore these...
					if (item.Name == "." || item.Name == "..")
						return null;
					break;
				case FtpFileSystemObjectType.Link:
					if (!item.Name.Contains(" -> "))
						return null;
					item.LinkTarget = item.Name.Remove(0, item.Name.IndexOf("-> ") + 3).Trim();
					item.Name = item.Name.Remove(item.Name.IndexOf(" -> "));
					break;
			}

			// for date parser testing only
			//capabilities = ~(capabilities & FtpCapability.MDTM);

			////
			// Ignore the Modify times sent in LIST format for files
			// when the server has support for the MDTM command
			// because they will never be as accurate as what can be had 
			// by using the MDTM command. MDTM does not work on directories
			// so if a modify time was parsed from the listing we will try
			// to convert it to a DateTime object and use it for directories.
			////
			if (((capabilities & FtpCapability.MDTM) != FtpCapability.MDTM || item.Type == FtpFileSystemObjectType.Directory) && m.Groups["modify"].Value.Length > 0) {
				item.Modified = m.Groups["modify"].Value.GetFtpDate(DateTimeStyles.AssumeLocal);
				if (item.Modified == DateTime.MinValue) {
                    client.LogStatus(FtpTraceLevel.Warn, "GetFtpDate() failed on " + m.Groups["modify"].Value);
				}
			} else {
				if (m.Groups["modify"].Value.Length == 0)
                    client.LogStatus(FtpTraceLevel.Warn, "RegEx failed to parse modified date from " + buf);
				else if (item.Type == FtpFileSystemObjectType.Directory)
                    client.LogStatus(FtpTraceLevel.Warn, "Modified times of directories are ignored in UNIX long listings.");
				else if ((capabilities & FtpCapability.MDTM) == FtpCapability.MDTM)
                    client.LogStatus(FtpTraceLevel.Warn, "Ignoring modified date because MDTM feature is present. If you aren't already, pass FtpListOption.Modify or FtpListOption.SizeModify to GetListing() to retrieve the modification time.");
			}

			if (m.Groups["size"].Value.Length > 0) {
				long size;

				if (long.TryParse(m.Groups["size"].Value, out size))
					item.Size = size;
			}

			if (m.Groups["permissions"].Value.Length > 0) {
				CalcUnixPermissions(item, m.Groups["permissions"].Value);
			}

			return item;
		}

		/// <summary>
		/// Parses IIS DOS format listings
		/// </summary>
		/// <param name="buf">A line from the listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private static FtpListItem ParseDosList(string buf, FtpCapability capabilities, FtpClient client) {
			FtpListItem item = new FtpListItem();
			string[] datefmt = new string[] {
                "MM-dd-yy  hh:mmtt",
                "MM-dd-yyyy  hh:mmtt"
            };
			Match m;

			// directory
			if ((m = Regex.Match(buf, @"(?<modify>\d+-\d+-\d+\s+\d+:\d+\w+)\s+<DIR>\s+(?<name>.*)$", RegexOptions.IgnoreCase)).Success) {
				DateTime modify;

				item.Type = FtpFileSystemObjectType.Directory;
				item.Name = m.Groups["name"].Value;

				//if (DateTime.TryParse(m.Groups["modify"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out modify))
				if (DateTime.TryParseExact(m.Groups["modify"].Value, datefmt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out modify))
					item.Modified = modify;
			}
				// file
			else if ((m = Regex.Match(buf, @"(?<modify>\d+-\d+-\d+\s+\d+:\d+\w+)\s+(?<size>\d+)\s+(?<name>.*)$", RegexOptions.IgnoreCase)).Success) {
				DateTime modify;
				long size;

				item.Type = FtpFileSystemObjectType.File;
				item.Name = m.Groups["name"].Value;

				if (long.TryParse(m.Groups["size"].Value, out size))
					item.Size = size;

				//if (DateTime.TryParse(m.Groups["modify"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out modify))
				if (DateTime.TryParseExact(m.Groups["modify"].Value, datefmt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out modify))
					item.Modified = modify;
			} else
				return null;

			return item;
		}

		private static FtpListItem ParseVMSList(string buf, FtpCapability capabilities, FtpClient client) {
			string regex =
				@"(?<name>.+)\.(?<extension>.+);(?<version>\d+)\s+" +
				@"(?<size>\d+)\s+" +
				@"(?<modify>\d+-\w+-\d+\s+\d+:\d+)";
			Match m;

			if ((m = Regex.Match(buf, regex)).Success) {
				FtpListItem item = new FtpListItem();

				item.Name = (m.Groups["name"].Value + "." +
					m.Groups["extension"].Value + ";" +
					m.Groups["version"].Value);

				if (m.Groups["extension"].Value.ToUpper() == "DIR")
					item.Type = FtpFileSystemObjectType.Directory;
				else
					item.Type = FtpFileSystemObjectType.File;

				long itemSize = 0;
				if (!long.TryParse(m.Groups["size"].Value, out itemSize))
					itemSize = -1;

				item.Size = itemSize;

				DateTime itemModified = DateTime.MinValue;

				if (!DateTime.TryParse(m.Groups["modify"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out itemModified))
					itemModified = DateTime.MinValue;

				item.Modified = itemModified;

				return item;
			}

			return null;
		}

		/// <summary>
		/// Ftp listing line parser
		/// </summary>
		/// <param name="line">The line from the listing</param>
		/// <param name="capabilities">The server capabilities</param>
		/// <returns>FtpListItem if the line can be parsed, null otherwise</returns>
		public delegate FtpListItem Parser(string line, FtpCapability capabilities, FtpClient client);

		#endregion

		#region Machine Listing Parser

		/// <summary>
		/// Parses MLSD/MLST format listings
		/// </summary>
		/// <param name="buf">A line from the listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private static FtpListItem ParseMachineList(string buf, FtpCapability capabilities, FtpClient client) {
			FtpListItem item = new FtpListItem();
			Match m;

			if (!(m = Regex.Match(buf, "type=(?<type>.+?);", RegexOptions.IgnoreCase)).Success)
				return null;

			switch (m.Groups["type"].Value.ToLower()) {
				case "dir":
				case "pdir":
				case "cdir":
					item.Type = FtpFileSystemObjectType.Directory;
					break;
				case "file":
					item.Type = FtpFileSystemObjectType.File;
					break;
				// These are not supported for now.
				case "link":
				case "device":
				default:
					return null;
			}

			if ((m = Regex.Match(buf, "; (?<name>.*)$", RegexOptions.IgnoreCase)).Success)
				item.Name = m.Groups["name"].Value;
			else // if we can't parse the file name there is a problem.
				return null;

			if ((m = Regex.Match(buf, "modify=(?<modify>.+?);", RegexOptions.IgnoreCase)).Success)
				item.Modified = m.Groups["modify"].Value.GetFtpDate(DateTimeStyles.AssumeUniversal);

			if ((m = Regex.Match(buf, "created?=(?<create>.+?);", RegexOptions.IgnoreCase)).Success)
				item.Created = m.Groups["create"].Value.GetFtpDate(DateTimeStyles.AssumeUniversal);

			if ((m = Regex.Match(buf, @"size=(?<size>\d+);", RegexOptions.IgnoreCase)).Success) {
				long size;

				if (long.TryParse(m.Groups["size"].Value, out size))
					item.Size = size;
			}

			if ((m = Regex.Match(buf, @"unix.mode=(?<mode>\d+);", RegexOptions.IgnoreCase)).Success) {
				if (m.Groups["mode"].Value.Length == 4) {
					item.SpecialPermissions = (FtpSpecialPermissions)int.Parse(m.Groups["mode"].Value[0].ToString());
					item.OwnerPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[1].ToString());
					item.GroupPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[2].ToString());
					item.OthersPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[3].ToString());
					CalcChmod(item);
				} else if (m.Groups["mode"].Value.Length == 3) {
					item.OwnerPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[0].ToString());
					item.GroupPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[1].ToString());
					item.OthersPermissions = (FtpPermission)int.Parse(m.Groups["mode"].Value[2].ToString());
					CalcChmod(item);
				}
			}

			return item;
		}

		#endregion

		#region Unix Parser

		private bool IsUnixValid(string[] listing) {
			int count = Math.Min(listing.Length, 10);

			bool perms1 = false;
			bool perms2 = false;

			for (int i = 0; i < count; i++) {
				if (listing[i].Trim().Length == 0)
					continue;
				string[] fields = SplitString(listing[i]);
				if (fields.Length < MIN_EXPECTED_FIELD_COUNT_UNIX)
					continue;
				// check perms
				char ch00 = Char.ToLower(fields[0][0]);
				if (ch00 == '-' || ch00 == 'l' || ch00 == 'd')
					perms1 = true;

				if (fields[0].Length > 1) {
					char ch01 = Char.ToLower(fields[0][1]);
					if (ch01 == 'r' || ch01 == '-')
						perms2 = true;
				}

				// last chance - Connect:Enterprise has -ART------TCP
				if (!perms2 && fields[0].Length > 2 && fields[0].IndexOf('-', 2) > 0)
					perms2 = true;
			}
			if (perms1 && perms2)
				return true;
		    this.client.LogStatus(FtpTraceLevel.Verbose, "Not in UNIX format");
			return false;
		}

		/// <summary>
		/// Parses Unix format listings
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseUnix(string raw) {

			//-----------------------------------------------------
			// EXAMPLES

			// lrwxrwxrwx   1 wuftpd   wuftpd         14 Jul 22  2002 MIRRORS -> README-MIRRORS
			// -rw-r--r--   1 b173771  users         431 Mar 31 20:04 .htaccess
			//-----------------------------------------------------

			// test it is a valid line, e.g. "total 342522" is invalid
			char ch = raw[0];
			if (ch != ORDINARY_FILE_CHAR && ch != DIRECTORY_CHAR && ch != SYMLINK_CHAR)
				return null;

			string[] fields = SplitString(raw);

			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_UNIX) {
				StringBuilder msg = new StringBuilder("Unexpected number of fields in listing '");
				msg.Append(raw).Append("' - expected minimum ").Append(MIN_EXPECTED_FIELD_COUNT_UNIX).
						Append(" fields but found ").Append(fields.Length).Append(" fields");
			    this.client.LogStatus(FtpTraceLevel.Verbose, msg.ToString());
				return null;
			}

			// field pos
			int index = 0;

			// first field is perms
			string permissions = fields[index++];
			ch = permissions[0];
			bool isDir = false;
			bool isLink = false;
			if (ch == DIRECTORY_CHAR)
				isDir = true;
			else if (ch == SYMLINK_CHAR)
				isLink = true;

			// some servers don't supply the link count
			int linkCount = 0;
			if (Char.IsDigit(fields[index][0])) // assume it is if a digit
            {
				string linkCountStr = fields[index++];
				try {
					linkCount = System.Int32.Parse(linkCountStr);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse link count: " + linkCountStr);
				}
			} else if (fields[index][0] == '-') // IPXOS Treck FTP server
            {
				index++;
			}

			// owner and group
			string owner = "";
			string group = "";
			// if 2 fields ahead is numeric and there's enough fields beyond (4) for
			// the date, then the next two fields should be the owner & group
			if (IsNumeric(fields[index + 2]) && fields.Length - (index + 2) > 4) {
				owner = fields[index++];
				group = fields[index++];
			}
				// no owner
			else if (IsNumeric(fields[index + 1]) && fields.Length - (index + 1) > 4) {
				group = fields[index++];
			}

			// size
			long size = 0L;
			string sizeStr = fields[index++].Replace(".", ""); // get rid of .'s in size           
			try {
				size = Int64.Parse(sizeStr);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + sizeStr);
			}

			// next 3 fields are the date time

			// we expect the month first on Unix. 
			// Connect:Enterprise UNIX has a weird extra numeric field here - we test if the 
			// next field is numeric and if so, we skip it (except we check for a BSD variant
			// that means it is the day of the month)
			int dayOfMonth = -1;
			if (IsNumeric(fields[index])) {
				// this just might be the day of month - BSD variant
				// we check it is <= 31 AND that the next field starts
				// with a letter AND the next has a ':' within it
				try {
					char[] chars = { '0' };
					string str = fields[index].TrimStart(chars);
					dayOfMonth = Int32.Parse(fields[index]);
					if (dayOfMonth > 31) // can't be day of month
						dayOfMonth = -1;
					if (!(Char.IsLetter(fields[index + 1][0])))
						dayOfMonth = -1;
					if (fields[index + 2].IndexOf(':') <= 0)
						dayOfMonth = -1;
				} catch (FormatException) { }
				index++;
			}

			int dateTimePos = index;
			DateTime lastModified = DateTime.MinValue;
			StringBuilder stamp = new StringBuilder(fields[index++]);
			stamp.Append('-');
			if (dayOfMonth > 0)
				stamp.Append(dayOfMonth);
			else
				stamp.Append(fields[index++]);
			stamp.Append('-');

			string field = fields[index++];
			if (field.IndexOf((System.Char)':') < 0 && field.IndexOf((System.Char)'.') < 0) {
				stamp.Append(field); // year
				try {
					lastModified = DateTime.ParseExact(stamp.ToString(), unixDateFormats1,
												parserCulture.DateTimeFormat, DateTimeStyles.None);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
				}
			} else {
				// add the year ourselves as not present
				int year = parserCulture.Calendar.GetYear(DateTime.Now);
				stamp.Append(year).Append('-').Append(field);
				try {

					lastModified = DateTime.ParseExact(stamp.ToString(), unixDateFormats2,
												parserCulture.DateTimeFormat, DateTimeStyles.None);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
				}

				// can't be in the future - must be the previous year
				// add 2 days for time zones (thanks hgfischer)
				if (lastModified > DateTime.Now.AddDays(2)) {
					lastModified = lastModified.AddYears(-1);
				}
			}

			// name of file or dir. Extract symlink if possible
			string name = null;
			string linkedname = null;

			// we've got to find the starting point of the name. We
			// do this by finding the pos of all the date/time fields, then
			// the name - to ensure we don't get tricked up by a userid the
			// same as the filename,for example
			int pos = 0;
			bool ok = true;
			int dateFieldCount = dayOfMonth > 0 ? 2 : 3; // only 2 fields left if we had a leading day of month
			for (int i = dateTimePos; i < dateTimePos + dateFieldCount; i++) {
				pos = raw.IndexOf(fields[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				} else {
					pos += fields[i].Length;
				}
			}
			if (ok) {
				string remainder = raw.Substring(pos).Trim();
				if (!isLink)
					name = remainder;
				else {
					// symlink, try to extract it
					pos = remainder.IndexOf(SYMLINK_ARROW);
					if (pos <= 0) {
						// couldn't find symlink, give up & just assign as name
						name = remainder;
					} else {
						int len = SYMLINK_ARROW.Length;
						name = remainder.Substring(0, (pos) - (0)).Trim();
						if (pos + len < remainder.Length)
							linkedname = remainder.Substring(pos + len);
					}
				}
			} else {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + raw);
			}

			FtpListItem file = new FtpListItem(raw, name, size, isDir, ref lastModified);
			if (isLink) {
				file.Type = FtpFileSystemObjectType.Link;
				file.LinkCount = linkCount;
				file.LinkTarget = linkedname.Trim();
			}
			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			CalcUnixPermissions(file, permissions);
			return file;
		}

		/// <summary>
		/// Parses Unix format listings with alternate parser
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseUnixAlt(string raw) {

			//-----------------------------------------------------
			// EXAMPLES

			// -r-------- GMETECHNOLOGY 1 TSI         8 Nov 06 11:00:25 ,GMETECHNOLOGY,file02.csv,U,20071106A00001105190.txt
			//-----------------------------------------------------

			// test it is a valid line, e.g. "total 342522" is invalid
			char ch = raw[0];
			if (ch != ORDINARY_FILE_CHAR && ch != DIRECTORY_CHAR && ch != SYMLINK_CHAR)
				return null;

			string[] fields = SplitString(raw);

			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_UNIXALT) {
				StringBuilder listing = new StringBuilder("Unexpected number of fields in listing '");
				listing.Append(raw).Append("' - expected minimum ").Append(MIN_EXPECTED_FIELD_COUNT_UNIXALT).
						Append(" fields but found ").Append(fields.Length).Append(" fields");
				throw new FormatException(listing.ToString());
			}

			// field pos
			int index = 0;

			// first field is perms
			string permissions = fields[index++];
			ch = permissions[0];
			bool isDir = false;
			bool isLink = false;
			if (ch == DIRECTORY_CHAR)
				isDir = true;
			else if (ch == SYMLINK_CHAR)
				isLink = true;

			string group = fields[index++];

			// some servers don't supply the link count
			int linkCount = 0;
			if (Char.IsDigit(fields[index][0])) // assume it is if a digit
            {
				string linkCountStr = fields[index++];
				try {
					linkCount = System.Int32.Parse(linkCountStr);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse link count: " + linkCountStr);
				}
			}

			string owner = fields[index++];


			// size
			long size = 0L;
			string sizeStr = fields[index++];
			try {
				size = Int64.Parse(sizeStr);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + sizeStr);
			}

			// next 3 fields are the date time

			// we expect the month first on Unix. 
			int dateTimePos = index;
			DateTime lastModified = DateTime.MinValue;
			StringBuilder stamp = new StringBuilder(fields[index++]);
			stamp.Append('-').Append(fields[index++]).Append('-');

			string field = fields[index++];
			if (field.IndexOf((System.Char)':') < 0) {
				stamp.Append(field); // year
				try {
					lastModified = DateTime.ParseExact(stamp.ToString(), unixAltDateFormats1,
												parserCulture.DateTimeFormat, DateTimeStyles.None);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
				}
			} else {
				// add the year ourselves as not present
				int year = parserCulture.Calendar.GetYear(DateTime.Now);
				stamp.Append(year).Append('-').Append(field);
				try {

					lastModified = DateTime.ParseExact(stamp.ToString(), unixAltDateFormats2,
												parserCulture.DateTimeFormat, DateTimeStyles.None);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
				}

				// can't be in the future - must be the previous year
				// add 2 days for time zones (thanks hgfischer)
				if (lastModified > DateTime.Now.AddDays(2)) {
					lastModified = lastModified.AddYears(-1);
				}
			}

			// name of file or dir. Extract symlink if possible
			string name = null;

			// we've got to find the starting point of the name. We
			// do this by finding the pos of all the date/time fields, then
			// the name - to ensure we don't get tricked up by a userid the
			// same as the filename,for example
			int pos = 0;
			bool ok = true;
			for (int i = dateTimePos; i < dateTimePos + 3; i++) {
				pos = raw.IndexOf(fields[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				} else {
					pos += fields[i].Length;
				}
			}
			if (ok) {
				name = raw.Substring(pos).Trim();
			} else {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + raw);
			}

			FtpListItem file = new FtpListItem(raw, name, size, isDir, ref lastModified);
			if (isLink) {
				file.Type = FtpFileSystemObjectType.Link;
				file.LinkCount = linkCount;
			}
			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			CalcUnixPermissions(file, permissions);
			return file;
		}

		#endregion

		#region Windows Parser

		private bool IsWindowsValid(string[] listing) {
			int count = Math.Min(listing.Length, 10);

			bool dateStart = false;
			bool timeColon = false;
			bool dirOrFile = false;

			for (int i = 0; i < count; i++) {
				if (listing[i].Trim().Length == 0)
					continue;
				string[] fields = SplitString(listing[i]);
				if (fields.Length < MIN_EXPECTED_FIELD_COUNT_WIN)
					continue;
				// first & last chars are digits of first field
				if (Char.IsDigit(fields[0][0]) && Char.IsDigit(fields[0][fields[0].Length - 1]))
					dateStart = true;
				if (fields[1].IndexOf(':') > 0)
					timeColon = true;
				if (fields[2].ToUpper() == WIN_DIR || Char.IsDigit(fields[2][0]))
					dirOrFile = true;
			}
			if (dateStart && timeColon && dirOrFile)
				return true;
		    this.client.LogStatus(FtpTraceLevel.Verbose, "Not in Windows format");
			return false;
		}

		/// <summary>
		/// Parses IIS/DOS format listings
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseWindows(string raw) {
			//-----------------------------------------------------
			// EXAMPLES

			// 05-17-03  02:47PM                70776 ftp.jar
			// 08-28-03  10:08PM       <DIR>          EDT SSLTest
			//-----------------------------------------------------

			string[] fields = SplitString(raw);

			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_WIN)
				return null;

			// first two fields are date time
			string lastModifiedStr = fields[0] + " " + fields[1];
			DateTime lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.ParseExact(lastModifiedStr, windowsDateFormats,
									parserCulture.DateTimeFormat, DateTimeStyles.None);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}

			// dir flag
			bool isDir = false;
			long size = 0L;
			if (fields[2].ToUpper().Equals(WIN_DIR.ToUpper()))
				isDir = true;
			else {
				try {
					size = Int64.Parse(fields[2]);
				} catch (FormatException) {
				    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + fields[2]);
				}
			}

			// we've got to find the starting point of the name. We
			// do this by finding the pos of all the date/time fields, then
			// the name - to ensure we don't get tricked up by a date or dir the
			// same as the filename, for example
			int pos = 0;
			bool ok = true;
			for (int i = 0; i < 3; i++) {
				pos = raw.IndexOf(fields[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				} else {
					pos += fields[i].Length;
				}
			}
			string name = null;
			if (ok) {
				name = raw.Substring(pos).Trim();
			} else {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + raw);
			}
			return new FtpListItem(raw, name, size, isDir, ref lastModified);
		}

		#endregion

		#region VMS Parser

		private bool IsVMSValid(String[] listing) {
			int count = Math.Min(listing.Length, 10);

			bool semiColonName = false;
			bool squareBracketStart = false, squareBracketEnd = false;

			for (int i = 0; i < count; i++) {
				if (listing[i].Trim().Length == 0)
					continue;
				int pos = 0;
				if ((pos = listing[i].IndexOf(';')) > 0 && (++pos < listing[i].Length) &&
					Char.IsDigit(listing[i][pos]))
					semiColonName = true;
				if (listing[i].IndexOf('[') > 0)
					squareBracketStart = true;
				if (listing[i].IndexOf(']') > 0)
					squareBracketEnd = true;
			}
			if (semiColonName && squareBracketStart && squareBracketEnd)
				return true;
		    this.client.LogStatus(FtpTraceLevel.Verbose, "Not in VMS format");
			return false;
		}

		/// <summary>
		/// Parses Vax/VMS format listings
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseVMS(string raw) {

			//-----------------------------------------------------
			// EXAMPLES
			//    
			//    Directory dirname
			//    filename;version  used/allocated    dd-MMM-yyyy HH:mm:ss [group,owner]   (PERMS)
			//    ...
			//    
			//    Total of n files, n/m blocks
			//-----------------------------------------------------

			string[] fields = SplitString(raw);

			// skip blank lines
			if (fields.Length <= 0)
				return null;
			// skip line which lists Directory
			if (fields.Length >= 2 && fields[0].Equals(VMS_HDR))
				return null;
			// skip line which lists Total
			if (fields.Length > 0 && fields[0].Equals(VMS_TOTAL))
				return null;
			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_VMS)
				return null;

			// first field is name
			string name = fields[0];

			// make sure it is the name (ends with ';<INT>')
			int semiPos = name.LastIndexOf(';');
			// check for ;
			if (semiPos <= 0) {
			    this.client.LogStatus(FtpTraceLevel.Verbose, "File version number not found in name '" + name + "'");
				return null;
			}

			string nameNoVersion = name.Substring(0, semiPos);

			// check for version after ;
			string afterSemi = fields[0].Substring(semiPos + 1);
			try {
				Int64.Parse(afterSemi);
				// didn't throw exception yet, must be number
				// we don't use it currently but we might in future
			} catch (FormatException) {
				// don't worry about version number
			}

			// test is dir
			bool isDir = false;
			if (nameNoVersion.EndsWith(VMS_DIR)) {
				isDir = true;
				name = nameNoVersion.Substring(0, nameNoVersion.Length - VMS_DIR.Length);
			}

			if (!vmsNameHasVersion && !isDir) {
				name = nameNoVersion;
			}

			// 2nd field is size USED/ALLOCATED format, or perhaps just USED
			int slashPos = fields[1].IndexOf('/');
			string sizeUsed = fields[1];
			long size = 0;
			if (slashPos == -1) {

				// only filesize in bytes
				size = Int64.Parse(fields[1]);

			}else{
				if (slashPos > 0)
					sizeUsed = fields[1].Substring(0, slashPos);
				size = Int64.Parse(sizeUsed) * vmsBlocksize;
			}

			// 3 & 4 fields are date time
			string lastModifiedStr = FixDateVMS(fields);
			DateTime lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.Parse(lastModifiedStr.ToString(), parserCulture.DateTimeFormat);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}

			// 5th field is [group,owner]
			string group = null;
			string owner = null;
			if (fields.Length >= 5) {
				if (fields[4][0] == '[' && fields[4][fields[4].Length - 1] == ']') {
					int commaPos = fields[4].IndexOf(',');
					if (commaPos < 0) {
						owner = fields[4].Substring(1, fields[4].Length - 2);
						group = "";
					} else {
						group = fields[4].Substring(1, commaPos - 1);
						owner = fields[4].Substring(commaPos + 1, fields[4].Length - commaPos - 2);
					}
				}
			}

			// 6th field is permissions e.g. (RWED,RWED,RE,)
			string permissions = null;
			if (fields.Length >= 6) {
				if (fields[5][0] == '(' && fields[5][fields[5].Length - 1] == ')') {
					permissions = fields[5].Substring(1, fields[5].Length - 2);
				}
			}

			FtpListItem file = new FtpListItem(raw, name, size, isDir, ref lastModified);
			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			return file;
		}

		#endregion

		#region NonStop Parser

		private bool IsNonstopValid(string[] listing) {
			return IsNonstopHeader(listing[0]);
		}

		private bool IsNonstopHeader(string line) {
			if (line.IndexOf("Code") > 0 && line.IndexOf("EOF") > 0 &&
				line.IndexOf("RWEP") > 0)
				return true;
			return false;
		}

		/// <summary>
		/// Parses NonStop format listings
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseNonstop(string raw) {
			//-----------------------------------------------------
			// EXAMPLES

			// File         Code             EOF  Last Modification    Owner  RWEP
			// IARPTS        101            16354 18-Mar-08 15:09:12 244, 10 "nnnn"
			// JENNYCB2      101            16384 10-Jul-08 11:44:56 244, 10 "nnnn"
			//-----------------------------------------------------

			if (IsNonstopHeader(raw))
				return null;

			string[] fields = SplitString(raw);

			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_TANDEM)
				return null;

			string name = fields[0];

			// first two fields are date time
			string lastModifiedStr = fields[3] + " " + fields[4];
			DateTime lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.ParseExact(lastModifiedStr, nonstopDateFormats,
									parserCulture.DateTimeFormat, DateTimeStyles.None);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}

			// dir flag
			bool isDir = false;
			long size = 0L;
			try {
				size = Int64.Parse(fields[2]);
			} catch (FormatException) {
			    this.client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + fields[2]);
			}

			string owner = fields[5] + fields[6];
			string permissions = fields[7].Trim(NONSTOP_TRIM);

			FtpListItem file = new FtpListItem(raw, name, size, isDir, ref lastModified);
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			return file;
		}

		#endregion

		#region IBM Parser

		private bool IsIBMValid(String[] listing) {
			int count = Math.Min(listing.Length, 10);

			bool dir = false;
			bool ddir = false;
			bool lib = false;
			bool stmf = false;
			bool flr = false;
			bool file = false;

			for (int i = 0; i < count; i++) {
				if (listing[i].IndexOf("*DIR") > 0)
					dir = true;
				else if (listing[i].IndexOf("*FILE") > 0)
					file = true;
				else if (listing[i].IndexOf("*FLR") > 0)
					flr = true;
				else if (listing[i].IndexOf("*DDIR") > 0)
					ddir = true;
				else if (listing[i].IndexOf("*STMF") > 0)
					stmf = true;
				else if (listing[i].IndexOf("*LIB") > 0)
					lib = true;
			}
			if (dir || file || ddir || lib || stmf || flr)
				return true;
		    this.client.LogStatus(FtpTraceLevel.Verbose, "Not in OS/400 format");
			return false;
		}

		/// <summary>
		/// Parses IBM OS/400 format listings
		/// </summary>
		/// <param name="raw">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		private FtpListItem ParseIBM(string raw) {

            //-----------------------------------------------------
            // EXAMPLES
            // 
            // In a library:
            //        CFT             45056 04/12/06 14:19:31 *FILE AFTFRE1.FILE
            //        CFT                                     *MEM AFTFRE1.FILE/AFTFRE1.MBR
            //        CFT             36864 28/11/06 15:19:30 *FILE AFTFRE2.FILE
            //        CFT                                     *MEM AFTFRE2.FILE/AFTFRE2.MBR
            //        CFT             45056 04/12/06 14:19:37 *FILE AFTFRE6.FILE
            //        CFT                                     *MEM  AFTFRE6.FILE/AFTFRE6.MBR
            //        QSYSOPR         28672 01/12/06 20:08:04 *FILE FPKI45POK5.FILE
            //        QSYSOPR                                 *MEM FPKI45POK5.FILE/FPKI45POK5.MBR    
            //        
		    // Inside a file:
		    //        DEREK           76128 07/11/17 14:25:46 *FILE
		    //        DEREK                                   *MEM AAR.MBR
		    //        DEREK                                   *MEM AAS.MBR
            //-----------------------------------------------------

            string[] fields = SplitString(raw);

			// skip blank lines
			if (fields.Length <= 0)
				return null;
			// return what we can for MEM
			if (fields.Length >= 2 && fields[1].Equals(IBM_MEM)) {
				DateTime lastModifiedm = DateTime.MinValue;
				string ownerm = fields[0];
				string namem = fields[2];
				FtpListItem filem = new FtpListItem(raw, namem, 0, false, ref lastModifiedm);
				filem.RawOwner = ownerm;
				return filem;
			}
			if (fields.Length < MIN_EXPECTED_FIELD_COUNT_OS400)
				return null;

			// first field is owner
			string owner = fields[0];

			// next is size
			long size = Int64.Parse(fields[1]);

			string lastModifiedStr = fields[2] + " " + fields[3];
			DateTime lastModified = GetLastModifiedIBM(lastModifiedStr);

			// test is dir
			bool isDir = false;
			if (fields[4] == IBM_DIR || fields[4] == IBM_DDIR || (fields.Length == 5 && fields[4] == IBM_FILE))
				isDir = true;

            // If there's no name, it's because we're inside a file.  Fake out a "current directory" name instead.
			string name = fields.Length >= 6 
                ? fields[5] 
                : ".";
			if (name.EndsWith("/")) {
				isDir = true;
				name = name.Substring(0, name.Length - 1);
			}
            
			FtpListItem file = new FtpListItem(raw, name, size, isDir, ref lastModified);
			file.RawOwner = owner;
			return file;
		}

		#endregion

		#region Utils

		/// <summary>
		/// Split into fields by splitting on strings
		/// </summary>
		private string[] SplitString(string str) {
			List<string> allTokens = new List<string>(str.Split(null));
			for (int i = allTokens.Count - 1; i >= 0; i--)
				if (((string)allTokens[i]).Trim().Length == 0)
					allTokens.RemoveAt(i);
			return (string[])allTokens.ToArray();
		}

		private int formatIndex = 0;

		private void CalcFullPaths(FtpListItem item, string path, bool isVMS) {


			// EXIT IF NO DIR PATH PROVIDED
			if (path == null) {

				// check if the path is absolute
				if (IsAbsolutePath(item.Name)) {
					item.FullName = item.Name;
					item.Name = item.Name.GetFtpFileName();
				}

				return;
			}


			// ONLY IF DIR PATH PROVIDED

			// if this is a vax/openvms file listing
			// there are no slashes in the path name
			if (isVMS)
				item.FullName = path + item.Name;
			else {
				//this.client.LogStatus(item.Name);

				// remove globbing/wildcard from path
				if (path.GetFtpFileName().Contains("*")) {
					path = path.GetFtpDirectoryName();
				}

				if (item.Name != null) {
					// absolute path? then ignore the path input to this method.
					if (IsAbsolutePath(item.Name)) {
						item.FullName = item.Name;
						item.Name = item.Name.GetFtpFileName();
					} else if (path != null) {
						item.FullName = path.GetFtpPath(item.Name); //.GetFtpPathWithoutGlob();
					} else {
                        this.client.LogStatus(FtpTraceLevel.Warn, "Couldn't determine the full path of this object: " +
							Environment.NewLine + item.ToString());
					}
				}


				// if a link target is set and it doesn't include an absolute path
				// then try to resolve it.
				if (item.LinkTarget != null && !item.LinkTarget.StartsWith("/")) {
					if (item.LinkTarget.StartsWith("./"))
						item.LinkTarget = path.GetFtpPath(item.LinkTarget.Remove(0, 2)).Trim();
					else
						item.LinkTarget = path.GetFtpPath(item.LinkTarget).Trim();
				}
			}
		}

		private bool IsAbsolutePath(string path) {
			return path.StartsWith("/") || path.StartsWith("./") || path.StartsWith("../");
		}

		private static void CalcChmod(FtpListItem item) {
			item.Chmod = FtpClient.CalcChmod(item.OwnerPermissions, item.GroupPermissions, item.OthersPermissions);
		}

		private static void CalcUnixPermissions(FtpListItem item, string permissions) {
			Match perms = Regex.Match(permissions,
							@"[\w-]{1}(?<owner>[\w-]{3})(?<group>[\w-]{3})(?<others>[\w-]{3})",
							RegexOptions.IgnoreCase);

			if (perms.Success) {

				if (perms.Groups["owner"].Value.Length == 3) {
					if (perms.Groups["owner"].Value[0] == 'r') {
						item.OwnerPermissions |= FtpPermission.Read;
					}
					if (perms.Groups["owner"].Value[1] == 'w') {
						item.OwnerPermissions |= FtpPermission.Write;
					}
					if (perms.Groups["owner"].Value[2] == 'x' || perms.Groups["owner"].Value[2] == 's') {
						item.OwnerPermissions |= FtpPermission.Execute;
					}
					if (perms.Groups["owner"].Value[2] == 's' || perms.Groups["owner"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetUserID;
					}
				}

				if (perms.Groups["group"].Value.Length == 3) {
					if (perms.Groups["group"].Value[0] == 'r') {
						item.GroupPermissions |= FtpPermission.Read;
					}
					if (perms.Groups["group"].Value[1] == 'w') {
						item.GroupPermissions |= FtpPermission.Write;
					}
					if (perms.Groups["group"].Value[2] == 'x' || perms.Groups["group"].Value[2] == 's') {
						item.GroupPermissions |= FtpPermission.Execute;
					}
					if (perms.Groups["group"].Value[2] == 's' || perms.Groups["group"].Value[2] == 'S') {
						item.SpecialPermissions |= FtpSpecialPermissions.SetGroupID;
					}
				}

				if (perms.Groups["others"].Value.Length == 3) {
					if (perms.Groups["others"].Value[0] == 'r') {
						item.OthersPermissions |= FtpPermission.Read;
					}
					if (perms.Groups["others"].Value[1] == 'w') {
						item.OthersPermissions |= FtpPermission.Write;
					}
					if (perms.Groups["others"].Value[2] == 'x' || perms.Groups["others"].Value[2] == 't') {
						item.OthersPermissions |= FtpPermission.Execute;
					}
					if (perms.Groups["others"].Value[2] == 't' || perms.Groups["others"].Value[2] == 'T') {
						item.SpecialPermissions |= FtpSpecialPermissions.Sticky;
					}
				}

				CalcChmod(item);
			}
		}


		// OS-SPECIFIC PARSERS

		private bool IsUnixListing(string raw) {
			char ch = raw[0];
			if (ch == ORDINARY_FILE_CHAR || ch == DIRECTORY_CHAR || ch == SYMLINK_CHAR)
				return true;
			return false;
		}

		private bool IsNumeric(string field) {
			field = field.Replace(".", ""); // strip dots
			for (int i = 0; i < field.Length; i++) {
				if (!Char.IsDigit(field[i]))
					return false;
			}
			return true;
		}

		private DateTime GetLastModifiedIBM(string lastModifiedStr) {
			DateTime lastModified = DateTime.MinValue;
			if (formatIndex >= ibmDateFormats.Length) {
			    this.client.LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
				return DateTime.MinValue;
			}
			int prevIndex = formatIndex;
			for (int i = formatIndex; i < ibmDateFormats.Length; i++, formatIndex++) {
				try {
					lastModified = DateTime.ParseExact(lastModifiedStr, ibmDateFormats[formatIndex],
						parserCulture.DateTimeFormat, DateTimeStyles.None);
					if (lastModified > DateTime.Now.AddDays(2)) {
					    this.client.LogStatus(FtpTraceLevel.Verbose, "Swapping to alternate format (found date in future)");
						continue;
					} else // all ok, exit loop
						break;
				} catch (FormatException) {
					continue;
				}
			}
			if (formatIndex >= ibmDateFormats.Length) {
			    this.client.LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
				return DateTime.MinValue;
			}
			if (formatIndex > prevIndex) // we've changed formatters so redo
            {
				throw new CriticalListParseException();
			}
			return lastModified;
		}

		/// <summary> Fix the date string to make the month camel case</summary>
		/// <param name="fields">array of fields</param>
		private string FixDateVMS(string[] fields) {
			// convert the last 2 chars of month to lower case
			StringBuilder lastModifiedStr = new StringBuilder();
			bool monthFound = false;
			for (int i = 0; i < fields[2].Length; i++) {
				if (!Char.IsLetter(fields[2][i])) {
					lastModifiedStr.Append(fields[2][i]);
				} else {
					if (!monthFound) {
						lastModifiedStr.Append(fields[2][i]);
						monthFound = true;
					} else {
						lastModifiedStr.Append(Char.ToLower(fields[2][i]));
					}
				}
			}
			lastModifiedStr.Append(" ").Append(fields[3]);
			return lastModifiedStr.ToString();
		}

		internal class CriticalListParseException : Exception {

		}


		#endregion

	}
}