using FluentFTP.Helpers.Parsers;
using System;
using System.Collections.Generic;

namespace FluentFTP.Helpers {
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
			FtpParser.Unix, FtpParser.Windows, FtpParser.VMS, FtpParser.IBMzOS, FtpParser.IBMOS400, FtpParser.NonStop
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
		public void Init(FtpOperatingSystem system, FtpParser forcedParser = FtpParser.Auto) {
			ParserConfirmed = false;

			if (forcedParser != FtpParser.Auto) {
				// use the parser that the server handler specified
				CurrentParser = forcedParser;
			}
			else {

				if (system == FtpOperatingSystem.Windows) {
					CurrentParser = FtpParser.Windows;
				}
				else if (system == FtpOperatingSystem.Unix || system == FtpOperatingSystem.SunOS) {
					CurrentParser = FtpParser.Unix;
				}
				else if (system == FtpOperatingSystem.VMS) {
					CurrentParser = FtpParser.VMS;
				}
				else if (system == FtpOperatingSystem.IBMzOS) {
					CurrentParser = FtpParser.IBMzOS;
				}
				else if (system == FtpOperatingSystem.IBMOS400) {
					CurrentParser = FtpParser.IBMOS400;
				}
				else
				{
					CurrentParser = FtpParser.Unix;
					client.LogStatus(FtpTraceLevel.Warn, "Cannot auto-detect listing parser for system '" + system + "', using Unix parser");
				}
			}

			DetectedParser = CurrentParser;

			client.LogStatus(FtpTraceLevel.Verbose, "Listing parser set to: " + DetectedParser.ToString());
		}

		/// <summary>
		/// Parse raw file from server into a file object, using the currently active parser.
		/// </summary>
		public FtpListItem ParseSingleLine(string path, string file, List<FtpCapability> caps, bool isMachineList) {
			FtpListItem result = null;

			// force machine listing if it is
			if (isMachineList) {
				result = MachineListParser.Parse(file, caps, client);
			}
			else {
				// use custom parser if given
				if (client.ListingParser == FtpParser.Custom && client.ListingCustomParser != null) {
					result = client.ListingCustomParser(file, caps, client);
				}
				else {
					if (IsWrongParser()) {
						ValidateParser(new[] {file});
					}

					// use one of the in-built parsers
					switch (CurrentParser) {
						case FtpParser.Machine:
							result = MachineListParser.Parse(file, caps, client);
							break;

						case FtpParser.Windows:
							result = WindowsParser.Parse(client, file);
							break;

						case FtpParser.Unix:
							result = UnixParser.Parse(client, file);
							break;

						case FtpParser.UnixAlt:
							result = UnixParser.ParseUnixAlt(client, file);
							break;

						case FtpParser.VMS:
							result = VMSParser.Parse(client, file);
							break;

						case FtpParser.IBMzOS:
							result = IBMzOSParser.Parse(client, file, path);
							break;

						case FtpParser.IBMOS400:
							result = IBMOS400Parser.Parse(client, file);
							break;

						case FtpParser.NonStop:
							result = NonStopParser.Parse(client, file);
							break;
					}
				}
			}

			// if parsed file successfully
			if (result != null) {

				// process created date into the timezone required
				result.RawCreated = result.Created;
				if (result.Created != DateTime.MinValue) {
					result.Created = client.ConvertDate(result.Created);
				}

				// process modified date into the timezone required
				result.RawModified = result.Modified;
				if (result.Modified != DateTime.MinValue) {
					result.Modified = client.ConvertDate(result.Modified);
				}

				// calc absolute file paths

				bool? handledByCustom = null;

				if (client.ServerHandler != null && client.ServerHandler.IsCustomCalculateFullFtpPath()) {
					handledByCustom = client.ServerHandler.CalculateFullFtpPath(client, path, result);
				}

				if (handledByCustom == null) {
					result.CalculateFullFtpPath(client, path);
				}
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
					return WindowsParser.IsValid(client, files);

				case FtpParser.Unix:
					return UnixParser.IsValid(client, files);

				case FtpParser.VMS:
					return VMSParser.IsValid(client, files);

				case FtpParser.IBMzOS:
					return IBMzOSParser.IsValid(client, files);

				case FtpParser.IBMOS400:
					return IBMOS400Parser.IsValid(client, files);

				case FtpParser.NonStop:
					return NonStopParser.IsValid(client, files);

				case FtpParser.Machine:
					return MachineListParser.IsValid(client, files);
			}

			return false;
		}

		#endregion

	}
}
