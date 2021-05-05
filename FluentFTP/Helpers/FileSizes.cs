namespace FluentFTP.Helpers {
	/// <summary>
	/// Extension methods related to FTP tasks
	/// </summary>
	public static class FileSizes {

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this int bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this uint bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this ulong bytes) {
			return ((long)bytes).FileSizeToString();
		}

		/// <summary>
		/// Converts a file size in bytes to a string representation (eg. 12345 becomes 12.3 KB)
		/// </summary>
		public static string FileSizeToString(this long bytes) {
			var order = 0;
			double len = bytes;
			while (len >= 1024 && order < sizePostfix.Length - 1) {
				order++;
				len = len / 1024;
			}

			return string.Format("{0:0.#} {1}", len, sizePostfix[order]);
		}
		private static string[] sizePostfix = { "bytes", "KB", "MB", "GB", "TB" };


	}
}