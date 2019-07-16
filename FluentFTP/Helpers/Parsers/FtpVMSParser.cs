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

namespace FluentFTP.Helpers.Parsers {
	internal class FtpVMSParser {

		/// <summary>
		/// Parses Vax/VMS format listings
		/// </summary>
		public static FtpListItem ParseLegacy(string record, FtpCapability capabilities, FtpClient client) {
			string regex =
				@"(?<name>.+)\.(?<extension>.+);(?<version>\d+)\s+" +
				@"(?<size>\d+)\s+" +
				@"(?<modify>\d+-\w+-\d+\s+\d+:\d+)";
			Match m;

			if ((m = Regex.Match(record, regex)).Success) {
				FtpListItem item = new FtpListItem();

				item.Name = (m.Groups["name"].Value + "." + m.Groups["extension"].Value + ";" + m.Groups["version"].Value);

				if (m.Groups["extension"].Value.ToUpper() == "DIR") {
					item.Type = FtpFileSystemObjectType.Directory;
				} else {
					item.Type = FtpFileSystemObjectType.File;
				}
				long itemSize = 0;
				if (!long.TryParse(m.Groups["size"].Value, out itemSize)) {
					itemSize = -1;
				}

				item.Size = itemSize;

				DateTime itemModified = DateTime.MinValue;

				if (!DateTime.TryParse(m.Groups["modify"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out itemModified)) {
					itemModified = DateTime.MinValue;
				}

				item.Modified = itemModified;

				return item;
			}

			return null;
		}

		/// <summary>
		/// Checks if the given listing is a valid VMS file listing
		/// </summary>
		public static bool IsValid(FtpClient client, string[] records) {
			int count = Math.Min(records.Length, 10);

			bool semiColonName = false;
			bool squareBracketStart = false, squareBracketEnd = false;
			
			for (int i = 0; i < count; i++) {
				var record = records[i];
				if (record.Trim().Length == 0) {
					continue;
				}
				int pos = 0;
				if ((pos = record.IndexOf(';')) > 0 && (++pos < record.Length) &&
					Char.IsDigit(record[pos])) {
					semiColonName = true;
				}
				if (record.IndexOf('[') > 0) {
					squareBracketStart = true;
				}
				if (record.IndexOf(']') > 0) {
					squareBracketEnd = true;
				}
			}
			if (semiColonName && squareBracketStart && squareBracketEnd) {
				return true;
			}
			client.LogStatus(FtpTraceLevel.Verbose, "Not in VMS format");
			return false;
		}

		/// <summary>
		/// Parses Vax/VMS format listings
		/// </summary>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(FtpClient client, string record) {

			string[] values = record.SplitString();

			// skip blank lines
			if (values.Length <= 0) {
				return null;
			}
			// skip line which lists Directory
			if (values.Length >= 2 && values[0].Equals(HDirectoryMarker)) {
				return null;
			}
			// skip line which lists Total
			if (values.Length > 0 && values[0].Equals(TotalMarker)) {
				return null;
			}
			if (values.Length < MinFieldCount) {
				return null;
			}

			// first field is name
			string name = values[0];

			// make sure it is the name (ends with ';<INT>')
			int semiPos = name.LastIndexOf(';');
			// check for ;
			if (semiPos <= 0) {
				client.LogStatus(FtpTraceLevel.Verbose, "File version number not found in name '" + name + "'");
				return null;
			}

			string nameNoVersion = name.Substring(0, semiPos);

			// check for version after ;
			string afterSemi = values[0].Substring(semiPos + 1);
			try {
				Int64.Parse(afterSemi);
				// didn't throw exception yet, must be number
				// we don't use it currently but we might in future
			} catch (FormatException) {
				// don't worry about version number
			}

			// test is dir
			bool isDir = false;
			if (nameNoVersion.EndsWith(DirectoryMarker)) {
				isDir = true;
				name = nameNoVersion.Substring(0, nameNoVersion.Length - DirectoryMarker.Length);
			}

			if (!FtpListParser.VMSNameHasVersion && !isDir) {
				name = nameNoVersion;
			}

			// 2nd field is size USED/ALLOCATED format, or perhaps just USED
			long size = ParseFileSize(values[1]);

			// 3 & 4 fields are date time
			DateTime lastModified = ParseDateTime(client, values[2], values[3]);

			// 5th field is [group,owner]
			string group = null;
			string owner = null;
			ParseGroupOwner(values, out group, out owner);

			// 6th field is permissions e.g. (RWED,RWED,RE,)
			string permissions = ParsePermissions(values);

			// create a new list item object with the parsed metadata
			FtpListItem file = new FtpListItem(record, name, size, isDir, ref lastModified);
			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			return file;
		}

		/// <summary>
		/// Parses the file size from Vax/VMS format listings
		/// </summary>
		private static long ParseFileSize(string sizeStr) {
			long size;
			int slashPos = sizeStr.IndexOf('/');
			string sizeUsed = sizeStr;
			if (slashPos == -1) {

				// only filesize in bytes
				size = Int64.Parse(sizeStr);

			} else {
				if (slashPos > 0) {
					sizeUsed = sizeStr.Substring(0, slashPos);
				}
				size = Int64.Parse(sizeUsed) * FileBlockSize;
			}

			return size;
		}

		/// <summary>
		/// Parses the owner and group permissions from Vax/VMS format listings
		/// </summary>
		private static void ParseGroupOwner(string[] values, out string group, out string owner) {
			group = null;
			owner = null;
			if (values.Length >= 5) {
				if (values[4][0] == '[' && values[4][values[4].Length - 1] == ']') {
					int commaPos = values[4].IndexOf(',');
					if (commaPos < 0) {
						owner = values[4].Substring(1, values[4].Length - 2);
						group = "";
					} else {
						group = values[4].Substring(1, commaPos - 1);
						owner = values[4].Substring(commaPos + 1, values[4].Length - commaPos - 2);
					}
				}
			}
		}

		/// <summary>
		/// Parses the permissions from Vax/VMS format listings
		/// </summary>
		private static string ParsePermissions(string[] values) {
			if (values.Length >= 6) {
				if (values[5][0] == '(' && values[5][values[5].Length - 1] == ')') {
					return values[5].Substring(1, values[5].Length - 2);
				}
			}
			return null;
		}

		/// <summary>
		/// Parses the last modified date from Vax/VMS format listings
		/// </summary>
		private static DateTime ParseDateTime(FtpClient client, string date, string time) {
			StringBuilder sb = new StringBuilder();
			bool monthFound = false;

			// add date
			for (int i = 0; i < date.Length; i++) {
				if (!Char.IsLetter(date[i])) {
					sb.Append(date[i]);
				} else {
					if (!monthFound) {
						sb.Append(date[i]);
						monthFound = true;
					} else {
						// convert the last 2 chars of month to lower case
						sb.Append(Char.ToLower(date[i]));
					}
				}
			}

			// add time
			sb.Append(" ").Append(time);
			var lastModifiedStr = sb.ToString();

			// parse it into a date/time object
			DateTime lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.Parse(lastModifiedStr, client.ListingCulture.DateTimeFormat);
			} catch (FormatException) {
				client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}
			return lastModified;
		}

		#region Constants

		private static string DirectoryMarker = ".DIR";
		private static string HDirectoryMarker = "Directory";
		private static string TotalMarker = "Total";
		private static int MinFieldCount = 4;
		private static int FileBlockSize = 512 * 1024;

		#endregion

	}
}