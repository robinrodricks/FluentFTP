using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentFTP.Client.BaseClient;

namespace FluentFTP.Helpers.Parsers {
	internal static class UnixParser {

		/// <summary>
		/// Checks if the given listing is a valid Unix file listing
		/// </summary>
		public static bool IsValid(BaseFtpClient client, string[] records) {
			var count = Math.Min(records.Length, 10);

			var perms1 = false;
			var perms2 = false;

			for (var i = 0; i < count; i++) {
				var record = records[i];
				if (record.Trim().Length == 0) {
					continue;
				}

				var values = record.SplitString();
				if (values.Length < MinFieldCount) {
					continue;
				}

				// check perms
				var ch00 = char.ToLower(values[0][0]);
				if (ch00 == '-' || ch00 == 'l' || ch00 == 'd') {
					perms1 = true;
				}

				if (values[0].Length > 1) {
					var ch01 = char.ToLower(values[0][1]);
					if (ch01 == 'r' || ch01 == '-') {
						perms2 = true;
					}
				}

				// last chance - Connect:Enterprise has -ART------TCP
				if (!perms2 && values[0].Length > 2 && values[0].IndexOf('-', 2) > 0) {
					perms2 = true;
				}
			}

			if (perms1 && perms2) {
				return true;
			}

			((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, "Not in UNIX format");
			return false;
		}

		/// <summary>
		/// Parses Unix format listings
		/// </summary>
		/// <param name="client">The FTP client</param>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(BaseFtpClient client, string record) {
			// test it is a valid line, e.g. "total 342522" is invalid
			var ch = record[0];
			if (ch != FileMarker && ch != DirectoryMarker && ch != SymbolicLinkMarker) {
				return null;
			}

			var values = record.SplitString();

			if (values.Length < MinFieldCount) {
				var msg = new StringBuilder("Unexpected number of fields in listing '");
				msg
					.Append(record)
					.Append("' - expected minimum ").Append(MinFieldCount)
					.Append(" fields but found ").Append(values.Length).Append(" fields");
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Verbose, msg.ToString());
				return null;
			}

			// field pos
			var index = 0;

			// first field is perms
			string permissions;
			bool isDir, isLink;
			ParsePermissions(values, ref index, out permissions, out isDir, out isLink);

			// some servers don't supply the link count
			var linkCount = ParseLinkCount(client, values, ref index);

			// parse owner & group permissions
			string owner, group;
			ParseOwnerGroup(values, ref index, out owner, out group);

			// parse size
			var size = ParseFileSize(client, values, ref index);

			// parse the date/time fields
			var dayOfMonth = ParseDayOfMonth(values, ref index);

			var dateTimePos = index;
			var lastModified = DateTime.MinValue;
			ParseDateTime(client, values, ref index, dayOfMonth, ref lastModified);

			// parse name of file or dir. Extract symlink if possible
			string name = null;
			string linkedname = null;
			ParseName(client, record, values, isLink, dayOfMonth, dateTimePos, ref name, ref linkedname);

			// create a new list item object with the parsed metadata
			var file = new FtpListItem(record, name, size, isDir, lastModified);
			if (isLink) {
				file.Type = FtpObjectType.Link;
				file.LinkCount = linkCount;
				file.LinkTarget = linkedname.Trim();
			}

			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			file.CalculateUnixPermissions(permissions);
			return file;
		}

		/// <summary>
		/// Parses the permissions from Unix format listings
		/// </summary>
		private static int ParsePermissions(string[] values, ref int index, out string permissions, out bool isDir, out bool isLink) {
			permissions = values[index++];
			var ch = permissions[0];
			isDir = false;
			isLink = false;
			if (ch == DirectoryMarker) {
				isDir = true;
			}
			else if (ch == SymbolicLinkMarker) {
				isLink = true;
			}

			return index;
		}

		/// <summary>
		/// Parses the link count from Unix format listings
		/// </summary>
		private static int ParseLinkCount(BaseFtpClient client, string[] values, ref int index) {
			var linkCount = 0;
			if (char.IsDigit(values[index][0])) {
				// assume it is if a digit
				var linkCountStr = values[index++];
				try {
					linkCount = int.Parse(linkCountStr);
				}
				catch (FormatException) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse link count: " + linkCountStr);
				}
			}
			else if (values[index][0] == '-') {
				// IPXOS Treck FTP server
				index++;
			}

