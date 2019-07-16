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
	internal class FtpNonStopParser {

		/// <summary>
		/// Checks if the given listing is a valid NonStop file listing
		/// </summary>
		public static bool IsValid(FtpClient client, string[] records) {
			return IsHeader(records[0]);
		}

		private static bool IsHeader(string line) {
			if (line.IndexOf("Code") > 0 && line.IndexOf("EOF") > 0 && line.IndexOf("RWEP") > 0) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// Parses NonStop format listings
		/// </summary>
		/// <param name="record">A line from the listing</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(FtpClient client, string record) {

			if (IsHeader(record)) {
				return null;
			}

			string[] values = record.SplitString();

			if (values.Length < MinFieldCount) {
				return null;
			}

			// parse name
			string name = values[0];

			// parse date modified
			DateTime lastModified = ParseDateTime(client, values[3] + " " + values[4]);

			// check if is a dir & parse file size
			bool isDir;
			long size;
			ParseDirAndFileSize(client, values, out isDir, out size);

			// parse owner and permissions
			string owner = values[5] + values[6];
			string permissions = values[7].Trim(TrimValues);

			// create a new list item object with the parsed metadata
			FtpListItem file = new FtpListItem(record, name, size, isDir, ref lastModified);
			file.RawOwner = owner;
			file.RawPermissions = permissions;
			return file;
		}

		/// <summary>
		/// Parses the directory type & file size from NonStop format listings
		/// </summary>
		private static void ParseDirAndFileSize(FtpClient client, string[] values, out bool isDir, out long size) {
			isDir = false;
			size = 0L;
			try {
				size = Int64.Parse(values[2]);
			} catch (FormatException) {
				client.LogStatus(FtpTraceLevel.Error, "Failed to parse size: " + values[2]);
			}
		}

		/// <summary>
		/// Parses the last modified date from NonStop format listings
		/// </summary>
		private static DateTime ParseDateTime(FtpClient client, string lastModifiedStr) {
			DateTime lastModified = DateTime.MinValue;
			try {
				lastModified = DateTime.ParseExact(lastModifiedStr, DateTimeFormats, client.ListingCulture.DateTimeFormat, DateTimeStyles.None);
			} catch (FormatException) {
				client.LogStatus(FtpTraceLevel.Error, "Failed to parse date string '" + lastModifiedStr + "'");
			}

			return lastModified;
		}

		#region Constants

		private static char[] TrimValues = { '"' };
		private static int MinFieldCount = 7;
		private static string[] DateTimeFormats = { "d'-'MMM'-'yy HH':'mm':'ss" };

		#endregion
	}
}
