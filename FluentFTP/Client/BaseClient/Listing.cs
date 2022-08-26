using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

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
				item = m_listParser.ParseSingleLine(path, buf, m_capabilities, machineList);
			}
			catch (FtpListParseException) {
				LogStatus(FtpTraceLevel.Verbose, "Restarting parsing from first entry in list");
				i = -1;
				lst.Clear();
				return false;
			}

			// FtpListItem.Parse() returns null if the line
			// could not be parsed
			if (item != null) {
				if (isIncludeSelf || !IsItemSelf(path, item)) {
					lst.Add(item);
				}
				else {
					//this.LogStatus(FtpTraceLevel.Verbose, "Skipped self or parent item: " + item.Name);
				}
			}
			else if (ServerHandler != null && !ServerHandler.SkipParserErrorReport()) {
				LogStatus(FtpTraceLevel.Warn, "Failed to parse file listing: " + buf);
			}
			return true;
		}

		protected bool IsItemSelf(string path, FtpListItem item) {
			return item.Name == "." ||
				item.Name == ".." ||
				item.SubType == FtpObjectSubType.ParentDirectory ||
				item.SubType == FtpObjectSubType.SelfDirectory ||
				item.FullName.EnsurePostfix("/") == path;
		}

		protected void CalculateGetListingCommand(string path, FtpListOption options, out string listcmd, out bool machineList) {

			// read flags
			var isForceList = options.HasFlag(FtpListOption.ForceList);
			var isUseStat = options.HasFlag(FtpListOption.UseStat);
			var isNoPath = options.HasFlag(FtpListOption.NoPath);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isUseLS = options.HasFlag(FtpListOption.UseLS);
			var isAllFiles = options.HasFlag(FtpListOption.AllFiles);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;

			machineList = false;

			// use stat listing if forced
			if (isUseStat) {
				listcmd = "STAT -l";
			}
			else {
				// use machine listing if supported by the server
				if (!isForceList && ListingParser == FtpParser.Machine && HasFeature(FtpCapability.MLSD)) {
					listcmd = "MLSD";
					machineList = true;
				}
				else {
					// otherwise use one of the legacy name listing commands
					if (isUseLS) {
						listcmd = "LS";
					}
					else if (isNameList) {
						listcmd = "NLST";
					}
					else {
						var listopts = "";

						listcmd = "LIST";

						// add option flags
						if (isAllFiles) {
							listopts += "a";
						}

						if (isRecursive) {
							listopts += "R";
						}

						if (listopts.Length > 0) {
							listcmd += " -" + listopts;
						}
					}
				}
			}

			if (!isNoPath) {
				listcmd = listcmd + " " + path.GetFtpPath();
			}
		}

		protected bool IsServerSideRecursionSupported(FtpListOption options) {

			// Fix #539: Correctly calculate if server-side recursion is supported else fallback to manual recursion

			// check if the connected FTP server supports recursion in the first place
			if (RecursiveList) {

				// read flags
				var isForceList = options.HasFlag(FtpListOption.ForceList);
				var isUseStat = options.HasFlag(FtpListOption.UseStat);
				var isNameList = options.HasFlag(FtpListOption.NameList);
				var isUseLS = options.HasFlag(FtpListOption.UseLS);

				// if not using STAT listing
				if (!isUseStat) {

					// if not using machine listing (MSLD)
					if ((!isForceList || ListingParser == FtpParser.Machine) && HasFeature(FtpCapability.MLSD)) {
					}
					else {

						// if not using legacy list (LS) and name listing (NSLT)
						if (!isUseLS && !isNameList) {

							// only supported if using LIST
							return true;
						}
					}
				}
			}

			return false;
		}


	}
}