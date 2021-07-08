namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	internal static class FileListings {


		/// <summary>
		/// Checks if the given file exists in the given file listing.
		/// Supports servers that return:  1) full paths,  2) only filenames,  3) full paths without slash prefixed
		/// </summary>
		/// <param name="fileList">The listing returned by GetNameListing</param>
		/// <param name="path">The full file path you want to check</param>
		/// <returns></returns>
		public static bool FileExistsInNameListing(string[] fileList, string path) {
			// exit quickly if no paths
			if (fileList.Length == 0) {
				return false;
			}

			// cleanup file path, get file name
			var pathName = path.GetFtpFileName();
			var pathPrefixed = path.EnsurePrefix("/");

			// per entry in the name list
			foreach (var fileListEntry in fileList) {
				// FIX: support servers that return:  1) full paths,  2) only filenames,  3) full paths without slash prefixed
				if (fileListEntry == pathName || fileListEntry == path || fileListEntry.EnsurePrefix("/") == pathPrefixed) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the given file exists in the given file listing.
		/// </summary>
		/// <param name="fileList">The listing returned by GetListing</param>
		/// <param name="path">The full file path you want to check</param>
		/// <returns></returns>
		public static bool FileExistsInListing(FtpListItem[] fileList, string path) {
			// exit quickly if no paths
			if (fileList == null || fileList.Length == 0) {
				return false;
			}

			// cleanup file path, get file name
			var trimSlash = new char[] { '/' };
			var pathClean = path.Trim(trimSlash);

			// per entry in the list
			foreach (var fileListEntry in fileList) {
				if (fileListEntry.FullName.Trim(trimSlash) == pathClean) {
					return true;
				}
			}

			return false;
		}


	}
}
