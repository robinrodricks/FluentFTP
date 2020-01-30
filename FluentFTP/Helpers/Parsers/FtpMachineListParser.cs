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
	internal static class FtpMachineListParser {
		/// <summary>
		/// Parses MLSD/MLST format listings
		/// </summary>
		/// <param name="record">A line from the listing</param>
		/// <param name="capabilities">Server capabilities</param>
		/// <param name="client">The FTP client</param>
		/// <returns>FtpListItem if the item is able to be parsed</returns>
		public static FtpListItem Parse(string record, List<FtpCapability> capabilities, FtpClient client) {
			var item = new FtpListItem();
			Match m;

			if (!(m = Regex.Match(record, "type=(?<type>.+?);", RegexOptions.IgnoreCase)).Success) {
				return null;
			}

			switch (m.Groups["type"].Value.ToLower()) {

				// Parent and self-directories are parsed but not always returned
				case "pdir":
					item.Type = FtpFileSystemObjectType.Directory;
					item.SubType = FtpFileSystemObjectSubType.ParentDirectory;
					break;
				case "cdir":
					item.Type = FtpFileSystemObjectType.Directory;
					item.SubType = FtpFileSystemObjectSubType.SelfDirectory;
					break;

				// Always list sub directories and files
				case "dir":
					item.Type = FtpFileSystemObjectType.Directory;
					item.SubType = FtpFileSystemObjectSubType.SubDirectory;
					break;
				case "file":
					item.Type = FtpFileSystemObjectType.File;
					break;

				// These are not supported
				case "link":
				case "device":
				default:
					return null;

			}

			if ((m = Regex.Match(record, "; (?<name>.*)$", RegexOptions.IgnoreCase)).Success) {
				item.Name = m.Groups["name"].Value;
			}
			else {
				// if we can't parse the file name there is a problem.
				return null;
			}

			ParseDateTime(record, item, client);

			ParseFileSize(record, item);

			ParsePermissions(record, item);

			return item;
		}

		/// <summary>
		/// Parses the date modified field from MLSD/MLST format listings
		/// </summary>
		private static void ParseDateTime(string record, FtpListItem item, FtpClient client) {
			Match m;
			if ((m = Regex.Match(record, "modify=(?<modify>.+?);", RegexOptions.IgnoreCase)).Success) {
				item.Modified = m.Groups["modify"].Value.GetFtpDate(client.TimeConversion);
			}

			if ((m = Regex.Match(record, "created?=(?<create>.+?);", RegexOptions.IgnoreCase)).Success) {
				item.Created = m.Groups["create"].Value.GetFtpDate(client.TimeConversion);
			}
		}

		/// <summary>
		/// Parses the file size field from MLSD/MLST format listings
		/// </summary>
		private static void ParseFileSize(string record, FtpListItem item) {
			Match m;
			if ((m = Regex.Match(record, @"size=(?<size>\d+);", RegexOptions.IgnoreCase)).Success) {
				long size;

				if (long.TryParse(m.Groups["size"].Value, out size)) {
					item.Size = size;
				}
			}
		}

		/// <summary>
		/// Parses the permissions from MLSD/MLST format listings
		/// </summary>
		private static void ParsePermissions(string record, FtpListItem item) {
			Match m;
			if ((m = Regex.Match(record, @"unix.mode=(?<mode>\d+);", RegexOptions.IgnoreCase)).Success) {
				if (m.Groups["mode"].Value.Length == 4) {
					item.SpecialPermissions = (FtpSpecialPermissions) int.Parse(m.Groups["mode"].Value[0].ToString());
					item.OwnerPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[1].ToString());
					item.GroupPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[2].ToString());
					item.OthersPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[3].ToString());
					item.CalculateChmod();
				}
				else if (m.Groups["mode"].Value.Length == 3) {
					item.OwnerPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[0].ToString());
					item.GroupPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[1].ToString());
					item.OthersPermissions = (FtpPermission) int.Parse(m.Groups["mode"].Value[2].ToString());
					item.CalculateChmod();
				}
			}
		}
	}
}