			return linkCount;
		}

		/// <summary>
		/// Parses the owner and group permissions from Unix format listings
		/// </summary>
		private static void ParseOwnerGroup(string[] values, ref int index, out string owner, out string group) {
			// owner and group
			owner = "";
			group = "";

			// if 2 fields ahead is numeric and there's enough fields beyond (4) for
			// the date, then the next two fields should be the owner & group
			if (values[index + 2].IsNumeric() && values.Length - (index + 2) > 4) {
				owner = values[index++];
				group = values[index++];
			}

			// no owner
			else if (values[index + 1].IsNumeric() && values.Length - (index + 1) > 4) {
				group = values[index++];
			}
		}

		/// <summary>
		/// Parses the file size from Unix format listings
		/// </summary>
		private static long ParseFileSize(BaseFtpClient client, string[] values, ref int index) {
			var size = 0L;
			var sizeStr = values[index++].Replace(".", ""); // get rid of .'s in size           
			try {
				size = long.Parse(sizeStr);
			}
			catch (FormatException) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + sizeStr);
			}

			return size;
		}

		/// <summary>
		/// Parses day-of-month from Unix format listings
		/// </summary>
		private static int ParseDayOfMonth(string[] values, ref int index) {
			var dayOfMonth = -1;

			// we expect the month first on Unix. 
			// Connect:Enterprise UNIX has a weird extra numeric field here - we test if the 
			// next field is numeric and if so, we skip it (except we check for a BSD variant
			// that means it is the day of the month)
			if (values[index].IsNumeric()) {
				// this just might be the day of month - BSD variant
				// we check it is <= 31 AND that the next field starts
				// with a letter AND the next has a ':' within it
				try {
					char[] chars = { '0' };
					var str = values[index].TrimStart(chars);
					dayOfMonth = int.Parse(values[index]);
					if (dayOfMonth > 31) // can't be day of month
					{
						dayOfMonth = -1;
					}

					if (!char.IsLetter(values[index + 1][0])) {
						dayOfMonth = -1;
					}

					if (values[index + 2].IndexOf(':') <= 0) {
						dayOfMonth = -1;
					}
				}
				catch (FormatException) {
				}

				index++;
			}

			return dayOfMonth;
		}

		/// <summary>
		/// Parses the file or folder name from Unix format listings
		/// </summary>
		private static void ParseName(BaseFtpClient client, string record, string[] values, bool isLink, int dayOfMonth, int dateTimePos, ref string name, ref string linkedname) {
			// find the starting point of the name by finding the pos of all the date/time fields
			var pos = 0;
			var ok = true;
			var dateFieldCount = dayOfMonth > 0 ? 2 : 3; // only 2 fields left if we had a leading day of month
			for (var i = dateTimePos; i < dateTimePos + dateFieldCount; i++) {
				pos = record.IndexOf(values[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				}
				else {
					pos += values[i].Length;
				}
			}

			if (ok) {
				var remainder = record.Substring(pos).Trim();
				if (!isLink) {
					name = remainder;
				}
				else {
					// symlink, try to extract it
					pos = remainder.IndexOf(SymbolicLinkArrowMarker);
					if (pos <= 0) {
						// couldn't find symlink, give up & just assign as name
						name = remainder;
					}
					else {
						var len = SymbolicLinkArrowMarker.Length;
						name = remainder.Substring(0, pos - 0).Trim();
						if (pos + len < remainder.Length) {
							linkedname = remainder.Substring(pos + len);
						}
					}
				}
			}
			else {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + record);
			}
		}

		/// <summary>
		/// Parses the last modified date from Unix format listings
		/// </summary>
		private static void ParseDateTime(BaseFtpClient client, string[] values, ref int index, int dayOfMonth, ref DateTime lastModified) {
			var stamp = new StringBuilder(values[index++]);
			stamp.Append('-');
			if (dayOfMonth > 0) {
				stamp.Append(dayOfMonth);
			}
			else {
				stamp.Append(values[index++]);
			}

			stamp.Append('-');

			var field = values[index++];
			if (field.IndexOf(':') < 0 && field.IndexOf('.') < 0) {
				stamp.Append(field); // year
				lastModified = ParseYear(client, stamp, DateTimeFormats1);
			}
			else {
				// add the year ourselves as not present
				var year = client.Config.ListingCulture.Calendar.GetYear(DateTime.Now);
				stamp.Append(year).Append('-').Append(field);
				lastModified = ParseDateTime(client, stamp, DateTimeFormats2);
			}
		}

		/// <summary>
		/// Parses the last modified year from Unix format listings
		/// </summary>
		private static DateTime ParseYear(BaseFtpClient client, StringBuilder stamp, string[] format) {
			var lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.ParseExact(stamp.ToString(), format, client.Config.ListingCulture.DateTimeFormat, DateTimeStyles.None);
			}
			catch (FormatException) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
			}

			return lastModified;
		}

		/// <summary>
		/// Parses the last modified date from Unix format listings
		/// </summary>
		private static DateTime ParseDateTime(BaseFtpClient client, StringBuilder stamp, string[] format) {
			var lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.ParseExact(stamp.ToString(), format, client.Config.ListingCulture.DateTimeFormat, DateTimeStyles.None);
			}
			catch (FormatException) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + stamp.ToString() + "'");
			}

			// can't be in the future - must be the previous year
			// add 2 days for time zones
			if (lastModified > DateTime.Now.AddDays(2)) {
				lastModified = lastModified.AddYears(-1);
			}

			return lastModified;
		}

		/// <summary>
		/// Parses Unix format listings with alternate parser
		/// </summary>
		/// <param name="client">The FTP client</param>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem ParseUnixAlt(BaseFtpClient client, string record) {
			// test it is a valid line, e.g. "total 342522" is invalid
			var ch = record[0];
			if (ch != FileMarker && ch != DirectoryMarker && ch != SymbolicLinkMarker) {
				return null;
			}

			var values = record.SplitString();

			if (values.Length < MinFieldCountAlt) {
				var listing = new StringBuilder("Unexpected number of fields in listing '");
				listing.Append(record)
					.Append("' - expected minimum ").Append(MinFieldCountAlt)
					.Append(" fields but found ").Append(values.Length).Append(" fields");
				throw new FormatException(listing.ToString());
			}

			// field pos
			var index = 0;

			// first field is perms
			var permissions = values[index++];
			ch = permissions[0];
			var isDir = false;
			var isLink = false;
			if (ch == DirectoryMarker) {
				isDir = true;
			}
			else if (ch == SymbolicLinkMarker) {
				isLink = true;
			}

			var group = values[index++];

			// some servers don't supply the link count
			var linkCount = 0;
			if (char.IsDigit(values[index][0])) {
				// assume it is if a digit
				var linkCountStr = values[index++];
				try {
					linkCount = int.Parse(linkCountStr);
				}
				catch (FormatException) {
					((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse link count: " + linkCountStr);
				}
			}

			var owner = values[index++];


			// size
			var size = 0L;
			var sizeStr = values[index++];
			try {
				size = long.Parse(sizeStr);
			}
			catch (FormatException) {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + sizeStr);
			}

			// next 3 fields are the date time

			// we expect the month first on Unix. 
			var dateTimePos = index;
			var lastModified = DateTime.MinValue;
			var stamp = new StringBuilder(values[index++]);
			stamp.Append('-').Append(values[index++]).Append('-');

			var field = values[index++];
			if (field.IndexOf(':') < 0) {
				stamp.Append(field); // year
				lastModified = ParseYear(client, stamp, DateTimeAltFormats1);
			}
			else {
				// add the year ourselves as not present
				var year = client.Config.ListingCulture.Calendar.GetYear(DateTime.Now);
				stamp.Append(year).Append('-').Append(field);
				lastModified = ParseDateTime(client, stamp, DateTimeAltFormats2);
			}

			// name of file or dir. Extract symlink if possible
			string name = null;

			// find the starting point of the name by finding the pos of all the date/time fields
			var pos = 0;
			var ok = true;
			for (var i = dateTimePos; i < dateTimePos + 3; i++) {
				pos = record.IndexOf(values[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				}
				else {
					pos += values[i].Length;
				}
			}

			if (ok) {
				name = record.Substring(pos).Trim();
			}
			else {
				((IInternalFtpClient)client).LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + record);
			}

			// create a new list item object with the parsed metadata
			var file = new FtpListItem(record, name, size, isDir, lastModified);
			if (isLink) {
				file.Type = FtpObjectType.Link;
				file.LinkCount = linkCount;
			}

			file.RawGroup = group;
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			file.CalculateUnixPermissions(permissions);
			return file;
		}

		#region Constants

		private static string SymbolicLinkArrowMarker = "->";
		private static char SymbolicLinkMarker = 'l';
		private static char FileMarker = '-';
		private static char DirectoryMarker = 'd';
		private static int MinFieldCount = 7;
		private static int MinFieldCountAlt = 8;
		private static string[] DateTimeFormats1 = { "MMM'-'d'-'yyyy", "MMM'-'dd'-'yyyy" };
		private static string[] DateTimeFormats2 = { "MMM'-'d'-'yyyy'-'HH':'mm", "MMM'-'dd'-'yyyy'-'HH':'mm", "MMM'-'d'-'yyyy'-'H':'mm", "MMM'-'dd'-'yyyy'-'H':'mm", "MMM'-'dd'-'yyyy'-'H'.'mm" };
		private static string[] DateTimeAltFormats1 = { "MMM'-'d'-'yyyy", "MMM'-'dd'-'yyyy" };
		private static string[] DateTimeAltFormats2 = { "MMM'-'d'-'yyyy'-'HH':'mm:ss", "MMM'-'dd'-'yyyy'-'HH':'mm:ss", "MMM'-'d'-'yyyy'-'H':'mm:ss", "MMM'-'dd'-'yyyy'-'H':'mm:ss" };

		#endregion
	}
}