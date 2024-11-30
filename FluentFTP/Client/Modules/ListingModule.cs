using System.Collections.Generic;
using FluentFTP.Client.BaseClient;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;

namespace FluentFTP.Client.Modules {
	internal static class ListingModule {

		/// <summary>
		/// Checks whether `GetListing` will be called recursively or not.
		/// </summary>
		public static bool WasGetListingRecursive(FtpListOption options) {
			// FIX: GetListing() now supports recursive listing for all types of lists (name list, file list, machine list)
			//		even if the server does not support recursive listing, because it does its own internal recursion.
			return (options & FtpListOption.Recursive) == FtpListOption.Recursive;
		}

		/// <summary>
		/// Checks if the folders are the same.
		/// </summary>
		public static bool IsItemSelf(string path, FtpListItem item) {
			return item.Name == "." ||
				item.Name == ".." ||
				item.SubType == FtpObjectSubType.ParentDirectory ||
				item.SubType == FtpObjectSubType.SelfDirectory ||
				item.FullName.EnsurePostfix("/") == path;
		}

		/// <summary>
		/// Determine which command to use for getting a listing
		/// </summary>
		public static void CalculateGetListingCommand(BaseFtpClient client, string path, FtpListOption options, out string listcmd, out bool machineList) {

			// read flags
			var isForceList = options.HasFlag(FtpListOption.ForceList);
			var isUseStat = options.HasFlag(FtpListOption.UseStat);
			var isNoPath = options.HasFlag(FtpListOption.NoPath);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isUseLS = options.HasFlag(FtpListOption.UseLS);
			var isAllFiles = options.HasFlag(FtpListOption.AllFiles);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && client.RecursiveList;

			machineList = false;

			// use stat listing if forced
			if (isUseStat) {
				listcmd = "STAT -l";
			}
			else {
				// use machine listing if supported by the server
				if (!isForceList && client.Config.ListingParser == FtpParser.Machine && client.HasFeature(FtpCapability.MLST)) {
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

		/// <summary>
		/// Can the server do recursion for us?
		/// </summary>
		public static bool IsServerSideRecursionSupported(BaseFtpClient client, FtpListOption options) {

			// Fix #539: Correctly calculate if server-side recursion is supported else fallback to manual recursion

			// check if the connected FTP server supports recursion in the first place
			if (client.RecursiveList) {

				// read flags
				var isForceList = options.HasFlag(FtpListOption.ForceList);
				var isUseStat = options.HasFlag(FtpListOption.UseStat);
				var isNameList = options.HasFlag(FtpListOption.NameList);
				var isUseLS = options.HasFlag(FtpListOption.UseLS);

				// if not using STAT listing
				if (!isUseStat) {

					// if not using machine listing (MSLD)
					if ((!isForceList || client.Config.ListingParser == FtpParser.Machine)
						&& client.HasFeature(FtpCapability.MLST)) {
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