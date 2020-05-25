using FluentFTP.Exceptions;
using FluentFTP.Helpers.Parsers;
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
		#region Internal API

		/// <summary>
		/// the FTP connection that owns this parser
		/// </summary>
		public FtpClient client;

		private static List<FtpParser> parsers = new List<FtpParser> {
			FtpParser.Unix, FtpParser.Windows, FtpParser.IBM, FtpParser.VMS, FtpParser.NonStop
		};

		/// <summary>
		/// current parser, or parser set by user
		/// </summary>
		public FtpParser CurrentParser = FtpParser.Auto;

		/// <summary>
		/// parser calculated based on system type (SYST command)
		/// </summary>
		public FtpParser DetectedParser = FtpParser.Auto;

		/// <summary>
		/// if we have detected that the current parser is valid
		/// </summary>
		public bool ParserConfirmed = false;

		/// <summary>
		/// what is the time offset between server/client?
		/// </summary>
		public TimeSpan TimeOffset = new TimeSpan();

		/// <summary>
		/// any time offset between server/client?
		/// </summary>
		public bool HasTimeOffset = false;

		/// <summary>
		/// Is the version number returned as part of the filename?
		/// 
		/// Some VMS FTP servers do not permit a file to be deleted unless
		/// the filename includes the version number. Note that directories are
		/// never returned with the version number.
		/// </summary>
		public static bool VMSNameHasVersion = false;

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
		public void Init(FtpOperatingSystem system, FtpParser defaultParser = FtpParser.Auto) {
			ParserConfirmed = false;

			if (system == FtpOperatingSystem.Windows) {
				CurrentParser = FtpParser.Windows;
			}
			else if (system == FtpOperatingSystem.Unix || system == FtpOperatingSystem.SunOS) {
				CurrentParser = FtpParser.Unix;
			}
			else if (system == FtpOperatingSystem.VMS) {
				CurrentParser = FtpParser.VMS;
			}
			else if (system == FtpOperatingSystem.IBMOS400 || system == FtpOperatingSystem.IBMzOS) {
				CurrentParser = FtpParser.IBM;
			}
			else {
				CurrentParser = defaultParser;
				client.LogStatus(FtpTraceLevel.Warn, "Cannot auto-detect listing parser for system '" + system + "', using " + defaultParser + " parser");
			}

			DetectedParser = CurrentParser;
		}

		/// <summary>
		/// Parse raw file from server into a file object, using the currently active parser.
		/// </summary>
		public FtpListItem ParseSingleLine(string path, string file, List<FtpCapability> caps, bool isMachineList) {
			FtpListItem result = null;

			// force machine listing if it is
			if (isMachineList) {
				result = FtpMachineListParser.Parse(file, caps, client);
			}
			else {
				// use custom parser if given
				if (m_customParser != null) {
					result = m_customParser(file, caps, client);
				}
				else {
					if (IsWrongParser()) {
						ValidateParser(new[] {file});
					}

					// use one of the in-built parsers
					switch (CurrentParser) {
						case FtpParser.Legacy:
							result = ParseLegacy(path, file, caps, client);
							break;

						case FtpParser.Machine:
							result = FtpMachineListParser.Parse(file, caps, client);
							break;

						case FtpParser.Windows:
							result = FtpWindowsParser.Parse(client, file);
							break;

						case FtpParser.Unix:
							result = FtpUnixParser.Parse(client, file);
							break;

						case FtpParser.UnixAlt:
							result = FtpUnixParser.ParseUnixAlt(client, file);
							break;

						case FtpParser.VMS:
							result = FtpVMSParser.Parse(client, file);
							break;

						case FtpParser.IBM:
							result = FtpIBMParser.Parse(client, file);
							break;

						case FtpParser.NonStop:
							result = FtpNonStopParser.Parse(client, file);
							break;
					}
				}
			}

			// if parsed file successfully
			if (result != null) {
				// apply time difference between server/client
				if (HasTimeOffset) {
					result.Modified = result.Modified - TimeOffset;
				}

				// calc absolute file paths
				result.CalculateFullFtpPath(client, path, false);
			}

			return result;
		}

		/// <summary>
		/// Validate if the current parser is correct, or if another parser seems more appropriate.
		/// </summary>
		private void ValidateParser(string[] files) {
			if (IsWrongParser()) {
				// by default use the UNIX parser, if none detected
				if (DetectedParser == FtpParser.Auto) {
					DetectedParser = FtpParser.Unix;
				}

				if (CurrentParser == FtpParser.Auto) {
					CurrentParser = DetectedParser;
				}

				// if machine listings not supported, switch to UNIX parser
				if (IsWrongMachineListing()) {
					CurrentParser = DetectedParser;
				}

				// use the initially set parser (from SYST)
				if (IsParserValid(CurrentParser, files)) {
					client.LogStatus(FtpTraceLevel.Verbose, "Confirmed format " + CurrentParser.ToString());
					ParserConfirmed = true;
					return;
				}

				foreach (var p in parsers) {
					if (IsParserValid(p, files)) {
						CurrentParser = p;
						client.LogStatus(FtpTraceLevel.Verbose, "Detected format " + CurrentParser.ToString());
						ParserConfirmed = true;
						return;
					}
				}

				CurrentParser = FtpParser.Unix;
				client.LogStatus(FtpTraceLevel.Verbose, "Could not detect format. Using default " + CurrentParser.ToString());
			}
		}

		private bool IsWrongParser() {
			return CurrentParser == FtpParser.Auto || !ParserConfirmed || IsWrongMachineListing();
		}

		private bool IsWrongMachineListing() {
			return CurrentParser == FtpParser.Machine && client != null && !client.HasFeature(FtpCapability.MLSD);
		}

		/// <summary>
		/// Validate if the current parser is correct
		/// </summary>
		private bool IsParserValid(FtpParser p, string[] files) {
			switch (p) {
				case FtpParser.Windows:
					return FtpWindowsParser.IsValid(client, files);

				case FtpParser.Unix:
					return FtpUnixParser.IsValid(client, files);

				case FtpParser.VMS:
					return FtpVMSParser.IsValid(client, files);

				case FtpParser.IBM:
					return FtpIBMParser.IsValid(client, files);

				case FtpParser.NonStop:
					return FtpNonStopParser.IsValid(client, files);
			}

			return false;
		}

		#endregion

		#region Legacy API

		/// <summary>
		/// Used for synchronizing access to the Parsers collection
		/// </summary>
		public static object m_parserLock = new object();

		/// <summary>
		/// Adds a custom parser
		/// </summary>
		/// <param name="parser">The parser delegate to add</param>
		/// <example><code source="..\Examples\CustomParser.cs" lang="cs" /></example>
		public static void AddParser(Parser parser) {
			lock (m_parserLock) {
				if (m_parsers == null) {
					InitParsers();
				}

				m_parsers.Add(parser);
				m_customParser = parser;
			}
		}

		/// <summary>
		/// Removes all parser delegates
		/// </summary>
		public static void ClearParsers() {
			lock (m_parserLock) {
				if (m_parsers == null) {
					InitParsers();
				}

				m_parsers.Clear();
				m_customParser = null;
			}
		}

		/// <summary>
		/// Removes the specified parser
		/// </summary>
		/// <param name="parser">The parser delegate to remove</param>
		public static void RemoveParser(Parser parser) {
			lock (m_parserLock) {
				if (m_parsers == null) {
					InitParsers();
				}

				m_parsers.Remove(parser);
				
				if(m_customParser == parser){
					m_customParser = null;
				}
			}
		}

		/// <summary>
		/// Ftp listing line parser
		/// </summary>
		/// <param name="line">The line from the listing</param>
		/// <param name="capabilities">The server capabilities</param>
		/// <param name="client">The FTP client</param>
		/// <returns>FtpListItem if the line can be parsed, null otherwise</returns>
		public delegate FtpListItem Parser(string line, List<FtpCapability> capabilities, FtpClient client);

		/// <summary>
		/// Parses a line from a file listing using the first successful match in the Parsers collection.
		/// </summary>
		/// <param name="path">The source path of the file listing</param>
		/// <param name="buf">A line from the file listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <returns>A FtpListItem object representing the parsed line, null if the line was
		/// unable to be parsed. If you have encountered an unsupported list type add a parser
		/// to the public static Parsers collection of FtpListItem.</returns>
		public static FtpListItem ParseLegacy(string path, string buf, List<FtpCapability> capabilities, FtpClient client) {
			if (!string.IsNullOrEmpty(buf)) {
				FtpListItem item;

				foreach (var parser in Parsers) {
					if ((item = parser(buf, capabilities, client)) != null) {
						item.Input = buf;
						return item;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Initializes the default list of parsers
		/// </summary>
		public static void InitParsers() {
			lock (m_parserLock) {
				if (m_parsers == null) {
					m_parsers = new List<Parser>();
					m_parsers.Add(new Parser(FtpMachineListParser.Parse));
					m_parsers.Add(new Parser(FtpUnixParser.ParseLegacy));
					m_parsers.Add(new Parser(FtpWindowsParser.ParseLegacy));
					m_parsers.Add(new Parser(FtpVMSParser.ParseLegacy));
				}
			}
		}

		public static List<Parser> m_parsers = null;

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
		public static Parser[] Parsers {
			get {
				Parser[] parsers;

				lock (m_parserLock) {
					if (m_parsers == null) {
						InitParsers();
					}

					parsers = m_parsers.ToArray();
				}

				return parsers;
			}
		}

		private static Parser m_customParser;

		#endregion
	}
}