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
	internal static class WindowsParser {

		/// <summary>
		/// Checks if the given listing is a valid IIS/DOS file listing
		/// </summary>
		public static bool IsValid(FtpClient client, string[] records) {
			var count = Math.Min(records.Length, 10);

			var dateStart = false;
			var timeColon = false;
			var dirOrFile = false;

			for (var i = 0; i < count; i++) {
				var record = records[i];
				if (record.Trim().Length == 0) {
					continue;
				}

				var values = record.SplitString();
				if (values.Length < MinFieldCount) {
					continue;
				}

				// first & last chars are digits of first field
				if (char.IsDigit(values[0][0]) && char.IsDigit(values[0][values[0].Length - 1])) {
					dateStart = true;
				}

				if (values[1].IndexOf(':') > 0) {
					timeColon = true;
				}

				if (values[2].ToUpper() == DirectoryMarker || char.IsDigit(values[2][0])) {
					dirOrFile = true;
				}
			}

			if (dateStart && timeColon && dirOrFile) {
				return true;
			}

			client.LogStatus(FtpTraceLevel.Verbose, "Not in Windows format");
			return false;
		}

		/// <summary>
		/// Parses IIS/DOS format listings
		/// </summary>
		/// <param name="client">The FTP client</param>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(FtpClient client, string record) {
			var values = record.SplitString();

			if (values.Length < MinFieldCount) {
				return null;
			}

			// parse date & time
			var lastModified = ParseDateTime(client, values[0] + " " + values[1]);

			// parse dir flag & file size
			bool isDir;
			long size;
			ParseTypeAndFileSize(client, values[2], out isDir, out size);

			// parse name of file or folder
			var name = ParseName(client, record, values, isDir);

			return new FtpListItem(record, name, size, isDir, lastModified);
		}

		/// <summary>
		/// Parses the file or folder name from IIS/DOS format listings
		/// </summary>
		private static string ParseName(FtpClient client, string record, string[] values, bool isDir) {
			// Find starting point of the name by finding the pos of all the date/time fields.
			var pos = 0;
			var ok = true;
			for (var i = 0; i < 3; i++) {
				pos = record.IndexOf(values[i], pos);
				if (pos < 0) {
					ok = false;
					break;
				}
				else {
					pos += values[i].Length;
				}
			}

			string name = null;
			if (ok) {
				//---------------------------------------
				//03-24-22  09:50AM       <DIR>          TestSubDirectory01
				//03-24-22  09:50AM                   31 TextFileA.txt
				//---------------------------------------
				name = isDir ? record.Substring(pos + OffsetLengthDirectory) : record.Substring(pos + OffsetLengthFile);
			}
			else {
				client.LogStatus(FtpTraceLevel.Error, "Failed to retrieve name: " + record);
			}

			return name;
		}

		/// <summary>
		/// Parses the file size and checks if the item is a directory from IIS/DOS format listings
		/// </summary>
		private static void ParseTypeAndFileSize(FtpClient client, string type, out bool isDir, out long size) {
			isDir = false;
			size = 0L;
			if (type.ToUpper().Equals(DirectoryMarker.ToUpper())) {
				isDir = true;
			}
			else {
				try {
					size = long.Parse(type.Replace(",", ""));
				}
				catch (FormatException) {
					client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + type);
				}
			}
		}

		/// <summary>
		/// Parses the last modified date from IIS/DOS format listings
		/// </summary>
		private static DateTime ParseDateTime(FtpClient client, string lastModifiedStr) {
			try {
				var lastModified = DateTime.ParseExact(lastModifiedStr, DateTimeFormats, client.ListingCulture.DateTimeFormat, DateTimeStyles.None);
				return lastModified;
			}
			catch (FormatException) {
				client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}

			return DateTime.MinValue;
		}

		#region Constants

		private static string DirectoryMarker = "<DIR>";
		private static int MinFieldCount = 4;
		private static string[] DateTimeFormats = {
			"MM'-'dd'-'yy hh':'mmtt", "MM'-'dd'-'yy HH':'mm", "MM'-'dd'-'yyyy hh':'mmtt",
			"yyyy'-'MM'-'dd hh':'mmtt", "yyyy'-'MM'-'dd HH':'mm", "yyyy'-'MM'-'dd hh':'mmtt"
		};
		private static int OffsetLengthDirectory = 10;
		private static int OffsetLengthFile = 1;

		#endregion
	}
}