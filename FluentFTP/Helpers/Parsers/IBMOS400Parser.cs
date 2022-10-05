using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Exceptions;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Helpers.Parsers {
	internal static class IBMOS400Parser {
		private static int formatIndex = 0;

		/// <summary>
		/// Checks if the given listing is a valid IBM OS/400 file listing
		/// </summary>
		public static bool IsValid(BaseFtpClient client, string[] listing) {
			var count = Math.Min(listing.Length, 10);

			for (var i = 0; i < count; i++) {
				if (listing[i].ContainsAny(ValidListFormats, 0)) {
					return true;
				}
			}

			((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Not in OS/400 format");
			return false;
		}

		/// <summary>
		/// Parses IBM OS/400 format listings
		/// </summary>
		/// <param name="client">The FTP client</param>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(BaseFtpClient client, string record) {
			var values = record.SplitString();

			// skip blank lines
			if (values.Length <= 0) {
				return null;
			}

			// return what we can for MEM
			if (values.Length >= 2 && values[1].Equals(MemoryMarker)) {
				var lastModifiedm = DateTime.MinValue;
				var ownerm = values[0];
				var namem = values[2];
				var filem = new FtpListItem(record, namem, 0, false, lastModifiedm);
				filem.RawOwner = ownerm;
				return filem;
			}

			if (values.Length < MinFieldCount) {
				return null;
			}

			// first field is owner
			var owner = values[0];

			// next is size
			var size = long.Parse(values[1]);

			var lastModifiedStr = values[2] + " " + values[3];
			var lastModified = ParseDateTime(client, lastModifiedStr);

			// test is dir
			var isDir = false;
			if (values[4] == DirectoryMarker || values[4] == DDirectoryMarker || values.Length == 5 && values[4] == FileMarker) {
				isDir = true;
			}

			string name = string.Empty;
			// If there's no name, it's because we're inside a file.  Fake out a "current directory" name instead.
			if (values.Length >= 6) {
				name = record.Substring(record.TrimEnd().LastIndexOf(values[5]));
			}
			else {
				name = ".";
			}
			if (name.EndsWith("/")) {
				isDir = true;
				name = name.Substring(0, name.Length - 1);
			}

			// create a new list item object with the parsed metadata
			var file = new FtpListItem(record, name, size, isDir, lastModified);
			file.RawOwner = owner;
			return file;
		}

		/// <summary>
		/// Parses the last modified date from IBM OS/400 format listings
		/// </summary>
		private static DateTime ParseDateTime(BaseFtpClient client, string lastModifiedStr) {
			var lastModified = DateTime.MinValue;
			if (formatIndex >= DateTimeFormats.Length) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
				return DateTime.MinValue;
			}

			var prevIndex = formatIndex;
			for (var i = formatIndex; i < DateTimeFormats.Length; i++, formatIndex++) {
				try {
					lastModified = DateTime.ParseExact(lastModifiedStr, DateTimeFormats[formatIndex], client.Config.ListingCulture.DateTimeFormat, DateTimeStyles.None);
					if (lastModified > DateTime.Now.AddDays(2)) {
						((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Swapping to alternate format (found date in future)");
						continue;
					}
					else {
						break;
					}
				}
				catch (FormatException) {
					continue;
				}
			}

			if (formatIndex >= DateTimeFormats.Length) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Warn, "Exhausted formats - failed to parse date");
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
		private static string[][] DateTimeFormats = { new[] { "dd'/'MM'/'yy' 'HH':'mm':'ss", "dd'/'MM'/'yyyy' 'HH':'mm':'ss", "dd'.'MM'.'yy' 'HH':'mm':'ss" }, new[] { "yy'/'MM'/'dd' 'HH':'mm':'ss", "yyyy'/'MM'/'dd' 'HH':'mm':'ss", "yy'.'MM'.'dd' 'HH':'mm':'ss" }, new[] { "MM'/'dd'/'yy' 'HH':'mm':'ss", "MM'/'dd'/'yyyy' 'HH':'mm':'ss", "MM'.'dd'.'yy' 'HH':'mm':'ss" } };
		private static string[] ValidListFormats = new[] { "*DIR", "*FILE", "*FLR", "*DDIR", "*STMF", "*LIB" };

		#endregion
	}
}