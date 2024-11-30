using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.Modules;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Get the basic flags required for `GetListing`.
		/// </summary>
		protected bool LoadBasicListingInfo(ref string path, ref FtpListItem item, List<FtpListItem> lst, List<string> rawlisting, ref int i, string listcmd, string buf, bool isRecursive, bool isIncludeSelf, bool machineList) {

			// if this is a result of LIST -R then the path will be spit out
			// before each block of objects
			if (listcmd.StartsWith("LIST") && isRecursive) {
				if (buf.StartsWith("/") && buf.EndsWith(":")) {
					path = buf.TrimEnd(':');
					return false;
				}
			}

			// if the next line in the listing starts with spaces
			// it is assumed to be a continuation of the current line
			if (i + 1 < rawlisting.Count && (rawlisting[i + 1].StartsWith("\t") || rawlisting[i + 1].StartsWith(" "))) {
				buf += rawlisting[++i];
			}

			try {
				item = CurrentListParser.ParseSingleLine(path, buf, m_capabilities, machineList);
			}
			catch (FtpListParseException) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Restarting parsing from first entry in list");
				i = -1;
				lst.Clear();
				return false;
			}

			// FtpListItem.Parse() returns null if the line
			// could not be parsed
			if (item != null) {
				if (isIncludeSelf || !ListingModule.IsItemSelf(path, item)) {
					lst.Add(item);
				}
				else {
					//this.LogStatus(FtpTraceLevel.Verbose, "Skipped self or parent item: " + item.Name);
				}
			}
			else if (ServerHandler != null && !ServerHandler.SkipParserErrorReport()) {
				LogWithPrefix(FtpTraceLevel.Warn, "Failed to parse file listing: " + buf);
			}
			return true;
		}


	}
}
