using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Exceptions;

#if NET45
using System.Threading.Tasks;
#endif

namespace FluentFTP.Helpers.Parsers {
	internal class FtpIBMParser {

		private static int formatIndex = 0;

		/// <summary>
		/// Checks if the given listing is a valid IBM OS/400 file listing
		/// </summary>
		public static bool IsValid(FtpClient client, string[] listing) {
			int count = Math.Min(listing.Length, 10);
			
			for (int i = 0; i < count; i++) {
				if (listing[i].ContainsAny(ValidListFormats, 0)) {
					return true;
				}
			}
			client.LogStatus(FtpTraceLevel.Verbose, "Not in OS/400 format");
			return false;
		}

		/// <summary>
		/// Parses IBM OS/400 format listings
		/// </summary>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(FtpClient client, string record) {

			string[] values = record.SplitString();

			// skip blank lines
			if (values.Length <= 0) {
				return null;
			}
			// return what we can for MEM
			if (values.Length >= 2 && values[1].Equals(MemoryMarker)) {
				DateTime lastModifiedm = DateTime.MinValue;
				string ownerm = values[0];
				string namem = values[2];
				FtpListItem filem = new FtpListItem(record, namem, 0, false, ref lastModifiedm);
				filem.RawOwner = ownerm;
				return filem;
			}
			if (values.Length < MinFieldCount) {
				return null;
			}

			// first field is owner
			string owner = values[0];

			// next is size
			long size = Int64.Parse(values[1]);

			string lastModifiedStr = values[2] + " " + values[3];
			DateTime lastModified = ParseDateTime(client, lastModifiedStr);

			// test is dir
			bool isDir = false;
			if (values[4] == DirectoryMarker || values[4] == DDirectoryMarker || (values.Length == 5 && values[4] == FileMarker)) {
				isDir = true;
			}

			// If there's no name, it's because we're inside a file.  Fake out a "current directory" name instead.
			string name = values.Length >= 6 ? values[5] : ".";
			if (name.EndsWith("/")) {
				isDir = true;
				name = name.Substring(0, name.Length - 1);
			}

			// create a new list item object with the parsed metadata
			FtpListItem file = new FtpListItem(record, name, size, isDir, ref lastModified);
			file.RawOwner = owner;
			return file;
		}

		/// <summary>
		/// Parses the last modified date from IBM OS/400 format listings
		/// </summary>
		private static DateTime ParseDateTime(FtpClient client, string lastModifiedStr) {
			DateTime lastModified = DateTime.MinValue;
			if (formatIndex >= DateTimeFormats.Length) {
				client.LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
				return DateTime.MinValue;
			}
			int prevIndex = formatIndex;
			for (int i = formatIndex; i < DateTimeFormats.Length; i++, formatIndex++) {
				try {
					lastModified = DateTime.ParseExact(lastModifiedStr, DateTimeFormats[formatIndex], client.ListingCulture.DateTimeFormat, DateTimeStyles.None);
					if (lastModified > DateTime.Now.AddDays(2)) {
						client.LogStatus(FtpTraceLevel.Verbose, "Swapping to alternate format (found date in future)");
						continue;
					} else {
						break;
					}
				} catch (FormatException) {
					continue;
				}
			}
			if (formatIndex >= DateTimeFormats.Length) {
				client.LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
				return DateTime.MinValue;
			}
			if (formatIndex > prevIndex) // we've changed FTP formatters so redo
			{
				throw new FtpListParseException();
			}
			return lastModified;
		}

		#region Constants

		private static string DirectoryMarker = "*DIR";
		private static string DDirectoryMarker = "*DDIR";
		private static string MemoryMarker = "*MEM";
		private static string FileMarker = "*FILE";
		private static int MinFieldCount = 5;
		private static string[][] DateTimeFormats = { new string[] { "dd'/'MM'/'yy' 'HH':'mm':'ss", "dd'/'MM'/'yyyy' 'HH':'mm':'ss", "dd'.'MM'.'yy' 'HH':'mm':'ss" }, new string[] { "yy'/'MM'/'dd' 'HH':'mm':'ss", "yyyy'/'MM'/'dd' 'HH':'mm':'ss", "yy'.'MM'.'dd' 'HH':'mm':'ss" }, new string[] { "MM'/'dd'/'yy' 'HH':'mm':'ss", "MM'/'dd'/'yyyy' 'HH':'mm':'ss", "MM'.'dd'.'yy' 'HH':'mm':'ss" } };
		private static string[] ValidListFormats = new string[] { "*DIR", "*FILE", "*FLR", "*DDIR", "*STMF", "*LIB" };

		#endregion

	}
}